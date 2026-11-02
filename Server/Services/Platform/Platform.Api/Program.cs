using BuildingBlocks.Authentication;
using BuildingBlocks.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSharedRequestContext();

var app = builder.Build();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapGet("/health", () => Results.Ok(ApiResponse<object>.Success(
    new { service = "platform", status = "ok" },
    "health-check")));

app.Run();
