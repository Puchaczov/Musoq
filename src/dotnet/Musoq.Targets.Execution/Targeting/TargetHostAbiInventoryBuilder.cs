using System;
using System.Collections.Generic;
using System.Linq;
namespace Musoq.Targets.Execution;

internal static class TargetHostAbiInventoryBuilder
{
    public static TargetHostAbiInventory Build(TargetRuntimeContract runtimeContract)
    {
        ArgumentNullException.ThrowIfNull(runtimeContract);

        var imports = new List<TargetHostAbiImport>();
        AddSourceAccess(runtimeContract, imports);
        AddPluginInvocations(runtimeContract, imports);
        AddRowShapes(runtimeContract, imports);
        AddNullBehavior(runtimeContract, imports);
        AddCancellation(runtimeContract, imports);
        AddDiagnostics(runtimeContract, imports);
        AddProfiling(runtimeContract, imports);
        return new TargetHostAbiInventory(imports);
    }

    private static void AddSourceAccess(
        TargetRuntimeContract runtimeContract,
        ICollection<TargetHostAbiImport> imports)
    {
        foreach (var source in runtimeContract.SourceAccess)
        {
            imports.Add(new TargetHostAbiImport(
                TargetHostAbiImportKind.SourceAccess,
                $"{source.Kind}:{source.SourceContextId}:{source.SchemaName}.{source.MethodName}",
                "source-access-v1",
                1,
                new TargetSourceAccessAbiDetails(
                    source.Kind,
                    source.SourceContextId,
                    source.SchemaName,
                    source.MethodName,
                    source.RowsType.StableName,
                    source.RowsType.Portability,
                    source.SourceType?.StableName ?? string.Empty,
                    source.SourceType?.Portability,
                    source.ArgumentTypes.Select((type, index) => new TargetSourceArgumentAbiContract(index, type)),
                    source.Fields.Select(static field => new TargetSourceFieldAbiContract(
                        field.Index,
                        field.Name,
                        field.Type,
                        field.PublicType,
                        field.Nullability,
                        field.ReadModifiers)),
                    source.AcceptedOperations,
                    source.RuntimeSettings)));
        }
    }

    private static void AddPluginInvocations(
        TargetRuntimeContract runtimeContract,
        ICollection<TargetHostAbiImport> imports)
    {
        foreach (var invocation in runtimeContract.PluginInvocations)
        {
            imports.Add(new TargetHostAbiImport(
                TargetHostAbiImportKind.PluginInvocation,
                $"{invocation.Detail} [{invocation.Callable.StableName}]",
                "plugin-invocation-v2",
                2,
                new TargetPluginInvocationAbiDetails(
                    invocation.Detail,
                    invocation.Callable.StableName,
                    invocation.Callable.Portability,
                    invocation.Callable.MethodName,
                    invocation.Callable.DeclaringType?.StableName ?? string.Empty,
                    invocation.Callable.ParameterTypes.Count)));
        }
    }

    private static void AddRowShapes(
        TargetRuntimeContract runtimeContract,
        ICollection<TargetHostAbiImport> imports)
    {
        foreach (var shape in runtimeContract.RowShapes)
        {
            imports.Add(new TargetHostAbiImport(
                TargetHostAbiImportKind.RowShapeTransfer,
                $"{shape.Kind}:{shape.Name}",
                "row-shape-transfer-v1",
                1,
                new TargetRowShapeTransferAbiDetails(
                    shape.Kind,
                    shape.Name,
                    shape.TypeSymbol?.StableName ?? string.Empty,
                    shape.TypeSymbol?.Portability,
                    shape.Fields.Count)));
        }
    }

    private static void AddNullBehavior(
        TargetRuntimeContract runtimeContract,
        ICollection<TargetHostAbiImport> imports)
    {
        var nullBehavior = runtimeContract.NullBehavior;
        if (!nullBehavior.UsesNullableValueTypes &&
            !nullBehavior.UsesObjectNulls &&
            !nullBehavior.UsesFieldNullabilityMetadata)
        {
            return;
        }

        imports.Add(new TargetHostAbiImport(
            TargetHostAbiImportKind.NullTypeCoercion,
            nullBehavior.Semantics,
            "null-type-coercion-v1",
            1,
            new TargetNullTypeCoercionAbiDetails(
                nullBehavior.Semantics,
                nullBehavior.UsesNullableValueTypes,
                nullBehavior.UsesObjectNulls,
                nullBehavior.UsesFieldNullabilityMetadata)));
    }

    private static void AddCancellation(
        TargetRuntimeContract runtimeContract,
        ICollection<TargetHostAbiImport> imports)
    {
        var cancellation = runtimeContract.Cancellation;
        if (!cancellation.RequiresCancellationToken &&
            !cancellation.RequiresParallelCancellation)
        {
            return;
        }

        imports.Add(new TargetHostAbiImport(
            TargetHostAbiImportKind.Cancellation,
            cancellation.RequiresParallelCancellation
                ? "cancellation-token+parallel"
                : "cancellation-token",
            "cancellation-v1",
            1,
            new TargetCancellationAbiDetails(
                cancellation.RequiresCancellationToken,
                cancellation.RequiresParallelCancellation)));
    }

    private static void AddDiagnostics(
        TargetRuntimeContract runtimeContract,
        ICollection<TargetHostAbiImport> imports)
    {
        var diagnostics = runtimeContract.Diagnostics;
        if (!diagnostics.RequiresBuildDiagnostics &&
            !diagnostics.RequiresSourceDiagnostics &&
            !diagnostics.RequiresRuntimeExceptionDiagnostics)
        {
            return;
        }

        imports.Add(new TargetHostAbiImport(
            TargetHostAbiImportKind.Diagnostics,
            $"{Flag("build", diagnostics.RequiresBuildDiagnostics)}{Flag("source", diagnostics.RequiresSourceDiagnostics)}{Flag("runtime", diagnostics.RequiresRuntimeExceptionDiagnostics)}".Trim('+'),
            "diagnostics-v1",
            1,
            new TargetDiagnosticsAbiDetails(
                diagnostics.RequiresBuildDiagnostics,
                diagnostics.RequiresSourceDiagnostics,
                diagnostics.RequiresRuntimeExceptionDiagnostics)));
    }

    private static void AddProfiling(
        TargetRuntimeContract runtimeContract,
        ICollection<TargetHostAbiImport> imports)
    {
        var profiling = runtimeContract.Profiling;
        if (!profiling.SupportsSourceBoundaryProfiling &&
            !profiling.SupportsOperatorProfiling)
        {
            return;
        }

        imports.Add(new TargetHostAbiImport(
            TargetHostAbiImportKind.Profiling,
            $"source-boundaries:{profiling.SourceBoundaryCount};operators:{profiling.OperatorCount}",
            "profiling-v1",
            1,
            new TargetProfilingAbiDetails(
                profiling.SupportsSourceBoundaryProfiling,
                profiling.SupportsOperatorProfiling,
                profiling.SourceBoundaryCount,
                profiling.OperatorCount)));
    }

    private static string Flag(string name, bool enabled)
    {
        return enabled ? $"{name}+" : string.Empty;
    }
}
