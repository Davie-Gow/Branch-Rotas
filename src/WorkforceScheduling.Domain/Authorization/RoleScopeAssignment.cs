namespace WorkforceScheduling.Domain.Authorization;

/// <summary>
/// A role and scope resolved from approved Entra security-group assignments.
/// Ordinary Microsoft 365 group membership must never create this assignment.
/// </summary>
/// <param name="Role">The operational role granted by the assignment.</param>
/// <param name="ScopeType">The kind of stable scope being granted.</param>
/// <param name="ScopeId">The stable identifier for the scope.</param>
public sealed record RoleScopeAssignment(
    OperationalRole Role,
    ScopeType ScopeType,
    string ScopeId);
