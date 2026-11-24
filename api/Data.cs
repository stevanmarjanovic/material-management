using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace MaterialManagement.Api;

// Care values follow ISO 3758. Names must match web/src/catalog.js.
public enum Wash { W30, W30Mild, W30VeryMild, W40, W40Mild, W40VeryMild, W50, W50Mild, W60, W60Mild, W70, W95, Hand, DoNot }
public enum Bleach { Any, NonChlorine, DoNot }
public enum Iron { Low, Medium, High, DoNot }
public enum DryClean { P, PMild, F, FMild, W, WMild, WVeryMild, DoNot }
public enum TumbleDry { Normal, Low, DoNot }
public enum ProjectStatus { Planned, InProgress, Done }

/// <summary>One purchase of a fabric, always full roll width. Its first piece is RollWidthCm x LengthCm.</summary>
public class Material
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Color { get; set; }
    public string? Composition { get; set; }
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
        b.Entity<Piece>().Property(x => x.Shape).HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<List<Rect>>(v, JsonSerializerOptions.Default)!);
        b.Entity<Cut>().Property(x => x.ResultPieceIds).HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<List<int>>(v, JsonSerializerOptions.Default)!);
    }
}
