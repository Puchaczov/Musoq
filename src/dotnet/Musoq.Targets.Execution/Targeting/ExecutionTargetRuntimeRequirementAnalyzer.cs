using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Execution;

internal static class ExecutionTargetRuntimeRequirementAnalyzer
{
    public static IReadOnlyList<ExecutionTargetRequirement> Analyze(TargetRuntimeContract runtimeContract)
    {
        ArgumentNullException.ThrowIfNull(runtimeContract);

        return CreateRuntimeContractRequirements(runtimeContract)
            .OrderBy(static requirement => requirement.Kind)
            .ThenBy(static requirement => requirement.Detail, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<ExecutionTargetRequirement> CreateRuntimeContractRequirements(
        TargetRuntimeContract runtimeContract)
    {
        foreach (var source in runtimeContract.SourceAccess)
        {
            yield return new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.HostSourceAccess,
                $"{source.Kind}:{source.SourceContextId}:{source.SchemaName}.{source.MethodName}",
                source.SourceType ?? source.RowsType);
        }

        foreach (var source in runtimeContract.QueryRowSourceAccess)
        {
            yield return new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.QueryRowSourceAccess,
                $"{source.SourceContextId}:{source.SchemaName}.{source.MethodName}:{source.ShapeFingerprint}");
        }

        foreach (var rowShape in runtimeContract.RowShapes)
        {
            if (rowShape.TypeSymbol?.Kind != ExecutionPortableTypeKind.GeneratedRow)
                continue;

            yield return new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.GeneratedClrRow,
                rowShape.Name,
                rowShape.TypeSymbol);
        }

        foreach (var pluginInvocation in runtimeContract.PluginInvocations)
        {
            yield return new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.PluginInvocation,
                pluginInvocation.Detail,
                CallableSymbol: pluginInvocation.Callable);
        }

        if (runtimeContract.NullBehavior.UsesNullableValueTypes ||
            runtimeContract.NullBehavior.UsesObjectNulls ||
            runtimeContract.NullBehavior.UsesFieldNullabilityMetadata)
        {
            yield return new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.NullTypeCoercion,
                CreateNullBehaviorDetail(runtimeContract.NullBehavior));
        }

        if (runtimeContract.Cancellation.RequiresCancellationToken)
        {
            yield return new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.Cancellation,
                runtimeContract.Cancellation.RequiresParallelCancellation
                    ? "cancellation-token+parallel-cancellation"
                    : "cancellation-token");
        }

        if (runtimeContract.Diagnostics.RequiresBuildDiagnostics ||
            runtimeContract.Diagnostics.RequiresSourceDiagnostics ||
            runtimeContract.Diagnostics.RequiresRuntimeExceptionDiagnostics ||
            runtimeContract.Profiling.SupportsSourceBoundaryProfiling ||
            runtimeContract.Profiling.SupportsOperatorProfiling)
        {
            yield return new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.ProfilingDiagnostics,
                CreateDiagnosticsProfilingDetail(runtimeContract));
        }
    }

    private static string CreateNullBehaviorDetail(TargetNullBehaviorContract nullBehavior)
    {
        var flags = new List<string>();

        if (nullBehavior.UsesNullableValueTypes)
            flags.Add("nullable-value-types");
        if (nullBehavior.UsesObjectNulls)
            flags.Add("object-nulls");
        if (nullBehavior.UsesFieldNullabilityMetadata)
            flags.Add("field-nullability-metadata");

        return $"{nullBehavior.Semantics}:{string.Join("+", flags)}";
    }

    private static string CreateDiagnosticsProfilingDetail(TargetRuntimeContract runtimeContract)
    {
        var diagnostics = new List<string>();

        if (runtimeContract.Diagnostics.RequiresBuildDiagnostics)
            diagnostics.Add("build-diagnostics");
        if (runtimeContract.Diagnostics.RequiresSourceDiagnostics)
            diagnostics.Add("source-diagnostics");
        if (runtimeContract.Diagnostics.RequiresRuntimeExceptionDiagnostics)
            diagnostics.Add("runtime-exception-diagnostics");
        if (runtimeContract.Profiling.SupportsSourceBoundaryProfiling)
            diagnostics.Add($"source-boundary-profiling:{runtimeContract.Profiling.SourceBoundaryCount}");
        if (runtimeContract.Profiling.SupportsOperatorProfiling)
            diagnostics.Add($"operator-profiling:{runtimeContract.Profiling.OperatorCount}");

        return string.Join("+", diagnostics);
    }
}
