using System.Text.Json.Serialization;
using MaterialManagement.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<Db>(o => o.UseSqlite("Data Source=materials.db"));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();

// Schema changes: dotnet ef migrations add <Name> --project api
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Db>();
    db.Database.Migrate();
    // Photos without a preview color yet (uploaded before it existed, or reset by a migration).
    foreach (var m in db.Materials.Where(m => m.Photo != null && m.PreviewColor == null).ToList())
        m.PreviewColor = ImageColor.AverageCenter(m.Photo!);
    db.SaveChanges();
}

var api = app.MapGroup("/api");

object View(Material m) => new
{
    m.Id, m.Name, m.Color, m.Composition, m.RollWidthCm, m.WeightGsm, m.NeedleType, m.NeedleSize,
    m.Certificate, m.Wash, m.Bleach, m.Iron, m.DryClean, m.TumbleDry, m.Link, m.Store,
    HasPhoto = m.Photo != null, m.PreviewColor,
    Purchases = m.Purchases.OrderBy(p => p.PurchaseDate).ThenBy(p => p.Id).Select(p => new
    {
        p.Id, p.LengthCm, p.PricePerMeter, p.DiscountPercent, p.FinalPricePerMeter, p.TotalPrice, p.PurchaseDate,
        Cut = m.Cuts.Any(c => c.PurchaseId == p.Id), // once cut: length is fixed and it can't be deleted
    }),
    Pieces = m.Pieces.Where(p => p.Active).OrderBy(p => p.Id).Select(p => new { p.Id, p.PurchaseId, p.Shape, Area = Geometry.Area(p.Shape) }),
    RemainingArea = m.Pieces.Where(p => p.Active).Sum(p => Geometry.Area(p.Shape)),
    Cuts = m.Cuts.OrderByDescending(c => c.Id).Select(c => new
    {
        c.Id, c.SourcePieceId, c.X, c.Y, c.W, c.H, c.Label, c.CreatedAt, c.ProjectId, ProjectName = c.Project?.Name,
        Area = c.W * c.H, Cost = m.Purchases.First(p => p.Id == c.PurchaseId).CostOf(c.W * c.H, m.RollWidthCm),
    }),
};

IQueryable<Material> Full(Db db) =>
    db.Materials.Include(m => m.Purchases).Include(m => m.Pieces).Include(m => m.Cuts).ThenInclude(c => c.Project);

api.MapGet("/materials", async (Db db) =>
    (await Full(db).OrderBy(m => m.Name).ToListAsync()).Select(View));

api.MapGet("/materials/{id:int}", async (int id, Db db) =>
    await Full(db).FirstOrDefaultAsync(m => m.Id == id) is { } m ? Results.Ok(View(m)) : Results.NotFound());

api.MapPost("/materials", async (MaterialInput input, Db db) =>
{
    if (input.Validate() is { } error) return Results.BadRequest(error);
    if (input.RollWidthCm is not > 0) return Results.BadRequest("Roll width must be > 0.");
    var m = new Material { Name = input.Name };
    input.Apply(m);
    db.Materials.Add(m);
    await db.SaveChangesAsync();
    return Results.Ok(View(m));
});

api.MapPut("/materials/{id:int}", async (int id, MaterialInput input, Db db) =>
{
    if (input.Validate() is { } error) return Results.BadRequest(error);
    var m = await Full(db).FirstOrDefaultAsync(m => m.Id == id);
    if (m == null) return Results.NotFound();
    // The roll width is the width of every bought piece, so it can only change while nothing is cut.
    var uncut = m.Cuts.Count == 0;
    if (input.RollWidthCm != m.RollWidthCm && !uncut) return Results.BadRequest("Roll width can't change after the first cut.");
    if (input.RollWidthCm is not > 0) return Results.BadRequest("Roll width must be > 0.");
    input.Apply(m);
    if (uncut)
        foreach (var piece in m.Pieces)
            piece.Shape = [new Rect(0, 0, input.RollWidthCm.Value, m.Purchases.First(p => p.Id == piece.PurchaseId).LengthCm)];
    await db.SaveChangesAsync();
    return Results.Ok(View(m));
});

