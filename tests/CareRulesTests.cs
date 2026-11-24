using MaterialManagement.Api;

public class CareRulesTests
{
    [Fact]
    public void Strictest_MostDelicateFabricWins_IgnoresUnset()
    {
        Assert.Equal(Wash.W30, CareRules.Strictest([Wash.W60, Wash.W30, null]));
        Assert.Equal(Wash.Hand, CareRules.Strictest([Wash.W30VeryMild, Wash.Hand]));
        Assert.Equal(Iron.Low, CareRules.Strictest([Iron.High, Iron.Low, Iron.Medium]));
        Assert.Null(CareRules.Strictest(new Bleach?[] { null, null }));
    }

    [Theory]
    [InlineData(new[] { DryClean.P, DryClean.F }, DryClean.F)]
    [InlineData(new[] { DryClean.PMild, DryClean.F }, DryClean.FMild)]
    [InlineData(new[] { DryClean.W, DryClean.WVeryMild }, DryClean.WVeryMild)]
    [InlineData(new[] { DryClean.P, DryClean.W }, DryClean.DoNot)]
    [InlineData(new[] { DryClean.P, DryClean.DoNot }, DryClean.DoNot)]
    public void DryClean_CombinesSolventAndMildness(DryClean[] input, DryClean expected) =>
        Assert.Equal(expected, CareRules.Strictest(input.Select(x => (DryClean?)x)));
}
