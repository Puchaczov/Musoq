using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static ExecutionAppendRow CreateAppendRow(
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        ProjectedField[] fields,
        RowShape sourceShape)
    {
        var sourceLookup = new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase)
        {
            [RowShapeLookup.ResolveSourceAlias(sourceShape)] = sourceShape
        };
        var values = fields
            .Select(field => new ExecutionRowValue(field.OutputName, ConvertProjectedExpression(field, sourceLookup)))
            .ToArray();

        return new ExecutionAppendRow(
            resultTable,
            resultShape,
            values,
            CreateContextValues(sourceLookup),
            SerialAppendMode,
            CreateContextLayout(sourceLookup));
    }

    private static ExecutionAppendRow CreateAppendRow(
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        ProjectedField[] fields,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var values = fields
            .Select(field => new ExecutionRowValue(field.OutputName, ConvertProjectedExpression(field, sourceLookup)))
            .ToArray();

        return new ExecutionAppendRow(
            resultTable,
            resultShape,
            values,
            CreateContextValues(sourceLookup),
            SerialAppendMode,
            CreateContextLayout(sourceLookup));
    }

    private static List<ExecutionExpression> CreateContextValues(
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        string? nullAlias = null)
    {
        var contexts = new List<ExecutionExpression>();

        foreach (var sourceShape in sourceLookup.Values)
            contexts.AddRange(CreateContextValues(sourceShape, nullAlias));

        return contexts;
    }

    private static ExecutionContextLayout? CreateContextLayout(
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        string? nullAlias = null)
    {
        var segments = new List<ExecutionContextSegment>(sourceLookup.Count);

        foreach (var sourceShape in sourceLookup.Values)
        {
            if (!TryCreateContextSegment(sourceShape, nullAlias, out var segment))
                return null;

            if (segment != null)
                segments.Add(segment);
        }

        return segments.Count is 0 or > 2
            ? null
            : new ExecutionContextLayout(segments);
    }

    private static bool TryCreateContextSegment(
        RowShape sourceShape,
        string? nullAlias,
        out ExecutionContextSegment? segment)
    {
        if (sourceShape is TableRowShape tableRow)
        {
            segment = CreateTableRowContextSegment(tableRow, nullAlias);
            return segment != null || tableRow.Contexts.Count == 0;
        }

        ExecutionExpression expression = IsNullExtendedSource(sourceShape, nullAlias)
            ? new ExecutionLiteral(null, typeof(object))
            : new ExecutionVariableRead(new ExecutionVariable(RowShapeLookup.ResolveSourceAlias(sourceShape), RowShapeLookup.ResolveSourceRuntimeType(sourceShape)));

        segment = new ExecutionContextSegment(ExecutionContextSegmentKind.Single, expression, 1);
        return true;
    }

    private static ExecutionContextSegment? CreateTableRowContextSegment(
        TableRowShape tableRow,
        string? nullAlias)
    {
        if (tableRow.Contexts.Count == 0)
        {
            ExecutionExpression expression = IsNullExtendedSource(tableRow, nullAlias)
                ? new ExecutionLiteral(null, typeof(object))
                : new ExecutionVariableRead(new ExecutionVariable(tableRow.Alias, typeof(object)));

            return new ExecutionContextSegment(ExecutionContextSegmentKind.Single, expression, 1);
        }

        if (IsNullExtendedSource(tableRow, nullAlias))
        {
            return new ExecutionContextSegment(
                ExecutionContextSegmentKind.Array,
                new ExecutionNullContextArray(tableRow.Contexts.Count),
                tableRow.Contexts.Count);
        }

        if (CanUseDirectTableRowContextSegment(tableRow))
        {
            return tableRow.Contexts.Count == 1
                ? new ExecutionContextSegment(
                    ExecutionContextSegmentKind.Single,
                    CreateTableRowContextFieldRead(tableRow, tableRow.Contexts[0]),
                    1)
                : new ExecutionContextSegment(
                ExecutionContextSegmentKind.Array,
                new ExecutionContextArray(tableRow.Contexts
                    .Select(context => new ExecutionContextSegment(
                        ExecutionContextSegmentKind.Single,
                        CreateTableRowContextFieldRead(tableRow, context),
                        1))
                    .ToArray()),
                tableRow.Contexts.Count);
        }

        if (tableRow.Contexts.Any(context => IsNullExtendedContext(context, nullAlias)))
            return null;

        return new ExecutionContextSegment(
            ExecutionContextSegmentKind.Row,
            new ExecutionVariableRead(new ExecutionVariable(tableRow.Alias, typeof(Row))),
            tableRow.Contexts.Count);
    }

    private static bool CanUseDirectTableRowContextSegment(TableRowShape tableRow) =>
        tableRow.Contexts.All(static context => context.AccessStrategy is GeneratedFieldAccess or GeneratedRowContextAccess);

    private static ExecutionFieldRead CreateTableRowContextFieldRead(TableRowShape tableRow, FieldBinding context) =>
        new(tableRow.Alias, context.Name, context.Type, context.AccessStrategy);

    private static IEnumerable<ExecutionExpression> CreateContextValues(
        RowShape sourceShape,
        string? nullAlias)
    {
        if (IsNullExtendedSource(sourceShape, nullAlias))
        {
            foreach (var context in CreateNullContextValues(sourceShape))
                yield return context;

            yield break;
        }

        if (sourceShape is TableRowShape tableRow)
        {
            if (tableRow.Contexts.Count == 0)
            {
                yield return new ExecutionVariableRead(new ExecutionVariable(tableRow.Alias, typeof(object)));
                yield break;
            }

            foreach (var context in tableRow.Contexts)
            {
                if (IsNullExtendedContext(context, nullAlias))
                {
                    yield return new ExecutionLiteral((object?)null, context.Type);
                    continue;
                }

                yield return new ExecutionFieldRead(tableRow.Alias, context.Name, context.Type, context.AccessStrategy);
            }

            yield break;
        }

        yield return new ExecutionVariableRead(new ExecutionVariable(RowShapeLookup.ResolveSourceAlias(sourceShape), RowShapeLookup.ResolveSourceRuntimeType(sourceShape)));
    }

    private static IEnumerable<ExecutionExpression> CreateNullContextValues(RowShape sourceShape)
    {
        if (sourceShape is TableRowShape tableRow)
        {
            if (tableRow.Contexts.Count == 0)
            {
                yield return new ExecutionLiteral(null, typeof(object));
                yield break;
            }

            foreach (var context in tableRow.Contexts)
                yield return new ExecutionLiteral((object?)null, context.Type);

            yield break;
        }

        yield return new ExecutionLiteral(null, RowShapeLookup.ResolveSourceRuntimeType(sourceShape));
    }

    private static bool IsNullExtendedSource(RowShape sourceShape, string? nullAlias)
    {
        return !string.IsNullOrWhiteSpace(nullAlias) &&
               string.Equals(RowShapeLookup.ResolveSourceAlias(sourceShape), nullAlias, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNullExtendedContext(FieldBinding context, string? nullAlias)
    {
        return !string.IsNullOrWhiteSpace(nullAlias) &&
               (string.Equals(context.Name, nullAlias, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(context.QualifiedName, nullAlias, StringComparison.OrdinalIgnoreCase));
    }
}
