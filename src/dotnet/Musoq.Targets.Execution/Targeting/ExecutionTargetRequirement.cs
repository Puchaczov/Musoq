namespace Musoq.Targets.Execution;

internal sealed record ExecutionTargetRequirement(
    ExecutionTargetRequirementKind Kind,
    string Detail,
    ExecutionPortableTypeDescriptor? TypeSymbol = null,
    ExecutionPortableCallableDescriptor? CallableSymbol = null);
