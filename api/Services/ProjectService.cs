using MaterialManagement.Api.Data;
using MaterialManagement.Api.Dtos;
using MaterialManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MaterialManagement.Api.Services;

public class ProjectService(Db db)
{
    public async Task<IEnumerable<object>> List()
    {
        var cuts = await db.CutsFull.Where(c => c.ProjectId != null).ToListAsync();
        return (await db.Projects.OrderBy(p => p.Name).ToListAsync()).Select(p => ProjectView.From(p, cuts.Where(c => c.ProjectId == p.Id)));
    }

    public async Task<object> Get(int id)
    {
        var p = await db.Projects.FindAsync(id) ?? throw new NotFoundException();
        return ProjectView.From(p, await db.CutsFull.Where(c => c.ProjectId == id).ToListAsync());
    }

    public async Task<object> Create(ProjectInput input)
    {
        BadRequestException.ThrowIf(input.Validate());
        var p = new Project { Name = input.Name };
        input.Apply(p);
        db.Projects.Add(p);
        await db.SaveChangesAsync();
        return await Get(p.Id);
    }

    public async Task<object> Update(int id, ProjectInput input)
    {
        BadRequestException.ThrowIf(input.Validate());
        var p = await db.Projects.FindAsync(id) ?? throw new NotFoundException();
        input.Apply(p);
        await db.SaveChangesAsync();
        return await Get(id);
    }

    /// <summary>Cuts of a deleted project are kept, just without a project.</summary>
    public async Task Delete(int id)
    {
        await db.Cuts.Where(c => c.ProjectId == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.ProjectId, (int?)null));
        if (await db.Projects.Where(p => p.Id == id).ExecuteDeleteAsync() == 0) throw new NotFoundException();
    }

    public async Task<Photo> GetPhoto(int id) =>
        await db.Projects.Where(p => p.Id == id && p.Photo != null)
            .Select(p => new Photo(p.Photo!, p.PhotoContentType ?? "image/jpeg")).FirstOrDefaultAsync()
        ?? throw new NotFoundException();

    public async Task SetPhoto(int id, IFormFile file)
    {
        var p = await db.Projects.FindAsync(id) ?? throw new NotFoundException();
        (p.Photo, p.PhotoContentType) = (await PhotoService.Read(file), file.ContentType);
        await db.SaveChangesAsync();
    }
}
