namespace BuildingBlocks.ApiResponses;

public sealed record ApiResponse<T>(
    bool Succeeded,
    T? Data,
    ApiError? Error,
    string TraceId)
{
    public static ApiResponse<T> Success(T? data, string traceId) =>
        new(true, data, null, traceId);

    public static ApiResponse<T> Failure(ApiError error, string traceId) =>
        new(false, default, error, traceId);
}
