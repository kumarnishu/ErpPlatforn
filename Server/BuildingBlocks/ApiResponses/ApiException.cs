namespace BuildingBlocks.ApiResponses;

/// <summary>An expected API failure with a safe message suitable for a client response.</summary>
public sealed class ApiException(
    int statusCode,
    string code,
    string publicMessage,
    IReadOnlyDictionary<string, string[]>? details = null) : Exception(publicMessage)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public string PublicMessage { get; } = publicMessage;
    public IReadOnlyDictionary<string, string[]>? Details { get; } = details;
}
