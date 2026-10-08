namespace WorkforceScheduling.Functions.Graph;

/// <summary>Safe Graph error metadata; response bodies are never surfaced to users or logs.</summary>
public sealed class GraphApiException(int statusCode, string errorCode)
    : Exception($"Microsoft Graph returned HTTP {statusCode} ({errorCode}).")
{
    public int StatusCode { get; } = statusCode;

    public string ErrorCode { get; } = errorCode;

    public bool IsRetryable => StatusCode is 429 or 503 or 504;
}