api.MapDelete("/materials/{id:int}", async (int id, Db db) =>
{
    await db.Cuts.Where(c => c.MaterialId == id).ExecuteDeleteAsync();
    await db.Pieces.Where(p => p.MaterialId == id).ExecuteDeleteAsync();
    await db.Purchases.Where(p => p.MaterialId == id).ExecuteDeleteAsync();
    return await db.Materials.Where(m => m.Id == id).ExecuteDeleteAsync() > 0 ? Results.NoContent() : Results.NotFound();
});

// Price after discount for values not saved yet (live preview in forms). Same formula as a saved purchase.
api.MapGet("/prices", (decimal pricePerMeter, decimal? discountPercent, double? lengthCm) =>
{
    if (pricePerMeter < 0 || discountPercent is < 0 or > 100) return Results.BadRequest("Price must be >= 0, discount 0-100 %.");
    var p = new Purchase { PricePerMeter = pricePerMeter, DiscountPercent = discountPercent, LengthCm = lengthCm ?? 0 };
    return Results.Ok(new { p.FinalPricePerMeter, TotalPrice = lengthCm > 0 ? p.TotalPrice : null });
});

// --- Purchases: each one adds a full-width piece of the bought length ---

api.MapPost("/materials/{id:int}/purchases", async (int id, PurchaseInput input, Db db) =>
{
    if (input.Validate() is { } error) return Results.BadRequest(error);
    var m = await Full(db).FirstOrDefaultAsync(m => m.Id == id);
    if (m == null) return Results.NotFound();
    if (m.RollWidthCm is not > 0) return Results.BadRequest("Set the material's roll width first.");
    var purchase = new Purchase { MaterialId = id };
    input.Apply(purchase);
    m.Purchases.Add(purchase);
    await db.SaveChangesAsync(); // purchase id for the piece
    m.Pieces.Add(new Piece { PurchaseId = purchase.Id, Shape = [new Rect(0, 0, m.RollWidthCm.Value, purchase.LengthCm)] });
    await db.SaveChangesAsync();
    return Results.Ok(View(m));
});

api.MapPut("/purchases/{id:int}", async (int id, PurchaseInput input, Db db) =>
{
    if (input.Validate() is { } error) return Results.BadRequest(error);
    var purchase = await db.Purchases.FindAsync(id);
    if (purchase == null) return Results.NotFound();
    var m = await Full(db).FirstAsync(m => m.Id == purchase.MaterialId);
    var cut = m.Cuts.Any(c => c.PurchaseId == id);
    if (cut && input.LengthCm != purchase.LengthCm) return Results.BadRequest("Length can't change after the first cut.");
    input.Apply(purchase);
    if (!cut) m.Pieces.Single(p => p.PurchaseId == id).Shape = [new Rect(0, 0, m.RollWidthCm!.Value, purchase.LengthCm)];
    await db.SaveChangesAsync();
    return Results.Ok(View(m));
});

api.MapDelete("/purchases/{id:int}", async (int id, Db db) =>
{
    var purchase = await db.Purchases.FindAsync(id);
    if (purchase == null) return Results.NotFound();
    if (await db.Cuts.AnyAsync(c => c.PurchaseId == id)) return Results.BadRequest("A purchase can only be deleted before it's cut.");
    await db.Pieces.Where(p => p.PurchaseId == id).ExecuteDeleteAsync();
    db.Purchases.Remove(purchase);
    await db.SaveChangesAsync();
    return Results.Ok(View(await Full(db).FirstAsync(m => m.Id == purchase.MaterialId)));
});

api.MapGet("/materials/{id:int}/photo", async (int id, Db db) =>
    PhotoResult(await db.Materials.Where(m => m.Id == id).Select(m => new Photo(m.Photo, m.PhotoContentType)).FirstOrDefaultAsync()));

