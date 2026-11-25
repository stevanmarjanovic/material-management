using System.Text.Json;
using System.Text.Json.Serialization;
using MaterialManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MaterialManagement.Api.Data;

public class Db(DbContextOptions<Db> options) : DbContext(options)
{
    static readonly JsonSerializerOptions EnumAsText = new() { Converters = { new JsonStringEnumConverter() } };

    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Piece> Pieces => Set<Piece>();
    public DbSet<Cut> Cuts => Set<Cut>();
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>Materials with everything their view needs.</summary>
    public IQueryable<Material> MaterialsFull =>
        Materials.Include(m => m.Store).Include(m => m.Purchases).Include(m => m.Pieces).Include(m => m.Cuts).ThenInclude(c => c.Project);

    /// <summary>Cuts with what project views need for cost and fibers.</summary>
    public IQueryable<Cut> CutsFull => Cuts.Include(c => c.Material).Include(c => c.Purchase);

    protected override void OnModelCreating(ModelBuilder b)
    {
        var m = b.Entity<Material>();
        m.Property(x => x.Wash).HasConversion<string>();
        m.Property(x => x.Bleach).HasConversion<string>();
        m.Property(x => x.Iron).HasConversion<string>();
        m.Property(x => x.DryClean).HasConversion<string>();
        m.Property(x => x.TumbleDry).HasConversion<string>();
        var pu = b.Entity<Purchase>();
        pu.Property(x => x.PricePerMeter).HasConversion<double?>(); // SQLite can't sort/sum decimal
        pu.Property(x => x.DiscountPercent).HasConversion<double?>();
        m.HasOne(x => x.Store).WithMany().OnDelete(DeleteBehavior.SetNull); // deleting a store keeps its materials
        b.Entity<Piece>().HasOne<Purchase>().WithMany().HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Cut>().HasOne(c => c.Purchase).WithMany().HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Restrict);
        var p = b.Entity<Project>();
        p.Property(x => x.Status).HasConversion<string>();
        p.Property(x => x.Wash).HasConversion<string>();
        p.Property(x => x.Bleach).HasConversion<string>();
        p.Property(x => x.Iron).HasConversion<string>();
        p.Property(x => x.DryClean).HasConversion<string>();
        p.Property(x => x.TumbleDry).HasConversion<string>();
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
