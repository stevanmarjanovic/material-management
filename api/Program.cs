using System.Text.Json.Serialization;
using MaterialManagement.Api.Controllers;
using MaterialManagement.Api.Data;
using MaterialManagement.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<Db>(o => o.UseSqlite("Data Source=materials.db"));
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<PurchaseService>();
builder.Services.AddScoped<StoreService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<CutService>();
builder.Services
    // Inputs validate themselves (Validate()), so a missing name gets our message instead of a generic "field is required".
    .AddControllers(o => { o.Filters.Add<ErrorFilter>(); o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true; })
    .ConfigureApiBehaviorOptions(o => o.SuppressMapClientErrors = true) // plain 404s, no ProblemDetails body
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();

// Schema changes: dotnet ef migrations add <Name> --project api
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<Db>().Database.Migrate();
    scope.ServiceProvider.GetRequiredService<MaterialService>().FillMissingPreviewColors();
}

app.MapControllers();
app.Run();
