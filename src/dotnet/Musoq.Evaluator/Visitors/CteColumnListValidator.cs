using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class CteColumnListValidator
{
    public static bool TryFindDuplicate(
        CteInnerExpressionNode definition,
        out DuplicateCteColumnFailure failure)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in definition.Columns)
        {
            if (names.Add(column.Name))
                continue;

            failure = new DuplicateCteColumnFailure(
                ErrorCatalog.GetMessage(
                    DiagnosticCode.MQ3078_DuplicateCteColumnName,
                    definition.Name,
                    column.Name),
                column.Span);
            return true;
        }

        failure = default;
        return false;
    }

    internal readonly record struct DuplicateCteColumnFailure(string Message, TextSpan Span);
}
