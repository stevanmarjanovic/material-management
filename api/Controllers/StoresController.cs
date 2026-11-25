using MaterialManagement.Api.Dtos;
using MaterialManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaterialManagement.Api.Controllers;

[ApiController, Route("api/stores")]
public class StoresController(StoreService stores) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await stores.List());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => Ok(await stores.Get(id));

    [HttpPost]
    public async Task<IActionResult> Create(StoreInput input) => Ok(await stores.Create(input));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, StoreInput input) => Ok(await stores.Update(id, input));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await stores.Delete(id);
        return NoContent();
    }
}
