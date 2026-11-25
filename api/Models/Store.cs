namespace MaterialManagement.Api.Models;

/// <summary>A shop where materials are bought.</summary>
public class Store
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string? WorkingHours { get; set; } // free text, e.g. "Mon-Fri 9-17, Sat 9-14"
    // Link to one purchase on the store's site: "{id}" is replaced by the purchase's ID, e.g. https://shop.rs/orders/{id}
    public string? PurchaseUrlTemplate { get; set; }

    public const string IdToken = "{id}";
    public string? PurchaseUrl(string? purchaseId) =>
        string.IsNullOrWhiteSpace(PurchaseUrlTemplate) || string.IsNullOrWhiteSpace(purchaseId) ? null
        : PurchaseUrlTemplate.Replace(IdToken, Uri.EscapeDataString(purchaseId.Trim()));
}
