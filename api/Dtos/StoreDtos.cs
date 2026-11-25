using MaterialManagement.Api.Models;

namespace MaterialManagement.Api.Dtos;

public record StoreInput(string Name, string? Address, string? Website, string? WorkingHours, string? PurchaseUrlTemplate)
{
    static bool IsWebUrl(string? s) =>
        Uri.TryCreate(s, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

    public string? Validate() =>
        string.IsNullOrWhiteSpace(Name) ? "Name is required."
        : !string.IsNullOrWhiteSpace(Website) && !IsWebUrl(Website) ? "Website must start with http:// or https://."
        : string.IsNullOrWhiteSpace(PurchaseUrlTemplate) ? null
        : !PurchaseUrlTemplate.Contains(Store.IdToken) ? "Purchase link must contain {id} where the purchase ID goes."
        : !IsWebUrl(PurchaseUrlTemplate.Replace(Store.IdToken, "1")) ? "Purchase link must start with http:// or https://."
        : null;

    static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    public void Apply(Store s) =>
        (s.Name, s.Address, s.Website, s.WorkingHours, s.PurchaseUrlTemplate) =
        (Name.Trim(), Clean(Address), Clean(Website), Clean(WorkingHours), Clean(PurchaseUrlTemplate));
}

public static class StoreView
{
    public static object From(Store s, IEnumerable<Material> materials) => new
    {
        s.Id, s.Name, s.Address, s.Website, s.WorkingHours, s.PurchaseUrlTemplate,
        Materials = materials.OrderBy(m => m.Name).ThenBy(m => m.Color).Select(m => new { m.Id, m.Name, m.Color }),
    };
}