api.MapPut("/materials/{id:int}/photo", async (int id, IFormFile file, Db db) =>
{
    var m = await db.Materials.FindAsync(id);
    if (m == null) return Results.NotFound();
    if (await ReadPhoto(file) is not { } bytes) return Results.BadRequest("Only image files up to 10 MB.");
    (m.Photo, m.PhotoContentType) = (bytes, file.ContentType);
    m.PreviewColor = ImageColor.AverageCenter(bytes);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).DisableAntiforgery(); // local single-user app, no cookies/auth

static IResult PhotoResult(Photo? p) =>
    p?.Bytes is { } bytes ? Results.File(bytes, p.ContentType ?? "image/jpeg") : Results.NotFound();

static async Task<byte[]?> ReadPhoto(IFormFile file)
{
    if (!file.ContentType.StartsWith("image/") || file.Length > 10_000_000) return null;
    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    return ms.ToArray();
}

// --- Projects ---

object ProjectView(Project p, IEnumerable<Cut> cuts) => new
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
    Fibers = FiberView(Fibers.ByWeight(cuts.Select(c => (c.W * c.H, c.Material!.WeightGsm, c.Material.Composition)))),
    // Cuts left out of the fiber calculation because their material has no g/m² or no composition.
    UncountedCuts = cuts.Count(c => c.Material!.WeightGsm is not > 0 || c.Material.Composition is not { Count: > 0 }),
    Care = ProjectCare(cuts.Select(c => c.Material!).DistinctBy(m => m.Id).ToList()),
};

static decimal? CutCost(Cut c) => c.Purchase!.CostOf(c.W * c.H, c.Material!.RollWidthCm);

static IEnumerable<object> FiberView(List<(Fiber Fiber, double Grams)> fibers)
{
    var total = fibers.Sum(f => f.Grams);
    return fibers.Select(f => new { f.Fiber, f.Grams, Percent = f.Grams / total * 100 });
}

// Care for the finished product, plus the materials that have no value set for each category.
object ProjectCare(List<Material> ms) => new
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

async Task<object?> LoadProject(int id, Db db) =>
    await db.Projects.FindAsync(id) is { } p
        ? ProjectView(p, await db.Cuts.Include(c => c.Material).Include(c => c.Purchase).Where(c => c.ProjectId == id).ToListAsync())
        : null;

api.MapGet("/projects", async (Db db) =>
{
    var cuts = await db.Cuts.Include(c => c.Material).Include(c => c.Purchase).Where(c => c.ProjectId != null).ToListAsync();
    return (await db.Projects.OrderBy(p => p.Name).ToListAsync()).Select(p => ProjectView(p, cuts.Where(c => c.ProjectId == p.Id)));
});

api.MapGet("/projects/{id:int}", async (int id, Db db) => await LoadProject(id, db) is { } v ? Results.Ok(v) : Results.NotFound());

api.MapPost("/projects", async (ProjectInput input, Db db) =>
{
    if (string.IsNullOrWhiteSpace(input.Name)) return Results.BadRequest("Name is required.");
    var p = new Project { Name = input.Name };
    input.Apply(p);
    db.Projects.Add(p);
    await db.SaveChangesAsync();
    return Results.Ok(await LoadProject(p.Id, db));
});

api.MapPut("/projects/{id:int}", async (int id, ProjectInput input, Db db) =>
{
    if (string.IsNullOrWhiteSpace(input.Name)) return Results.BadRequest("Name is required.");
    var p = await db.Projects.FindAsync(id);
    if (p == null) return Results.NotFound();
    input.Apply(p);
    await db.SaveChangesAsync();
    return Results.Ok(await LoadProject(id, db));
});

// Cuts of a deleted project are kept, just without a project.
api.MapDelete("/projects/{id:int}", async (int id, Db db) =>
{
    await db.Cuts.Where(c => c.ProjectId == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.ProjectId, (int?)null));
    return await db.Projects.Where(p => p.Id == id).ExecuteDeleteAsync() > 0 ? Results.NoContent() : Results.NotFound();
});

api.MapGet("/projects/{id:int}/photo", async (int id, Db db) =>
    PhotoResult(await db.Projects.Where(p => p.Id == id).Select(p => new Photo(p.Photo, p.PhotoContentType)).FirstOrDefaultAsync()));

