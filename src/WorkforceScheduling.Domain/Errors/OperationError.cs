namespace WorkforceScheduling.Domain.Errors;

/// <summary>Safe user-facing error metadata plus a correlation reference for diagnostics.</summary>
/// <param name="Category">The controlled error category.</param>
/// <param name="IsRetryable">Whether retry is permitted by policy.</param>
/// <param name="SafeMessage">A message safe to show to the requesting user.</param>
/// <param name="DiagnosticReference">A protected reference to technical diagnostics.</param>
/// <param name="CorrelationId">The operation correlation identifier.</param>
/// <param name="OriginatingComponent">The component that produced the error.</param>
public sealed record OperationError(
    ErrorCategory Category,
    bool IsRetryable,
    string SafeMessage,
    string DiagnosticReference,
    string CorrelationId,
    string OriginatingComponent);
