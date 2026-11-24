using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace MaterialManagement.Api;

// Care values follow ISO 3758. Names must match web/src/catalog.js.
public enum Wash { W30, W30Mild, W30VeryMild, W40, W40Mild, W40VeryMild, W50, W50Mild, W60, W60Mild, W70, W95, Hand, DoNot }
public enum Bleach { Any, NonChlorine, DoNot }
public enum Iron { Low, Medium, High, DoNot }
public enum DryClean { P, PMild, F, FMild, W, WMild, WVeryMild, DoNot }
public enum TumbleDry { Normal, Low, DoNot }
public enum ProjectStatus { Planned, InProgress, Done }
public enum Fiber { Cotton, Polyester, Elastane, Viscose, Polyamide, Wool, Linen, Silk, Other }

public record FiberPart(Fiber Fiber, double Percent);

/// <summary>Care for a product sewn from several fabrics: per category, the most delicate fabric wins.</summary>
public static class CareRules
{
    // Least to most restrictive.
    static readonly Wash[] WashOrder = [Wash.W95, Wash.W70, Wash.W60, Wash.W60Mild, Wash.W50, Wash.W50Mild, Wash.W40,
        Wash.W40Mild, Wash.W40VeryMild, Wash.W30, Wash.W30Mild, Wash.W30VeryMild, Wash.Hand, Wash.DoNot];
    static readonly Bleach[] BleachOrder = [Bleach.Any, Bleach.NonChlorine, Bleach.DoNot];
    static readonly Iron[] IronOrder = [Iron.High, Iron.Medium, Iron.Low, Iron.DoNot];
    static readonly TumbleDry[] TumbleOrder = [TumbleDry.Normal, TumbleDry.Low, TumbleDry.DoNot];

    public static Wash? Strictest(IEnumerable<Wash?> v) => Strictest(v, WashOrder);
    public static Bleach? Strictest(IEnumerable<Bleach?> v) => Strictest(v, BleachOrder);
    public static Iron? Strictest(IEnumerable<Iron?> v) => Strictest(v, IronOrder);
    public static TumbleDry? Strictest(IEnumerable<TumbleDry?> v) => Strictest(v, TumbleOrder);

    static T? Strictest<T>(IEnumerable<T?> values, T[] order) where T : struct =>
        values.OfType<T>().Select(v => (T?)v).MaxBy(v => Array.IndexOf(order, v!.Value));

    /// <summary>
    /// P allows the F solvents too, so P + F = F; any mild = mild. Wet cleaning (W) is a different process:
    /// mixing it with P/F means no single process is known to suit all fabrics, so: do not dry clean.
    /// </summary>
    public static DryClean? Strictest(IEnumerable<DryClean?> values)
    {
        var v = values.OfType<DryClean>().Distinct().ToList();
        if (v.Count == 0) return null;
        if (v.Contains(DryClean.DoNot)) return DryClean.DoNot;
        var wet = v.Where(x => x is DryClean.W or DryClean.WMild or DryClean.WVeryMild).ToList();
        if (wet.Count > 0 && wet.Count < v.Count) return DryClean.DoNot;
        if (wet.Count > 0) return wet.Max(); // W < WMild < WVeryMild in the enum
        var solventF = v.Any(x => x is DryClean.F or DryClean.FMild);
        var mild = v.Any(x => x is DryClean.PMild or DryClean.FMild);
        return (solventF, mild) switch
        {
            (true, true) => DryClean.FMild, (true, false) => DryClean.F, (false, true) => DryClean.PMild, _ => DryClean.P,
        };
    }
}

public static class Fibers
{
    public static string? Validate(List<FiberPart>? parts)
    {
        if (parts is not { Count: > 0 }) return null;
        if (parts.Any(p => p.Percent is <= 0 or > 100)) return "Each fiber must be between 0 and 100 %.";
        if (parts.Select(p => p.Fiber).Distinct().Count() != parts.Count) return "Each fiber can be listed only once.";
        if (Math.Abs(parts.Sum(p => p.Percent) - 100) > 0.01) return "Composition must add up to 100 %.";
        return null;
    }