api.MapPut("/projects/{id:int}/photo", async (int id, IFormFile file, Db db) =>
{
    var p = await db.Projects.FindAsync(id);
    if (p == null) return Results.NotFound();
    if (await ReadPhoto(file) is not { } bytes) return Results.BadRequest("Only image files up to 10 MB.");
    (p.Photo, p.PhotoContentType) = (bytes, file.ContentType);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).DisableAntiforgery();

// --- Cuts ---

api.MapPost("/pieces/{id:int}/cuts", async (int id, CutInput input, Db db) =>
{
    var piece = await db.Pieces.FirstOrDefaultAsync(p => p.Id == id && p.Active);
    if (piece == null) return Results.NotFound();
    var (leftovers, error) = Geometry.Cut(piece.Shape, new Rect(input.X, input.Y, input.W, input.H));
    if (error != null) return Results.BadRequest(error);
    if (input.ProjectId is { } pid && await db.Projects.FindAsync(pid) == null) return Results.BadRequest("Project not found.");

    var created = leftovers!.Select(s => new Piece { MaterialId = piece.MaterialId, PurchaseId = piece.PurchaseId, Shape = s }).ToList();
    db.Pieces.AddRange(created);
    piece.Active = false;
    await db.SaveChangesAsync(); // assigns ids to the new pieces
    db.Cuts.Add(new Cut
    {
        MaterialId = piece.MaterialId, SourcePieceId = piece.Id, PurchaseId = piece.PurchaseId, X = input.X, Y = input.Y, W = input.W, H = input.H,
        Label = input.Label, ProjectId = input.ProjectId, ResultPieceIds = created.Select(p => p.Id).ToList(),
    });
    await db.SaveChangesAsync();
    return Results.Ok(View(await Full(db).FirstAsync(m => m.Id == piece.MaterialId)));
});

api.MapDelete("/materials/{id:int}/cuts/last", async (int id, Db db) =>
{
    var cut = await db.Cuts.Where(c => c.MaterialId == id).OrderByDescending(c => c.Id).FirstOrDefaultAsync();
    if (cut == null) return Results.NotFound();
    await db.Pieces.Where(p => cut.ResultPieceIds.Contains(p.Id)).ExecuteDeleteAsync();
    await db.Pieces.Where(p => p.Id == cut.SourcePieceId).ExecuteUpdateAsync(s => s.SetProperty(p => p.Active, true));
    db.Cuts.Remove(cut);
    await db.SaveChangesAsync();
    return Results.Ok(View(await Full(db).FirstAsync(m => m.Id == id)));
});

app.Run();

public record CutInput(double X, double Y, double W, double H, string? Label, int? ProjectId);

public record Photo(byte[]? Bytes, string? ContentType);

public record ProjectInput(
    string Name, ProjectStatus Status, DateOnly? StartDate, string? Notes,
    Wash? Wash, Bleach? Bleach, Iron? Iron, DryClean? DryClean, TumbleDry? TumbleDry)
{
    public void Apply(Project p)
    {
        (p.Name, p.Status, p.StartDate, p.Notes) = (Name.Trim(), Status, StartDate, Notes);
        (p.Wash, p.Bleach, p.Iron, p.DryClean, p.TumbleDry) = (Wash, Bleach, Iron, DryClean, TumbleDry);
    }
}

public record PurchaseInput(double LengthCm, decimal? PricePerMeter, decimal? DiscountPercent, DateOnly? PurchaseDate)
{
    public string? Validate() =>
        LengthCm <= 0 ? "Length must be > 0."
        : PricePerMeter < 0 ? "Price can't be negative."
        : DiscountPercent is < 0 or > 100 ? "Discount must be 0-100 %."
        : null;

    public void Apply(Purchase p) =>
        (p.LengthCm, p.PricePerMeter, p.DiscountPercent, p.PurchaseDate) = (LengthCm, PricePerMeter, DiscountPercent, PurchaseDate);
}

public record MaterialInput(
    string Name, string? Color, List<FiberPart>? Composition, double? RollWidthCm, int? WeightGsm,
    string? NeedleType, int? NeedleSize, string? Certificate, Wash? Wash, Bleach? Bleach, Iron? Iron,
    DryClean? DryClean, TumbleDry? TumbleDry, string? Link, string? Store)
{
    public string? Validate() =>
        string.IsNullOrWhiteSpace(Name) ? "Name is required."
        : Fibers.Validate(Composition);

    public void Apply(Material m)
    {
        m.Name = Name.Trim(); m.Color = Color; m.Composition = Composition is { Count: > 0 } ? Composition : null; m.RollWidthCm = RollWidthCm;
        m.WeightGsm = WeightGsm; m.NeedleType = NeedleType; m.NeedleSize = NeedleSize; m.Certificate = Certificate;
        m.Wash = Wash; m.Bleach = Bleach; m.Iron = Iron; m.DryClean = DryClean; m.TumbleDry = TumbleDry;
        m.Link = Link; m.Store = Store;
    }
}
