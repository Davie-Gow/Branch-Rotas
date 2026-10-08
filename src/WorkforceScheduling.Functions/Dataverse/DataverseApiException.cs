namespace WorkforceScheduling.Functions.Dataverse;

/// <summary>Dataverse HTTP failure with a safe service code, never raw exception text.</summary>
public sealed class DataverseApiException(int statusCode, string errorCode)
    : Exception($"Dataverse returned HTTP {statusCode} ({errorCode}).")
{
    public int StatusCode { get; } = statusCode;

    public string ErrorCode { get; } = errorCode;
}
