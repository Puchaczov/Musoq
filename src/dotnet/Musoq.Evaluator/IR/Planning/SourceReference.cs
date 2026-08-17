using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourceReference(
    string? SourceContextId,
    string Alias,
    IReadOnlySet<string> OutputColumns)
{
    public bool ContainsOutputColumn(string columnName)
    {
        return OutputColumns.Count == 0 || OutputColumns.Contains(columnName);
    }
}
