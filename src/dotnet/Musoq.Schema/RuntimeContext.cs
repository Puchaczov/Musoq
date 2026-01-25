using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Schema;

public class RuntimeContext(
    string queryId,
    CancellationToken endWorkToken,
    IReadOnlyCollection<ISchemaColumn> originallyInferredColumns,
    IReadOnlyDictionary<string, string> environmentVariables,
    (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> Columns, WhereNode WhereNode, bool
        HasExternallyProvidedTypes) queryInformation,
    ILogger logger,
    DataSourceEventHandler dataSourceProgressCallback = null)
{
    public string QueryId { get; } = queryId;

    public CancellationToken EndWorkToken { get; } = endWorkToken;

    public IReadOnlyCollection<ISchemaColumn> AllColumns { get; } = originallyInferredColumns;


    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; } = environmentVariables;


    public (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> Columns, WhereNode WhereNode, bool
        HasExternallyProvidedTypes) QueryInformation { get; } = queryInformation;

    public ILogger Logger { get; } = logger;

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
