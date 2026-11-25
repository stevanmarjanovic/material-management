using MaterialManagement.Api.Data;
using MaterialManagement.Api.Dtos;
using MaterialManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MaterialManagement.Api.Services;

/// <summary>Each purchase adds a full-width piece of the bought length.</summary>
public class PurchaseService(Db db, MaterialService materials)
{
    /// <summary>Price after discount for values not saved yet (live preview in forms). Same formula as a saved purchase.</summary>
    public static object Preview(decimal pricePerMeter, decimal? discountPercent, double? lengthCm)
    {
        if (pricePerMeter < 0 || discountPercent is < 0 or > 100) throw new BadRequestException("Price must be >= 0, discount 0-100 %.");
        var p = new Purchase { PricePerMeter = pricePerMeter, DiscountPercent = discountPercent, LengthCm = lengthCm ?? 0 };
        return new { p.FinalPricePerMeter, TotalPrice = lengthCm > 0 ? p.TotalPrice : null };
    }

    public async Task<object> Create(int materialId, PurchaseInput input)
    {
        BadRequestException.ThrowIf(input.Validate());
        var m = await materials.Load(materialId);
        if (m.RollWidthCm is not > 0) throw new BadRequestException("Set the material's roll width first.");
        var purchase = new Purchase { MaterialId = materialId };
        input.Apply(purchase);
        m.Purchases.Add(purchase);
        await db.SaveChangesAsync(); // purchase id for the piece
        m.Pieces.Add(new Piece { PurchaseId = purchase.Id, Shape = [new Rect(0, 0, m.RollWidthCm.Value, purchase.LengthCm)] });
        await db.SaveChangesAsync();
        return MaterialView.From(m);
    }

    public async Task<object> Update(int id, PurchaseInput input)
    {
        BadRequestException.ThrowIf(input.Validate());
        var purchase = await db.Purchases.FindAsync(id) ?? throw new NotFoundException();
        var m = await materials.Load(purchase.MaterialId);
        var cut = m.Cuts.Any(c => c.PurchaseId == id);
        if (cut && input.LengthCm != purchase.LengthCm) throw new BadRequestException("Length can't change after the first cut.");
        input.Apply(purchase);
        if (!cut) m.Pieces.Single(p => p.PurchaseId == id).Shape = [new Rect(0, 0, m.RollWidthCm!.Value, purchase.LengthCm)];
        await db.SaveChangesAsync();
        return MaterialView.From(m);
    }

    public async Task<object> Delete(int id)
    {
        var purchase = await db.Purchases.FindAsync(id) ?? throw new NotFoundException();
        if (await db.Cuts.AnyAsync(c => c.PurchaseId == id)) throw new BadRequestException("A purchase can only be deleted before it's cut.");
        await db.Pieces.Where(p => p.PurchaseId == id).ExecuteDeleteAsync();
        db.Purchases.Remove(purchase);
        await db.SaveChangesAsync();
        return await materials.Get(purchase.MaterialId);
    }
}
