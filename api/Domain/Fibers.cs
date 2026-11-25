using MaterialManagement.Api.Models;

namespace MaterialManagement.Api.Domain;

public static class Fibers
{
    public static string? Validate(List<FiberPart>? parts)
    {
        if (parts is not { Count: > 0 }) return null;
        if (parts.Any(p => p.Percent is <= 0 or > 100)) return "Each fiber must be between 0 and 100 %.";
        if (parts.Select(p => p.Fiber).Distinct().Count() != parts.Count) return "Each fiber can be listed only once.";
        if (Math.Abs(parts.Sum(p => p.Percent) - 100) > 0.01) return "Composition must add up to 100 %.";
        return null;
    }

    /// <summary>Fiber weights in grams for cut pieces (area cm², g/m², composition). Pieces missing weight or composition are skipped.</summary>
    public static List<(Fiber Fiber, double Grams)> ByWeight(IEnumerable<(double AreaCm2, int? Gsm, List<FiberPart>? Parts)> pieces) =>
        pieces.Where(x => x.Gsm > 0 && x.Parts is { Count: > 0 })
            .SelectMany(x => x.Parts!.Select(p => (p.Fiber, Grams: x.AreaCm2 / 10000 * x.Gsm!.Value * p.Percent / 100)))
            .GroupBy(x => x.Fiber)
            .Select(g => (g.Key, g.Sum(x => x.Grams)))
            .OrderByDescending(x => x.Item2)
            .ToList();
}
