using MaterialManagement.Api.Data;
using MaterialManagement.Api.Dtos;
using MaterialManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MaterialManagement.Api.Services;

public class StoreService(Db db)
{
    public async Task<IEnumerable<object>> List()
    {
        var materials = await db.Materials.Where(m => m.StoreId != null).ToListAsync();
        return (await db.Stores.OrderBy(s => s.Name).ToListAsync()).Select(s => StoreView.From(s, materials.Where(m => m.StoreId == s.Id)));
    }

    public async Task<object> Get(int id)
    {
        var s = await db.Stores.FindAsync(id) ?? throw new NotFoundException();
        return StoreView.From(s, await db.Materials.Where(m => m.StoreId == id).ToListAsync());
    }

    public async Task<object> Create(StoreInput input)
    {
        BadRequestException.ThrowIf(input.Validate());
        var s = new Store { Name = input.Name };
        input.Apply(s);
        db.Stores.Add(s);
        await db.SaveChangesAsync();
        return await Get(s.Id);
    }

    public async Task<object> Update(int id, StoreInput input)
    {
        BadRequestException.ThrowIf(input.Validate());
        var s = await db.Stores.FindAsync(id) ?? throw new NotFoundException();
        input.Apply(s);
        await db.SaveChangesAsync();
        return await Get(id);
    }

    /// <summary>Materials of a deleted store are kept, just without a store.</summary>
    public async Task Delete(int id)
    {
        await db.Materials.Where(m => m.StoreId == id).ExecuteUpdateAsync(s => s.SetProperty(m => m.StoreId, (int?)null));
        if (await db.Stores.Where(s => s.Id == id).ExecuteDeleteAsync() == 0) throw new NotFoundException();
    }
}
