using System.Text.Json;
using BuildingBlocks.ApiResponses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.RequestAborted.IsCancellationRequested)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (statusCode, error) = exception is ApiException apiException
                ? (apiException.StatusCode, new ApiError(apiException.Code, apiException.PublicMessage, apiException.Details))
                : (StatusCodes.Status500InternalServerError,
                    new ApiError("INTERNAL_ERROR", "An unexpected error occurred."));

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Unhandled request failure. TraceId: {TraceId}", context.TraceIdentifier);
            }
            else
            {
                logger.LogInformation(exception, "Request rejected. TraceId: {TraceId}", context.TraceIdentifier);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<object>.Failure(error, context.TraceIdentifier);
            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                response,
                JsonOptions,
                context.RequestAborted);
        }
    }
}
