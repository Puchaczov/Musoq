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
            return new ExecutionRowPresence(rowPresence.Alias, rowPresence.IsPresent, directSource);

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
            if (shape is SourceEntityShape scalar &&
                scalar.Fields.Any(static field => field.AccessStrategy is DirectScalarValueAccess))
            {
                source = new ExecutionLiteral(true, typeof(bool));
                return true;
            }
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

            var ordinal = IsScalarPresenceContext(context.Type) ? tableRow.Fields.FirstOrDefault(field =>
                string.Equals(field.Name, $"{alias}.Ordinal", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(field.QualifiedName, $"{alias}.Ordinal", StringComparison.OrdinalIgnoreCase)) : null;
            if (ordinal != null)
            {
                if (Nullable.GetUnderlyingType(ordinal.Type.ResolveClrType()) == null)
                {
                    source = new ExecutionLiteral(true, typeof(bool));
                    return true;
                }
                source = new ExecutionFieldRead(tableRow.Alias, ordinal.Name, ordinal.Type, ordinal.AccessStrategy);
                return true;
            }

            var presenceType = context.Type;
            if (presenceType.ResolveClrType().IsValueType)
                presenceType = ExecutionClrBindingFactory.FromClr(typeof(object));
            source = new ExecutionFieldRead(tableRow.Alias, context.Name, presenceType, context.AccessStrategy);
            return true;
        }

        source = new ExecutionLiteral(null, typeof(object));
        return false;
    }

    private static bool IsScalarPresenceContext(ExecutionTypeRef type)
    {
        var scalar = type.ResolveClrType();
        scalar = Nullable.GetUnderlyingType(scalar) ?? scalar;
        return scalar.IsPrimitive || scalar == typeof(string) || scalar == typeof(decimal) ||
               scalar == typeof(DateTime) || scalar == typeof(Guid);
    }
}
