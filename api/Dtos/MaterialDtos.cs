using MaterialManagement.Api.Domain;
using MaterialManagement.Api.Models;

namespace MaterialManagement.Api.Dtos;

public record MaterialInput(
    string Name, string? Color, List<FiberPart>? Composition, double? RollWidthCm, int? WeightGsm,
    string? NeedleType, int? NeedleSize, string? Certificate, Wash? Wash, Bleach? Bleach, Iron? Iron,
    DryClean? DryClean, TumbleDry? TumbleDry, string? Link, int? StoreId)
{
    public string? Validate() =>
        string.IsNullOrWhiteSpace(Name) ? "Name is required."
        : Fibers.Validate(Composition);

    public void Apply(Material m)
    {
        m.Name = Name.Trim(); m.Color = Color; m.Composition = Composition is { Count: > 0 } ? Composition : null; m.RollWidthCm = RollWidthCm;
        m.WeightGsm = WeightGsm; m.NeedleType = NeedleType; m.NeedleSize = NeedleSize; m.Certificate = Certificate;
        m.Wash = Wash; m.Bleach = Bleach; m.Iron = Iron; m.DryClean = DryClean; m.TumbleDry = TumbleDry;
        m.Link = Link; m.StoreId = StoreId;
    }
}

public static class MaterialView
{
    /// <summary>Needs a material loaded with <c>Db.MaterialsFull</c>.</summary>
    public static object From(Material m) => new
    {
        m.Id, m.Name, m.Color, m.Composition, m.RollWidthCm, m.WeightGsm, m.NeedleType, m.NeedleSize,
        m.Certificate, m.Wash, m.Bleach, m.Iron, m.DryClean, m.TumbleDry, m.Link, m.StoreId, StoreName = m.Store?.Name,
        HasPhoto = m.Photo != null, m.PreviewColor,
        Purchases = m.Purchases.OrderBy(p => p.PurchaseDate).ThenBy(p => p.Id).Select(p => new
        {
            p.Id, p.LengthCm, p.PricePerMeter, p.DiscountPercent, p.FinalPricePerMeter, p.TotalPrice, p.PurchaseDate,
            p.StorePurchaseId, Url = m.Store?.PurchaseUrl(p.StorePurchaseId),
            Cut = m.Cuts.Any(c => c.PurchaseId == p.Id), // once cut: length is fixed and it can't be deleted
        }),
        Pieces = m.Pieces.Where(p => p.Active).OrderBy(p => p.Id).Select(p => new { p.Id, p.PurchaseId, p.Shape, Area = Geometry.Area(p.Shape) }),
        RemainingArea = m.Pieces.Where(p => p.Active).Sum(p => Geometry.Area(p.Shape)),
        Cuts = m.Cuts.OrderByDescending(c => c.Id).Select(c => new
        {
            c.Id, c.SourcePieceId, c.X, c.Y, c.W, c.H, c.Label, c.CreatedAt, c.ProjectId, ProjectName = c.Project?.Name,
            Area = c.W * c.H, Cost = m.Purchases.First(p => p.Id == c.PurchaseId).CostOf(c.W * c.H, m.RollWidthCm),
        }),
    };
}
