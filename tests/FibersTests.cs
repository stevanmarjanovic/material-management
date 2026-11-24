using MaterialManagement.Api;

public class FibersTests
{
    [Fact]
    public void Validate_RequiresExactly100()
    {
        Assert.Null(Fibers.Validate([new(Fiber.Cotton, 95), new(Fiber.Elastane, 5)]));
        Assert.NotNull(Fibers.Validate([new(Fiber.Cotton, 90), new(Fiber.Elastane, 5)]));
        Assert.NotNull(Fibers.Validate([new(Fiber.Cotton, 50), new(Fiber.Cotton, 50)]));
        Assert.Null(Fibers.Validate(null));
    }

    [Fact]
    public void ByWeight_WeighsByAreaAndGsm_SkipsIncomplete()
    {
        // Same 1 m² area: 295 g/m² cotton velvet + 100 g/m² polyester felt -> 295 g vs 100 g.
        var result = Fibers.ByWeight([
            (10000, 295, [new(Fiber.Cotton, 100)]),
            (10000, 100, [new(Fiber.Polyester, 100)]),
            (10000, null, [new(Fiber.Wool, 100)]),
        ]);
        Assert.Equal([(Fiber.Cotton, 295.0), (Fiber.Polyester, 100.0)], result.Select(r => (r.Fiber, Math.Round(r.Grams, 6))));
    }
}
