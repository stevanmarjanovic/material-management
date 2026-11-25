using MaterialManagement.Api.Data;
using MaterialManagement.Api.Domain;
using MaterialManagement.Api.Dtos;
using MaterialManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MaterialManagement.Api.Services;

public class CutService(Db db, MaterialService materials)
{
    /// <summary>Cuts a rectangle off an active piece; the piece is replaced by its leftovers.</summary>
    public async Task<object> Cut(int pieceId, CutInput input)
    {
        var piece = await db.Pieces.FirstOrDefaultAsync(p => p.Id == pieceId && p.Active) ?? throw new NotFoundException();
        var (leftovers, error) = Geometry.Cut(piece.Shape, new Rect(input.X, input.Y, input.W, input.H));
        BadRequestException.ThrowIf(error);
        if (input.ProjectId is { } pid && await db.Projects.FindAsync(pid) == null) throw new BadRequestException("Project not found.");

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
        return await materials.Get(piece.MaterialId);
    }

    public async Task<object> UndoLast(int materialId)
    {
        var cut = await db.Cuts.Where(c => c.MaterialId == materialId).OrderByDescending(c => c.Id).FirstOrDefaultAsync()
            ?? throw new NotFoundException();
        await db.Pieces.Where(p => cut.ResultPieceIds.Contains(p.Id)).ExecuteDeleteAsync();
        await db.Pieces.Where(p => p.Id == cut.SourcePieceId).ExecuteUpdateAsync(s => s.SetProperty(p => p.Active, true));
        db.Cuts.Remove(cut);
        await db.SaveChangesAsync();
        return await materials.Get(materialId);
    }
}
