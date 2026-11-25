namespace MaterialManagement.Api.Models;

/// <summary>One buy of a material: full roll width x LengthCm. Its first piece has that size;
/// every piece and cut that comes from it links back here, so each cut is costed at this purchase's price.</summary>
public class Purchase
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public double LengthCm { get; set; }
    public decimal? PricePerMeter { get; set; }
    public decimal? DiscountPercent { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public string? StorePurchaseId { get; set; } // the store's order/receipt number; with the store's template it links to the purchase

    public decimal? FinalPricePerMeter => PricePerMeter * (1 - (DiscountPercent ?? 0) / 100);
    public decimal? TotalPrice => FinalPricePerMeter * (decimal)LengthCm / 100;
    // Price is per meter of the full roll width, so cost per cm² = price per m / roll width / 100.
    public decimal? CostOf(double areaCm2, double? rollWidthCm) =>
        rollWidthCm is > 0 ? FinalPricePerMeter * (decimal)(areaCm2 / rollWidthCm.Value) / 100 : null;
}
