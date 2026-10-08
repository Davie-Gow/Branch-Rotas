namespace WorkforceScheduling.Functions.Locks;

/// <summary>Indicates another operation currently owns the branch's distributed lease.</summary>
public sealed class BranchOperationConflictException()
    : Exception("A branch operation is already running.");
