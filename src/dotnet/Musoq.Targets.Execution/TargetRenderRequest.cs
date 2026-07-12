using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Targets.Execution;

internal sealed record TargetRenderIdentity(string CompilationUnitName);

internal sealed record TargetRenderOptions
{
    public TargetRenderOptions(IReadOnlyDictionary<string, string>? values)
    {
        Values = TargetRenderContractImmutability.FreezeDictionary(values);
    }

    public IReadOnlyDictionary<string, string> Values { get; }

    public static TargetRenderOptions Empty { get; } = new((IReadOnlyDictionary<string, string>?)null);
}

internal sealed record TargetScriptBindingContract
{
    public TargetScriptBindingContract(
        IEnumerable<string>? parameterNames,
        IEnumerable<string>? variableNames)
    {
        ParameterNames = TargetRenderContractImmutability.Freeze(parameterNames);
        VariableNames = TargetRenderContractImmutability.Freeze(variableNames);
    }

    public IReadOnlyList<string> ParameterNames { get; }

    public IReadOnlyList<string> VariableNames { get; }

    public static TargetScriptBindingContract Empty { get; } = new([], []);
}

internal sealed record TargetReferenceInventory
{
    public TargetReferenceInventory(IEnumerable<string>? referenceNames)
    {
        ReferenceNames = TargetRenderContractImmutability.Freeze(referenceNames);
    }

    public IReadOnlyList<string> ReferenceNames { get; }

    public static TargetReferenceInventory Empty { get; } = new([]);
}

internal abstract record TargetBackendRenderInputs(ExecutionTargetId TargetId);

internal sealed record EmptyTargetBackendRenderInputs(ExecutionTargetId TargetId)
    : TargetBackendRenderInputs(TargetId);

internal sealed record TargetRenderRequest
{
    public required ExecutionTargetId TargetId { get; init; }

    public required TargetRenderIdentity Identity { get; init; }

    public required TargetRenderOptions Options { get; init; }

    public required TargetScriptBindingContract ScriptBinding { get; init; }

    public required TargetReferenceInventory References { get; init; }

    public required ExecutionPlan ExecutionPlan { get; init; }

    public required int ExecutionIrVersion { get; init; }

    public required ExecutionSemanticsContract SemanticsContract { get; init; }

    public required ExecutionTargetOperationReport OperationReport { get; init; }

    public required ExecutionTargetFeatureReport FeatureReport { get; init; }

    public required ExecutionTargetCompatibilityReport CompatibilityReport { get; init; }

    public required TargetRuntimeContract RuntimeContract { get; init; }

    public required int HostAbiVersion { get; init; }

    public required TargetBackendRenderInputs BackendInputs { get; init; }

}

file static class TargetRenderContractImmutability
{
    public static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }

    public static IReadOnlyDictionary<string, string> FreezeDictionary(
        IReadOnlyDictionary<string, string>? values)
    {
        return new ReadOnlyDictionary<string, string>(
            values is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(values, StringComparer.Ordinal));
    }
}
