using System.Text.Json.Serialization;
using MaterialManagement.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<Db>(o => o.UseSqlite("Data Source=materials.db"));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();

// Schema changes: dotnet ef migrations add <Name> --project api
using (var scope = app.Services.CreateScope()) scope.ServiceProvider.GetRequiredService<Db>().Database.Migrate();

var api = app.MapGroup("/api");

object View(Material m) => new
{
    m.Id, m.Name, m.Color, m.Composition, m.RollWidthCm, m.WeightGsm, m.NeedleType, m.NeedleSize,
    m.Certificate, m.Wash, m.Bleach, m.Iron, m.DryClean, m.TumbleDry, m.Link, m.Store, m.PurchaseDate,
    m.LengthCm, m.PricePerMeter, m.DiscountPercent, m.FinalPricePerMeter, m.TotalPrice,
    HasPhoto = m.Photo != null, m.PreviewColor,
    Pieces = m.Pieces.Where(p => p.Active).OrderBy(p => p.Id).Select(p => new { p.Id, p.Shape, Area = Geometry.Area(p.Shape) }),
    Cuts = m.Cuts.OrderByDescending(c => c.Id).Select(c => new
    {
        c.Id, c.SourcePieceId, c.X, c.Y, c.W, c.H, c.Label, c.CreatedAt, c.ProjectId, ProjectName = c.Project?.Name,
        Area = c.W * c.H, Cost = m.CostOf(c.W * c.H),
    }),
};

IQueryable<Material> Full(Db db) => db.Materials.Include(m => m.Pieces).Include(m => m.Cuts).ThenInclude(c => c.Project);

api.MapGet("/materials", async (Db db) =>
    (await Full(db).OrderBy(m => m.Name).ToListAsync()).Select(View));

api.MapGet("/materials/{id:int}", async (int id, Db db) =>
    await Full(db).FirstOrDefaultAsync(m => m.Id == id) is { } m ? Results.Ok(View(m)) : Results.NotFound());

api.MapPost("/materials", async (MaterialInput input, Db db) =>
{
    if (input.Validate() is { } error) return Results.BadRequest(error);
    if (input.RollWidthCm is not > 0 || input.LengthCm is not > 0) return Results.BadRequest("Roll width and length must be > 0.");
    var m = new Material { Name = input.Name, LengthCm = input.LengthCm.Value };
    input.Apply(m);
    m.Pieces.Add(new Piece { Shape = [new Rect(0, 0, input.RollWidthCm.Value, input.LengthCm.Value)] });
    db.Materials.Add(m);
    await db.SaveChangesAsync();
    return Results.Ok(View(m));
});

api.MapPut("/materials/{id:int}", async (int id, MaterialInput input, Db db) =>
{
    if (input.Validate() is { } error) return Results.BadRequest(error);
    var m = await Full(db).FirstOrDefaultAsync(m => m.Id == id);
    if (m == null) return Results.NotFound();
    // The roll width is the first piece's width, so it can only change while that piece is uncut.
    var uncut = m.Cuts.Count == 0;
    if (input.RollWidthCm != m.RollWidthCm && !uncut) return Results.BadRequest("Roll width can't change after the first cut.");
    if (input.RollWidthCm is not > 0) return Results.BadRequest("Roll width must be > 0.");
    input.Apply(m);
    if (uncut) m.Pieces[0].Shape = [new Rect(0, 0, input.RollWidthCm.Value, m.LengthCm)];
    await db.SaveChangesAsync();
    return Results.Ok(View(m));
});

api.MapDelete("/materials/{id:int}", async (int id, Db db) =>
{
    await db.Pieces.Where(p => p.MaterialId == id).ExecuteDeleteAsync();
    await db.Cuts.Where(c => c.MaterialId == id).ExecuteDeleteAsync();
    return await db.Materials.Where(m => m.Id == id).ExecuteDeleteAsync() > 0 ? Results.NoContent() : Results.NotFound();
});

api.MapGet("/materials/{id:int}/photo", async (int id, Db db) =>
    PhotoResult(await db.Materials.Where(m => m.Id == id).Select(m => new Photo(m.Photo, m.PhotoContentType)).FirstOrDefaultAsync()));

