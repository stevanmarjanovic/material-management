using MaterialManagement.Api.Domain;
using MaterialManagement.Api.Models;

public class StoreTests
{
    [Fact]
    public void PurchaseUrl_PutsTheEncodedIdIntoTheTemplate()
    {
        var store = new Store { Name = "Liupka", PurchaseUrlTemplate = "https://liupka.rs/orders/{id}/details" };
        Assert.Equal("https://liupka.rs/orders/12345/details", store.PurchaseUrl("12345"));
        Assert.Equal("https://liupka.rs/orders/A%201%2F2/details", store.PurchaseUrl(" A 1/2 "));
        Assert.Null(store.PurchaseUrl(null));
        Assert.Null(new Store { Name = "No template" }.PurchaseUrl("12345"));
    }
}
