using MaterialManagement.Api.Domain;
using MaterialManagement.Api.Models;

namespace MaterialManagement.Api.Dtos;

public record ProjectInput(
    string Name, ProjectStatus Status, DateOnly? StartDate, string? Notes,
    Wash? Wash, Bleach? Bleach, Iron? Iron, DryClean? DryClean, TumbleDry? TumbleDry)
{
    public string? Validate() => string.IsNullOrWhiteSpace(Name) ? "Name is required." : null;

    public void Apply(Project p)
    {
        (p.Name, p.Status, p.StartDate, p.Notes) = (Name.Trim(), Status, StartDate, Notes);
        (p.Wash, p.Bleach, p.Iron, p.DryClean, p.TumbleDry) = (Wash, Bleach, Iron, DryClean, TumbleDry);
    }
}

public static class ProjectView
{
    /// <summary>Needs cuts loaded with <c>Db.CutsFull</c>.</summary>
    public static object From(Project p, IEnumerable<Cut> cuts) => new
    {
        p.Id, p.Name, p.Status, p.StartDate, p.Notes, HasPhoto = p.Photo != null,
        p.Wash, p.Bleach, p.Iron, p.DryClean, p.TumbleDry, // overrides; `Care` below is the calculated value
        Cuts = cuts.OrderBy(c => c.Material!.Name).ThenBy(c => c.Id).Select(c => new
        {
            c.Id, c.MaterialId, MaterialName = c.Material!.Name, MaterialColor = c.Material.Color, c.W, c.H, c.Label, c.CreatedAt,
            Area = c.W * c.H, Cost = CutCost(c),
        }),
        // One row per material used: number of cut pieces, area (cm²) and cost (each cut at its own purchase's price).
        Materials = cuts.GroupBy(c => c.Material!).OrderBy(g => g.Key.Name).Select(g => new
        {
            MaterialId = g.Key.Id, g.Key.Name, g.Key.Color, Pieces = g.Count(),
            Area = g.Sum(c => c.W * c.H), Cost = g.Sum(CutCost),
        }),
        Totals = new { Pieces = cuts.Count(), Area = cuts.Sum(c => c.W * c.H), Cost = cuts.Sum(CutCost) },
        Fibers = FiberView(Domain.Fibers.ByWeight(cuts.Select(c => (c.W * c.H, c.Material!.WeightGsm, c.Material.Composition)))),
        // Cuts left out of the fiber calculation because their material has no g/m² or no composition.
        UncountedCuts = cuts.Count(c => c.Material!.WeightGsm is not > 0 || c.Material.Composition is not { Count: > 0 }),
        Care = Care(cuts.Select(c => c.Material!).DistinctBy(m => m.Id).ToList()),
    };

    static decimal? CutCost(Cut c) => c.Purchase!.CostOf(c.W * c.H, c.Material!.RollWidthCm);

    static IEnumerable<object> FiberView(List<(Fiber Fiber, double Grams)> fibers)
    {
        var total = fibers.Sum(f => f.Grams);
        return fibers.Select(f => new { f.Fiber, f.Grams, Percent = f.Grams / total * 100 });
    }

    // Care for the finished product, plus the materials that have no value set for each category.
    static object Care(List<Material> ms) => new
    {
        Wash = CareRules.Strictest(ms.Select(m => m.Wash)),
        Bleach = CareRules.Strictest(ms.Select(m => m.Bleach)),
        Iron = CareRules.Strictest(ms.Select(m => m.Iron)),
        DryClean = CareRules.Strictest(ms.Select(m => m.DryClean)),
        TumbleDry = CareRules.Strictest(ms.Select(m => m.TumbleDry)),
        Missing = new Dictionary<string, IEnumerable<string>>
        {
            ["wash"] = ms.Where(m => m.Wash == null).Select(m => m.Name),
            ["bleach"] = ms.Where(m => m.Bleach == null).Select(m => m.Name),
            ["iron"] = ms.Where(m => m.Iron == null).Select(m => m.Name),
            ["dryClean"] = ms.Where(m => m.DryClean == null).Select(m => m.Name),
            ["tumbleDry"] = ms.Where(m => m.TumbleDry == null).Select(m => m.Name),
        }.Where(kv => kv.Value.Any()).ToDictionary(),
    };
}
