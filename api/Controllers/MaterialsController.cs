using MaterialManagement.Api.Dtos;
using MaterialManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaterialManagement.Api.Controllers;

[ApiController, Route("api/materials")]
public class MaterialsController(MaterialService materials) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await materials.List());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => Ok(await materials.Get(id));

    [HttpPost]
    public async Task<IActionResult> Create(MaterialInput input) => Ok(await materials.Create(input));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, MaterialInput input) => Ok(await materials.Update(id, input));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await materials.Delete(id);
        return NoContent();
    }

    [HttpGet("{id:int}/photo")]
    public async Task<IActionResult> GetPhoto(int id)
    {
        var p = await materials.GetPhoto(id);
        return File(p.Bytes, p.ContentType);
    }

    [HttpPut("{id:int}/photo")]
    public async Task<IActionResult> SetPhoto(int id, IFormFile file)
    {
        await materials.SetPhoto(id, file);
        return NoContent();
    }
}
