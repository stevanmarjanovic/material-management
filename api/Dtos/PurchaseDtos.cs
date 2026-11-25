using MaterialManagement.Api.Models;

namespace MaterialManagement.Api.Dtos;

public record PurchaseInput(double LengthCm, decimal? PricePerMeter, decimal? DiscountPercent, DateOnly? PurchaseDate, string? StorePurchaseId)
{
    public string? Validate() =>
        LengthCm <= 0 ? "Length must be > 0."
        : PricePerMeter < 0 ? "Price can't be negative."
        : DiscountPercent is < 0 or > 100 ? "Discount must be 0-100 %."
        : null;

    public void Apply(Purchase p) =>
        (p.LengthCm, p.PricePerMeter, p.DiscountPercent, p.PurchaseDate, p.StorePurchaseId) =
        (LengthCm, PricePerMeter, DiscountPercent, PurchaseDate, string.IsNullOrWhiteSpace(StorePurchaseId) ? null : StorePurchaseId.Trim());
}
