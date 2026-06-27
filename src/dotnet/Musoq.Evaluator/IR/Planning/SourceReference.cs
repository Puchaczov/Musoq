using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

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
