using MaterialManagement.Api.Dtos;
using MaterialManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaterialManagement.Api.Controllers;

[ApiController, Route("api/projects")]
public class ProjectsController(ProjectService projects) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await projects.List());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => Ok(await projects.Get(id));

    [HttpPost]
    public async Task<IActionResult> Create(ProjectInput input) => Ok(await projects.Create(input));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProjectInput input) => Ok(await projects.Update(id, input));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await projects.Delete(id);
        return NoContent();
    }

    [HttpGet("{id:int}/photo")]
    public async Task<IActionResult> GetPhoto(int id)
    {
        var p = await projects.GetPhoto(id);
        return File(p.Bytes, p.ContentType);
    }

    [HttpPut("{id:int}/photo")]
    public async Task<IActionResult> SetPhoto(int id, IFormFile file)
    {
        await projects.SetPhoto(id, file);
        return NoContent();
    }
}
