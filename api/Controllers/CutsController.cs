using MaterialManagement.Api.Dtos;
using MaterialManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaterialManagement.Api.Controllers;

[ApiController, Route("api")]
public class CutsController(CutService cuts) : ControllerBase
{
    [HttpPost("pieces/{pieceId:int}/cuts")]
    public async Task<IActionResult> Cut(int pieceId, CutInput input) => Ok(await cuts.Cut(pieceId, input));

    [HttpDelete("materials/{materialId:int}/cuts/last")]
    public async Task<IActionResult> UndoLast(int materialId) => Ok(await cuts.UndoLast(materialId));
}
