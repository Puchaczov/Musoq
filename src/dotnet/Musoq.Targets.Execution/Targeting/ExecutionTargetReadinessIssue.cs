namespace Musoq.Targets.Execution;

internal sealed record ExecutionTargetReadinessIssue(
    ExecutionTargetRuntimeFamily RuntimeFamily,
    ExecutionTargetReadinessCategory Category,
    ExecutionTargetRequirement Requirement,
    string Diagnostic);
