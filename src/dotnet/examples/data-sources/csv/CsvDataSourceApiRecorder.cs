using Microsoft.Extensions.Logging;
using Musoq.Schema;
using Musoq.Schema.Diagnostics;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv;

internal sealed class CsvDataSourceApiRecorder
{
    public List<SourceRuntimeSettingRequirement> RuntimeSettingRequirements { get; } = [];

    public List<SourceContractDiagnostic> DescribeSourceContractDiagnostics { get; } = [];

    public List<SourceContractDiagnostic> PlanContractDiagnostics { get; } = [];

    public Func<SourcePlanRequest, SourcePlanResult>? PlanResultFactory { get; set; }

    public List<string> SchemaRequests { get; } = [];

    public List<CsvRawConstructorCall> RawConstructorCalls { get; } = [];

    public List<CsvGetTableCall> GetTableCalls { get; } = [];

    public List<CsvRuntimeSettingsCall> RuntimeSettingsCalls { get; } = [];

    public List<CsvDescribeSourceCall> DescribeSourceCalls { get; } = [];

    public List<CsvPlanCall> PlanCalls { get; } = [];

    public List<CsvRowSourceCall> RowSourceCalls { get; } = [];
}

internal sealed record CsvSourceMetadataSnapshot(
    string QueryId,
    IReadOnlyList<ISchemaColumn> AllColumns,
    IReadOnlyDictionary<string, string> SourceRuntimeSettings,
    Type LoggerType,
    bool CancellationCanBeCanceled)
{
    public static CsvSourceMetadataSnapshot From(SourceMetadataContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CsvSourceMetadataSnapshot(
            context.QueryId,
            context.AllColumns.ToArray(),
            new Dictionary<string, string>(context.SourceRuntimeSettings, StringComparer.Ordinal),
            context.Logger.GetType(),
            context.EndWorkToken.CanBeCanceled);
    }
}

internal sealed record CsvSourceExecutionSnapshot(
    string QueryId,
    SourceExecutionPlan Plan,
    IReadOnlyList<ISchemaColumn> AllColumns,
    IReadOnlyDictionary<string, string> SourceRuntimeSettings,
    Type LoggerType,
    bool CancellationCanBeCanceled,
    SourceDiagnostics Diagnostics)
{
    public static CsvSourceExecutionSnapshot From(SourceExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CsvSourceExecutionSnapshot(
            context.QueryId,
            context.Plan,
            context.AllColumns.ToArray(),
            new Dictionary<string, string>(context.SourceRuntimeSettings, StringComparer.Ordinal),
            context.Logger.GetType(),
            context.EndWorkToken.CanBeCanceled,
            context.Diagnostics);
    }
}

internal sealed record CsvRawConstructorCall(
    string? MethodName,
    CsvSourceMetadataSnapshot Metadata);

internal sealed record CsvGetTableCall(
    string Name,
    CsvSourceMetadataSnapshot Metadata,
    IReadOnlyList<object?> Parameters,
    IReadOnlyList<ISchemaColumn> Columns);

internal sealed record CsvRuntimeSettingsCall(
    string Name,
    SourceIdentity Identity,
    CsvSourceMetadataSnapshot Metadata,
    IReadOnlyList<object?> Parameters);

internal sealed record CsvDescribeSourceCall(
    string Name,
    SourceIdentity Identity,
    CsvSourceMetadataSnapshot Metadata,
    IReadOnlyList<object?> Parameters,
    IReadOnlyList<ISchemaColumn> Columns);

internal sealed record CsvPlanCall(
    string Name,
    SourcePlanRequest Request,
    IReadOnlyList<object?> Parameters);

internal sealed record CsvRowSourceCall(
    string Name,
    CsvSourceExecutionSnapshot Execution,
    IReadOnlyList<object?> Parameters,
    Type RequestedRowType);
