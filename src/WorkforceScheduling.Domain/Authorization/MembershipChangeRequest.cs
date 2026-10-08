using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Domain.Authorization;

/// <summary>Auditable intent to add or remove an ordinary member from one branch group.</summary>
/// <param name="OperationId">Unique idempotency and orchestration identifier.</param>
/// <param name="BranchId">Stable branch key.</param>
/// <param name="UserObjectId">The target user's Entra object identifier.</param>
/// <param name="Action">The requested membership operation.</param>
/// <param name="RequestedBy">The authenticated requesting principal.</param>
public sealed record MembershipChangeRequest(
    string OperationId,
    BranchId BranchId,
    string UserObjectId,
    MembershipChangeAction Action,
    PrincipalId RequestedBy);
