using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionExpressionConverter
{
    private static ExecutionExpression ConvertRowPresence(
        RowPresence rowPresence,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        if (TryCreateDirectPresenceSource(rowPresence.Alias, sourceShapes, out var directSource))
            return directSource.ReturnType.ResolveClrType().IsValueType &&
                   Nullable.GetUnderlyingType(directSource.ReturnType.ResolveClrType()) == null
                ? new ExecutionLiteral(rowPresence.IsPresent, typeof(bool))
                : new ExecutionRowPresence(rowPresence.Alias, rowPresence.IsPresent, directSource);

        if (TryCreateTableContextPresenceSource(rowPresence.Alias, sourceShapes, out var contextSource))
            return new ExecutionRowPresence(rowPresence.Alias, rowPresence.IsPresent, contextSource);

        return new ExecutionRowPresence(
            rowPresence.Alias,
            rowPresence.IsPresent,
            new ExecutionVariableRead(new ExecutionVariable(rowPresence.Alias, typeof(object))));
    }

    private static bool TryCreateDirectPresenceSource(
        string alias,
        IReadOnlyDictionary<string, RowShape> sourceShapes,
        out ExecutionExpression source)
    {
        if (sourceShapes.TryGetValue(alias, out var shape) &&
            RowShapeLookup.TryResolveSourceAlias(shape, out var sourceAlias) &&
            string.Equals(sourceAlias, alias, StringComparison.OrdinalIgnoreCase))
        {
            source = new ExecutionVariableRead(new ExecutionVariable(alias, RowShapeLookup.ResolveSourceRuntimeType(shape)));
            return true;
        }

        source = new ExecutionLiteral(null, typeof(object));
        return false;
    }

    private static bool TryCreateTableContextPresenceSource(
        string alias,
        IReadOnlyDictionary<string, RowShape> sourceShapes,
        out ExecutionExpression source)
    {
        foreach (var tableRow in sourceShapes.Values.OfType<TableRowShape>())
        {
            var context = tableRow.Contexts.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, alias, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.QualifiedName, alias, StringComparison.OrdinalIgnoreCase));
            if (context == null)
                continue;

            source = new ExecutionFieldRead(tableRow.Alias, context.Name, context.Type, context.AccessStrategy);
            return true;
        }

        source = new ExecutionLiteral(null, typeof(object));
        return false;
    }
}