api.MapPut("/materials/{id:int}/photo", async (int id, IFormFile file, Db db) =>
{
    var m = await db.Materials.FindAsync(id);
    if (m == null) return Results.NotFound();
    if (await ReadPhoto(file) is not { } bytes) return Results.BadRequest("Only image files up to 10 MB.");
    (m.Photo, m.PhotoContentType) = (bytes, file.ContentType);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).DisableAntiforgery(); // local single-user app, no cookies/auth

// The browser averages the photo's pixels (no image library needed on the server).
api.MapPut("/materials/{id:int}/preview-color", async (int id, ColorInput input, Db db) =>
{
    if (!System.Text.RegularExpressions.Regex.IsMatch(input.PreviewColor ?? "", "^#[0-9a-fA-F]{6}$"))
        return Results.BadRequest("Color must look like #a1b2c3.");
    return await db.Materials.Where(m => m.Id == id).ExecuteUpdateAsync(s => s.SetProperty(m => m.PreviewColor, input.PreviewColor)) > 0
        ? Results.NoContent() : Results.NotFound();
});

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
    Cuts = cuts.OrderBy(c => c.Material!.Name).ThenBy(c => c.Id).Select(c => new
    {
        c.Id, c.MaterialId, MaterialName = c.Material!.Name, MaterialColor = c.Material.Color, c.W, c.H, c.Label, c.CreatedAt,
        Area = c.W * c.H, Cost = c.Material.CostOf(c.W * c.H),
    }),
};

async Task<object?> LoadProject(int id, Db db) =>
    await db.Projects.FindAsync(id) is { } p
        ? ProjectView(p, await db.Cuts.Include(c => c.Material).Where(c => c.ProjectId == id).ToListAsync())
        : null;

api.MapGet("/projects", async (Db db) =>
{
    var cuts = await db.Cuts.Include(c => c.Material).Where(c => c.ProjectId != null).ToListAsync();
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

    var created = leftovers!.Select(s => new Piece { MaterialId = piece.MaterialId, Shape = s }).ToList();
    db.Pieces.AddRange(created);
    piece.Active = false;
    await db.SaveChangesAsync(); // assigns ids to the new pieces
    db.Cuts.Add(new Cut
    {
        MaterialId = piece.MaterialId, SourcePieceId = piece.Id, X = input.X, Y = input.Y, W = input.W, H = input.H,
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

public record ColorInput(string? PreviewColor);

public record ProjectInput(string Name, ProjectStatus Status, DateOnly? StartDate, string? Notes)
{
    public void Apply(Project p) => (p.Name, p.Status, p.StartDate, p.Notes) = (Name.Trim(), Status, StartDate, Notes);
}

public record MaterialInput(
    string Name, string? Color, string? Composition, double? RollWidthCm, int? WeightGsm,
    string? NeedleType, int? NeedleSize, string? Certificate, Wash? Wash, Bleach? Bleach, Iron? Iron,
    DryClean? DryClean, TumbleDry? TumbleDry, string? Link, decimal? PricePerMeter, decimal? DiscountPercent,
    string? Store, DateOnly? PurchaseDate, double? LengthCm)
{
    public string? Validate() =>
        string.IsNullOrWhiteSpace(Name) ? "Name is required."
        : DiscountPercent is < 0 or > 100 ? "Discount must be 0-100 %."
        : null;

    public void Apply(Material m)
    {
        m.Name = Name.Trim(); m.Color = Color; m.Composition = Composition; m.RollWidthCm = RollWidthCm;
        m.WeightGsm = WeightGsm; m.NeedleType = NeedleType; m.NeedleSize = NeedleSize; m.Certificate = Certificate;
        m.Wash = Wash; m.Bleach = Bleach; m.Iron = Iron; m.DryClean = DryClean; m.TumbleDry = TumbleDry;
        m.Link = Link; m.PricePerMeter = PricePerMeter; m.DiscountPercent = DiscountPercent; m.Store = Store; m.PurchaseDate = PurchaseDate;
    }
}
