using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Execution;

internal sealed record TargetRuntimeContract
{
    public TargetRuntimeContract(
        string planIdentifier,
        IReadOnlyList<TargetSourceAccessContract>? sourceAccess,
        IReadOnlyList<TargetPluginInvocationContract>? pluginInvocations,
        IReadOnlyList<TargetRowShapeContract>? rowShapes,
        TargetNullBehaviorContract nullBehavior,
        TargetCancellationContract cancellation,
        TargetDiagnosticsContract diagnostics,
        TargetProfilingContract profiling)
    {
        PlanIdentifier = planIdentifier;
        SourceAccess = Freeze(sourceAccess);
        PluginInvocations = Freeze(pluginInvocations);
        RowShapes = Freeze(rowShapes);
        NullBehavior = nullBehavior;
        Cancellation = cancellation;
        Diagnostics = diagnostics;
        Profiling = profiling;
    }

    public string PlanIdentifier { get; }

    public IReadOnlyList<TargetSourceAccessContract> SourceAccess { get; }

    public IReadOnlyList<TargetPluginInvocationContract> PluginInvocations { get; }

    public IReadOnlyList<TargetRowShapeContract> RowShapes { get; }

    public TargetNullBehaviorContract NullBehavior { get; }

    public TargetCancellationContract Cancellation { get; }

    public TargetDiagnosticsContract Diagnostics { get; }

    public TargetProfilingContract Profiling { get; }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }
}

internal sealed record TargetSourceAccessContract
{
    public TargetSourceAccessContract(
        string kind,
        string sourceContextId,
        string schemaName,
        string methodName,
        ExecutionPortableTypeDescriptor rowsType,
        ExecutionPortableTypeDescriptor? sourceType,
        IReadOnlyList<TargetFieldContract>? fields)
        : this(kind, sourceContextId, schemaName, methodName, rowsType, sourceType, [], fields, [], [])
    {
    }

    public TargetSourceAccessContract(
        string kind,
        string sourceContextId,
        string schemaName,
        string methodName,
        ExecutionPortableTypeDescriptor rowsType,
        ExecutionPortableTypeDescriptor? sourceType,
        IReadOnlyList<ExecutionPortableTypeDescriptor>? argumentTypes,
        IReadOnlyList<TargetFieldContract>? fields,
        IReadOnlyList<TargetSourcePlanOperation>? acceptedOperations,
        IReadOnlyList<TargetRuntimeSettingAbiContract>? runtimeSettings)
    {
        Kind = kind;
        SourceContextId = sourceContextId;
        SchemaName = schemaName;
        MethodName = methodName;
        RowsType = rowsType;
        SourceType = sourceType;
        ArgumentTypes = Freeze(argumentTypes);
        Fields = Freeze(fields);
        AcceptedOperations = Freeze(acceptedOperations);
        RuntimeSettings = Freeze(runtimeSettings);
    }

    public string Kind { get; }

    public string SourceContextId { get; }

    public string SchemaName { get; }

    public string MethodName { get; }

    public ExecutionPortableTypeDescriptor RowsType { get; }

    public ExecutionPortableTypeDescriptor? SourceType { get; }

    public IReadOnlyList<ExecutionPortableTypeDescriptor> ArgumentTypes { get; }

    public IReadOnlyList<TargetFieldContract> Fields { get; }

    public IReadOnlyList<TargetSourcePlanOperation> AcceptedOperations { get; }

    public IReadOnlyList<TargetRuntimeSettingAbiContract> RuntimeSettings { get; }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }
}

internal sealed record TargetPluginInvocationContract(
    string Detail,
    ExecutionPortableCallableDescriptor Callable);

internal sealed record TargetRowShapeContract
{
    public TargetRowShapeContract(
        string kind,
        string name,
        ExecutionPortableTypeDescriptor? typeSymbol,
        IReadOnlyList<TargetFieldContract>? fields)
    {
        Kind = kind;
        Name = name;
        TypeSymbol = typeSymbol;
        Fields = Freeze(fields);
    }

    public string Kind { get; init; }

    public string Name { get; init; }

    public ExecutionPortableTypeDescriptor? TypeSymbol { get; }

    public IReadOnlyList<TargetFieldContract> Fields { get; }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }
}

internal sealed record TargetFieldContract
{
    public TargetFieldContract(
        string name,
        string qualifiedName,
        ExecutionPortableTypeDescriptor type,
        ExecutionPortableTypeDescriptor publicType,
        string nullability)
        : this(0, name, qualifiedName, type, publicType, nullability, null)
    {
    }

    public TargetFieldContract(
        int index,
        string name,
        string qualifiedName,
        ExecutionPortableTypeDescriptor type,
        ExecutionPortableTypeDescriptor publicType,
        string nullability,
        IReadOnlyDictionary<string, string>? readModifiers)
    {
        Index = index;
        Name = name;
        QualifiedName = qualifiedName;
        Type = type;
        PublicType = publicType;
        Nullability = nullability;
        ReadModifiers = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(
            readModifiers is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(readModifiers, StringComparer.Ordinal));
    }

    public int Index { get; }

    public string Name { get; }

    public string QualifiedName { get; }

    public ExecutionPortableTypeDescriptor Type { get; }

    public ExecutionPortableTypeDescriptor PublicType { get; }

    public string Nullability { get; }

    public IReadOnlyDictionary<string, string> ReadModifiers { get; }
}

internal sealed record TargetSourceRuntimeMetadata
{
    public TargetSourceRuntimeMetadata(
        string sourceContextId,
        IEnumerable<TargetSourcePlanOperation>? acceptedOperations,
        IEnumerable<TargetRuntimeSettingAbiContract>? runtimeSettings)
    {
        SourceContextId = sourceContextId;
        AcceptedOperations = Array.AsReadOnly(
            (acceptedOperations ?? []).Distinct().OrderBy(static operation => operation).ToArray());
        RuntimeSettings = Array.AsReadOnly(
            (runtimeSettings ?? []).OrderBy(static setting => setting.Key, StringComparer.Ordinal).ToArray());
    }

    public string SourceContextId { get; }

    public IReadOnlyList<TargetSourcePlanOperation> AcceptedOperations { get; }

    public IReadOnlyList<TargetRuntimeSettingAbiContract> RuntimeSettings { get; }
}

internal sealed record TargetNullBehaviorContract(
    bool UsesNullableValueTypes,
    bool UsesObjectNulls,
    bool UsesFieldNullabilityMetadata,
    string Semantics);

internal sealed record TargetCancellationContract(
    bool RequiresCancellationToken,
    bool RequiresParallelCancellation);

internal sealed record TargetDiagnosticsContract(
    bool RequiresBuildDiagnostics,
    bool RequiresSourceDiagnostics,
    bool RequiresRuntimeExceptionDiagnostics);

internal sealed record TargetProfilingContract(
    bool SupportsSourceBoundaryProfiling,
    bool SupportsOperatorProfiling,
    int SourceBoundaryCount,
    int OperatorCount);
