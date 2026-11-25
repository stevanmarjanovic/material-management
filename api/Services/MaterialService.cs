using MaterialManagement.Api.Data;
using MaterialManagement.Api.Domain;
using MaterialManagement.Api.Dtos;
using MaterialManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MaterialManagement.Api.Services;

public class MaterialService(Db db)
{
    public async Task<IEnumerable<object>> List() =>
        (await db.MaterialsFull.OrderBy(m => m.Name).ToListAsync()).Select(MaterialView.From);

    public async Task<object> Get(int id) => MaterialView.From(await Load(id));

    /// <summary>The material with everything its view needs; 404 if missing.</summary>
    public async Task<Material> Load(int id) =>
        await db.MaterialsFull.FirstOrDefaultAsync(m => m.Id == id) ?? throw new NotFoundException();

    public async Task<object> Create(MaterialInput input)
    {
        BadRequestException.ThrowIf(input.Validate());
        if (input.RollWidthCm is not > 0) throw new BadRequestException("Roll width must be > 0.");
        await CheckStore(input.StoreId);
        var m = new Material { Name = input.Name };
        input.Apply(m);
        db.Materials.Add(m);
        await db.SaveChangesAsync();
        return MaterialView.From(m);
    }

    public async Task<object> Update(int id, MaterialInput input)
    {
        BadRequestException.ThrowIf(input.Validate());
        var m = await Load(id);
        await CheckStore(input.StoreId);
        // The roll width is the width of every bought piece, so it can only change while nothing is cut.
        var uncut = m.Cuts.Count == 0;
        if (input.RollWidthCm != m.RollWidthCm && !uncut) throw new BadRequestException("Roll width can't change after the first cut.");
        if (input.RollWidthCm is not > 0) throw new BadRequestException("Roll width must be > 0.");
        input.Apply(m);
        await db.Entry(m).Reference(x => x.Store).LoadAsync(); // new store name for the response
        if (uncut)
            foreach (var piece in m.Pieces)
                piece.Shape = [new Rect(0, 0, input.RollWidthCm.Value, m.Purchases.First(p => p.Id == piece.PurchaseId).LengthCm)];
        await db.SaveChangesAsync();
        return MaterialView.From(m);
    }

    public async Task Delete(int id)
    {
        await db.Cuts.Where(c => c.MaterialId == id).ExecuteDeleteAsync();
        await db.Pieces.Where(p => p.MaterialId == id).ExecuteDeleteAsync();
        await db.Purchases.Where(p => p.MaterialId == id).ExecuteDeleteAsync();
        if (await db.Materials.Where(m => m.Id == id).ExecuteDeleteAsync() == 0) throw new NotFoundException();
    }

    public async Task<Photo> GetPhoto(int id) =>
        await db.Materials.Where(m => m.Id == id && m.Photo != null)
            .Select(m => new Photo(m.Photo!, m.PhotoContentType ?? "image/jpeg")).FirstOrDefaultAsync()
        ?? throw new NotFoundException();

    public async Task SetPhoto(int id, IFormFile file)
    {
        var m = await db.Materials.FindAsync(id) ?? throw new NotFoundException();
        var bytes = await PhotoService.Read(file);
        (m.Photo, m.PhotoContentType) = (bytes, file.ContentType);
        m.PreviewColor = ImageColor.AverageCenter(bytes);
        await db.SaveChangesAsync();
    }

    /// <summary>Photos without a preview color yet (uploaded before it existed, or reset by a migration).</summary>
    public void FillMissingPreviewColors()
    {
        foreach (var m in db.Materials.Where(m => m.Photo != null && m.PreviewColor == null).ToList())
            m.PreviewColor = ImageColor.AverageCenter(m.Photo!);
        db.SaveChanges();
    }

    async Task CheckStore(int? storeId)
    {
        if (storeId is { } sid && await db.Stores.FindAsync(sid) == null) throw new BadRequestException("Store not found.");
    }
}
