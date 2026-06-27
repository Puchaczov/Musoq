using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Musoq.Schema.Diagnostics;

namespace Musoq.Schema.Optimization;

public class SourceExecutionContext(
    string queryId,
    SourceExecutionPlan plan,
    CancellationToken endWorkToken,
    IReadOnlyCollection<ISchemaColumn> originallyInferredColumns,
    IReadOnlyDictionary<string, string> sourceRuntimeSettings,
    ILogger logger,
    DataSourceEventHandler? dataSourceProgressCallback = null,
    SourceDiagnostics? sourceDiagnostics = null)
    : SourceMetadataContext(queryId, endWorkToken, originallyInferredColumns, sourceRuntimeSettings, logger)
{
    public SourceExecutionPlan Plan { get; } = plan ?? SourceExecutionPlan.Empty(SourceIdentity.Empty);

    public SourceDiagnostics Diagnostics { get; } = sourceDiagnostics ?? SourceDiagnostics.None;

    public void ReportDataSourceBegin(string dataSourceName)
    {
        dataSourceProgressCallback?.Invoke(this,
            new DataSourceEventArgs(QueryId, dataSourceName, DataSourcePhase.Begin));
    }

    public void ReportDataSourceRowsKnown(string dataSourceName, long totalRows)
    {
        dataSourceProgressCallback?.Invoke(this,
            new DataSourceEventArgs(QueryId, dataSourceName, DataSourcePhase.RowsKnown, totalRows));
    }

    public void ReportDataSourceRowsRead(string dataSourceName, long rowsProcessed, long? totalRows = null)
    {
        dataSourceProgressCallback?.Invoke(this,
            new DataSourceEventArgs(QueryId, dataSourceName, DataSourcePhase.RowsRead, totalRows, rowsProcessed));
    }

    public void ReportDataSourceEnd(string dataSourceName, long? totalRowsProcessed = null)
    {
        dataSourceProgressCallback?.Invoke(this,
            new DataSourceEventArgs(QueryId, dataSourceName, DataSourcePhase.End, totalRowsProcessed,
                totalRowsProcessed));
    }
}
