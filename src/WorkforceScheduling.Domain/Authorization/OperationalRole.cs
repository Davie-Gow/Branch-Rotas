namespace WorkforceScheduling.Domain.Authorization;

/// <summary>Roles that may administer ordinary staff membership through the control plane.</summary>
public enum OperationalRole
{
    /// <summary>May administer ordinary membership for explicitly assigned branches.</summary>
    BranchManager,
    /// <summary>May administer ordinary membership for explicitly assigned franchises.</summary>
    FranchiseManager,
    /// <summary>May administer ordinary membership across the explicitly assigned trial scope.</summary>
    PlatformSupportAdmin
}
