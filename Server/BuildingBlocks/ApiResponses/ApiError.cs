namespace BuildingBlocks.ApiResponses;

public sealed record ApiError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Details = null);
