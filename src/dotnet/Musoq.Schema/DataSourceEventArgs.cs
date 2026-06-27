namespace Musoq.Schema;

public class DataSourceEventArgs(
    string queryId,
    string dataSourceName,
    DataSourcePhase phase,
    long? totalRows = null,
    long? rowsProcessed = null)
    : EventArgs
{
    public string QueryId { get; } = queryId;

    public string DataSourceName { get; } = dataSourceName;

    public DataSourcePhase Phase { get; } = phase;

    public long? TotalRows { get; } = totalRows;

    public long? RowsProcessed { get; } = rowsProcessed;
}
