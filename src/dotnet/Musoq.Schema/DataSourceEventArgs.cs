using System;

namespace Musoq.Schema;

public class DataSourceEventArgs : EventArgs
{
    public DataSourceEventArgs(string queryId, string dataSourceName, DataSourcePhase phase, long? totalRows = null,
        long? rowsProcessed = null)
    {
        QueryId = queryId;
        DataSourceName = dataSourceName;
        Phase = phase;
        TotalRows = totalRows;
        RowsProcessed = rowsProcessed;
    }

    public string QueryId { get; }

    public string DataSourceName { get; }

    public DataSourcePhase Phase { get; }

    public long? TotalRows { get; }

    public long? RowsProcessed { get; }
}