    /// <summary>Fiber weights in grams for cut pieces (area cm², g/m², composition). Pieces missing weight or composition are skipped.</summary>
    public static List<(Fiber Fiber, double Grams)> ByWeight(IEnumerable<(double AreaCm2, int? Gsm, List<FiberPart>? Parts)> pieces) =>
        pieces.Where(x => x.Gsm > 0 && x.Parts is { Count: > 0 })
            .SelectMany(x => x.Parts!.Select(p => (p.Fiber, Grams: x.AreaCm2 / 10000 * x.Gsm!.Value * p.Percent / 100)))
            .GroupBy(x => x.Fiber)
            .Select(g => (g.Key, g.Sum(x => x.Grams)))
            .OrderByDescending(x => x.Item2)
            .ToList();
}

/// <summary>One purchase of a fabric, always full roll width. Its first piece is RollWidthCm x LengthCm.</summary>
public class Material
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Color { get; set; }
    public List<FiberPart>? Composition { get; set; }
    public double? RollWidthCm { get; set; }
    public int? WeightGsm { get; set; }
    public string? NeedleType { get; set; }
    public int? NeedleSize { get; set; }
    public string? Certificate { get; set; }
    public Wash? Wash { get; set; }
    public Bleach? Bleach { get; set; }
    public Iron? Iron { get; set; }
    public DryClean? DryClean { get; set; }
    public TumbleDry? TumbleDry { get; set; }
    public string? Link { get; set; }
    public double LengthCm { get; set; }
    public decimal? PricePerMeter { get; set; }
    public decimal? DiscountPercent { get; set; }
    public string? Store { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public byte[]? Photo { get; set; }
    public string? PhotoContentType { get; set; }
    public string? PreviewColor { get; set; } // average photo color, #rrggbb
    public List<Piece> Pieces { get; set; } = [];
    public List<Cut> Cuts { get; set; } = [];

    public decimal? FinalPricePerMeter => PricePerMeter * (1 - (DiscountPercent ?? 0) / 100);
    public decimal? TotalPrice => FinalPricePerMeter * (decimal)LengthCm / 100;
    public decimal? CostOf(double areaCm2) =>
        RollWidthCm is > 0 ? FinalPricePerMeter * (decimal)(areaCm2 / RollWidthCm.Value) / 100 : null;
}

public class Project
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ProjectStatus Status { get; set; }
    public DateOnly? StartDate { get; set; }
    public string? Notes { get; set; }
    public byte[]? Photo { get; set; }
    public string? PhotoContentType { get; set; }
}

public class Piece
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public List<Rect> Shape { get; set; } = [];
    // Cut pieces are kept (inactive) so the last cut can be undone.
    public bool Active { get; set; } = true;
}

public class Cut
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public int SourcePieceId { get; set; }
    public Material? Material { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<int> ResultPieceIds { get; set; } = [];
}

public class Db(DbContextOptions<Db> options) : DbContext(options)
{
    static readonly JsonSerializerOptions EnumAsText = new() { Converters = { new JsonStringEnumConverter() } };

    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Piece> Pieces => Set<Piece>();
    public DbSet<Cut> Cuts => Set<Cut>();
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        var m = b.Entity<Material>();
        m.Property(x => x.Wash).HasConversion<string>();
        m.Property(x => x.Bleach).HasConversion<string>();
        m.Property(x => x.Iron).HasConversion<string>();
        m.Property(x => x.DryClean).HasConversion<string>();
        m.Property(x => x.TumbleDry).HasConversion<string>();
        m.Property(x => x.PricePerMeter).HasConversion<double?>(); // SQLite can't sort/sum decimal
        m.Property(x => x.DiscountPercent).HasConversion<double?>();
        b.Entity<Project>().Property(x => x.Status).HasConversion<string>();
        b.Entity<Cut>().HasOne(c => c.Project).WithMany().OnDelete(DeleteBehavior.SetNull);
        m.Property(x => x.Composition).HasConversion(
            v => v == null ? null : JsonSerializer.Serialize(v, EnumAsText),
            v => v == null ? null : JsonSerializer.Deserialize<List<FiberPart>>(v, EnumAsText));
        b.Entity<Piece>().Property(x => x.Shape).HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<List<Rect>>(v, JsonSerializerOptions.Default)!);
        b.Entity<Cut>().Property(x => x.ResultPieceIds).HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<List<int>>(v, JsonSerializerOptions.Default)!);
    }
}
