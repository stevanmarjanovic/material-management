using MaterialManagement.Api;

public class PurchaseTests
{
    [Fact]
    public void Cuts_AreCostedAtTheirOwnPurchasePrice()
    {
        // Same 140 cm roll bought twice at different prices; 0.28 m² cut from each.
        var first = new Purchase { PricePerMeter = 1790, DiscountPercent = 20, LengthCm = 100 };
        var second = new Purchase { PricePerMeter = 1990, LengthCm = 50 };

        Assert.Equal(1432m, first.FinalPricePerMeter);
        Assert.Equal(1432m, first.TotalPrice);
        Assert.Equal(286.40m, Math.Round(first.CostOf(2800, 140)!.Value, 2));
        Assert.Equal(398.00m, Math.Round(second.CostOf(2800, 140)!.Value, 2));
        Assert.Null(second.CostOf(2800, null)); // no roll width: no cost
    }
}
