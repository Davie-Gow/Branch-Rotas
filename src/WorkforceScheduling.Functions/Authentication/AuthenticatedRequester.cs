using WorkforceScheduling.Domain.Identifiers;

namespace WorkforceScheduling.Functions.Authentication;

/// <summary>Validated caller identity and group claims from a signed access token.</summary>
public sealed record AuthenticatedRequester(PrincipalId PrincipalId, IReadOnlyCollection<string> SecurityGroupIds);
