using MaterialManagement.Api.Dtos;
using MaterialManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaterialManagement.Api.Controllers;

[ApiController, Route("api")]
public class PurchasesController(PurchaseService purchases) : ControllerBase
{
    [HttpGet("prices")]
    public IActionResult Preview(decimal pricePerMeter, decimal? discountPercent, double? lengthCm) =>
        Ok(PurchaseService.Preview(pricePerMeter, discountPercent, lengthCm));

    [HttpPost("materials/{materialId:int}/purchases")]
    public async Task<IActionResult> Create(int materialId, PurchaseInput input) => Ok(await purchases.Create(materialId, input));

    [HttpPut("purchases/{id:int}")]
    public async Task<IActionResult> Update(int id, PurchaseInput input) => Ok(await purchases.Update(id, input));

    [HttpDelete("purchases/{id:int}")]
    public async Task<IActionResult> Delete(int id) => Ok(await purchases.Delete(id));
}
