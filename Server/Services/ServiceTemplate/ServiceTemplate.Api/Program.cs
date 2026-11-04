using BuildingBlocks.Authentication;
using BuildingBlocks.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSharedRequestContext();
builder.Services.AddSharedJwtAuthentication(
    builder.Configuration["Authentication:Authority"],
    builder.Configuration["Authentication:Audience"]);

var app = builder.Build();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(ApiResponse<object>.Success(
    new { service = "service-template", status = "ok" },
    "health-check")));

app.Run();
