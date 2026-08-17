using Musoq.Schema;
using Musoq.Schema.Diagnostics;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Git;

internal sealed class GitDataSourceApiRecorder
{
    public List<SourceRuntimeSettingRequirement> RuntimeSettingRequirements { get; } = [];

    public List<OptimizationDiagnostic> DescribeSourceDiagnostics { get; } = [];

    public List<SourceContractDiagnostic> DescribeSourceContractDiagnostics { get; } = [];

    public List<OptimizationDiagnostic> PlanDiagnostics { get; } = [];

    public List<SourceContractDiagnostic> PlanContractDiagnostics { get; } = [];

    public Func<SourcePlanRequest, SourcePlanResult>? PlanResultFactory { get; set; }

    public List<string> SchemaRequests { get; } = [];

    public List<GitRawConstructorCall> RawConstructorCalls { get; } = [];

    public List<GitGetTableCall> GetTableCalls { get; } = [];

    public List<GitRuntimeSettingsCall> RuntimeSettingsCalls { get; } = [];

    public List<GitDescribeSourceCall> DescribeSourceCalls { get; } = [];

    public List<GitPlanCall> PlanCalls { get; } = [];

    public List<GitRowSourceCall> RowSourceCalls { get; } = [];
}

internal sealed record GitSourceMetadataSnapshot(
    string QueryId,
    IReadOnlyList<ISchemaColumn> AllColumns,
    IReadOnlyDictionary<string, string> SourceRuntimeSettings,
    Type LoggerType,
    bool CancellationCanBeCanceled)
{
    public static GitSourceMetadataSnapshot From(SourceMetadataContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new GitSourceMetadataSnapshot(
            context.QueryId,
            context.AllColumns.ToArray(),
            new Dictionary<string, string>(context.SourceRuntimeSettings, StringComparer.Ordinal),
            context.Logger.GetType(),
            context.EndWorkToken.CanBeCanceled);
    }
}

internal sealed record GitSourceExecutionSnapshot(
    string QueryId,
    SourceExecutionPlan Plan,
    IReadOnlyList<ISchemaColumn> AllColumns,
    IReadOnlyDictionary<string, string> SourceRuntimeSettings,
    Type LoggerType,
    bool CancellationCanBeCanceled,
    SourceDiagnostics Diagnostics)
{
    public static GitSourceExecutionSnapshot From(SourceExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new GitSourceExecutionSnapshot(
            context.QueryId,
            context.Plan,
            context.AllColumns.ToArray(),
            new Dictionary<string, string>(context.SourceRuntimeSettings, StringComparer.Ordinal),
            context.Logger.GetType(),
            context.EndWorkToken.CanBeCanceled,
            context.Diagnostics);
    }
}

internal sealed record GitRawConstructorCall(
    string? MethodName,
    GitSourceMetadataSnapshot Metadata);

internal sealed record GitGetTableCall(
    string Name,
    GitSourceMetadataSnapshot Metadata,
    IReadOnlyList<object?> Parameters,
    IReadOnlyList<ISchemaColumn> Columns);

internal sealed record GitRuntimeSettingsCall(
    string Name,
    SourceIdentity Identity,
    GitSourceMetadataSnapshot Metadata,
    IReadOnlyList<object?> Parameters);

internal sealed record GitDescribeSourceCall(
    string Name,
    SourceIdentity Identity,
    GitSourceMetadataSnapshot Metadata,
    IReadOnlyList<object?> Parameters,
    IReadOnlyList<ISchemaColumn> Columns,
    Type? RowType);

internal sealed record GitPlanCall(
    string Name,
    SourcePlanRequest Request,
    IReadOnlyList<object?> Parameters);

internal sealed record GitRowSourceCall(
    string Name,
    GitSourceExecutionSnapshot Execution,
    IReadOnlyList<object?> Parameters,
    Type RequestedRowType);
