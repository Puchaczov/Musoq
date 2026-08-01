using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record CteReference(
    string CteName,
    string Alias,
    IReadOnlySet<string> OutputColumns)
{
    public bool ContainsOutputColumn(string columnName)
    {
        var relativeColumnName = columnName.StartsWith($"{Alias}.", StringComparison.OrdinalIgnoreCase)
            ? columnName[(Alias.Length + 1)..]
            : columnName;

        return OutputColumns.Count == 0 ||
               OutputColumns.Contains(columnName) ||
               OutputColumns.Contains(relativeColumnName) ||
               OutputColumns.Any(output => output.EndsWith($".{relativeColumnName}", StringComparison.OrdinalIgnoreCase));
    }
}
