using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static AggregateGroupValueCaptureBuildResult CreateAggregateGroupValueCaptureNodes(
        IReadOnlyList<ProjectedField> fields,
        IrExpression? havingPredicate,
        IReadOnlyList<PostOperation> postOperations,
        AggregateFinalizationGroupKeys groupKeys,
        ExecutionVariable group,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        AggregateGroupLowering aggregateGroup)
    {
        var columns = CollectAggregateFinalSourceColumns(fields, havingPredicate, postOperations, groupKeys);
        var nodes = new List<ExecutionNode>(columns.Length);
        var capturedValues = new Dictionary<string, AggregateCapturedValue>(StringComparer.OrdinalIgnoreCase);
        var ambiguousKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var capturedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in columns)
        {
            var valueName = CreateAggregateColumnValueName(column);
            if (!capturedNames.Add(valueName))
                continue;

            var value = ExecutionExpressionConverter.Convert(column, sourceLookup);
            var capturedField = TryResolveAggregateCapturedField(aggregateGroup, valueName);
            if (capturedField == null)
            {
                return AggregateGroupValueCaptureBuildResult.Unsupported(
                    $"Execution IR aggregate lowering cannot resolve typed captured field for aggregate finalization value '{valueName}'.");
            }

            nodes.Add(new ExecutionAggregateCapturedValueSet(group, valueName, value, column.ReturnType, capturedField));
            AddAggregateCapturedValue(capturedValues, ambiguousKeys, valueName, valueName, column.ReturnType);
            AddAggregateCapturedValue(
                capturedValues,
                ambiguousKeys,
                AggregateRefRewriter.NormalizeIdentifier(valueName),
                valueName,
                column.ReturnType);
        }

        return AggregateGroupValueCaptureBuildResult.Success(nodes, capturedValues);
    }

    private static ColumnRef[] CollectAggregateFinalSourceColumns(
        IReadOnlyList<ProjectedField> fields,
        IrExpression? havingPredicate,
        IReadOnlyList<PostOperation> postOperations,
        AggregateFinalizationGroupKeys groupKeys)
    {
        var columns = new List<ColumnRef>();

        foreach (var field in fields)
            CollectAggregateFinalSourceColumns(field.Expression, columns, groupKeys);

        if (havingPredicate != null)
            CollectAggregateFinalSourceColumns(havingPredicate, columns, groupKeys);

        foreach (var postOperation in postOperations)
            CollectAggregateFinalSourceColumns(postOperation, columns, groupKeys);

        return columns
            .Where(column => TryGetGroupKeyExpressionIndex(column, groupKeys) == null)
            .ToArray();
    }

    private static void CollectAggregateFinalSourceColumns(
        PostOperation postOperation,
        ICollection<ColumnRef> columns,
        AggregateFinalizationGroupKeys groupKeys)
    {
        switch (postOperation)
        {
            case SortOperation sort:
                foreach (var key in sort.Keys)
                    CollectAggregateFinalSourceColumns(key.Expression, columns, groupKeys);

                foreach (var field in sort.ProjectedFields)
                    CollectAggregateFinalSourceColumns(field.Expression, columns, groupKeys);

                break;
            case TopNOperation topN:
                foreach (var key in topN.Keys)
                    CollectAggregateFinalSourceColumns(key.Expression, columns, groupKeys);

                foreach (var field in topN.ProjectedFields)
                    CollectAggregateFinalSourceColumns(field.Expression, columns, groupKeys);

                break;
            case TopOffsetOperation topOffset:
                foreach (var key in topOffset.Keys)
                    CollectAggregateFinalSourceColumns(key.Expression, columns, groupKeys);

                foreach (var field in topOffset.ProjectedFields)
                    CollectAggregateFinalSourceColumns(field.Expression, columns, groupKeys);

                break;
        }
    }

    private static void CollectAggregateFinalSourceColumns(
        IrExpression expression,
        ICollection<ColumnRef> columns,
        AggregateFinalizationGroupKeys groupKeys)
    {
        if (TryGetGroupKeyExpressionIndex(expression, groupKeys) != null)
            return;

        switch (expression)
        {
            case ColumnRef columnRef:
                columns.Add(columnRef);
                break;
            case BinaryOp binary:
                CollectAggregateFinalSourceColumns(binary.Left, columns, groupKeys);
                CollectAggregateFinalSourceColumns(binary.Right, columns, groupKeys);
                break;
            case UnaryOp unary:
                CollectAggregateFinalSourceColumns(unary.Operand, columns, groupKeys);
                break;
            case MethodCall methodCall:
                if (IsAggregateLikeMethodCall(methodCall))
                    break;

                foreach (var argument in methodCall.Arguments)
                    CollectAggregateFinalSourceColumns(argument, columns, groupKeys);

                break;
            case IsNullCheck isNull:
                CollectAggregateFinalSourceColumns(isNull.Expression, columns, groupKeys);
                break;
            case InCheck inCheck:
                CollectAggregateFinalSourceColumns(inCheck.Expression, columns, groupKeys);
                foreach (var value in inCheck.Values)
                    CollectAggregateFinalSourceColumns(value, columns, groupKeys);

                break;
            case PatternMatch patternMatch:
                CollectAggregateFinalSourceColumns(patternMatch.Expression, columns, groupKeys);
                CollectAggregateFinalSourceColumns(patternMatch.Pattern, columns, groupKeys);
                break;
            case Between between:
                CollectAggregateFinalSourceColumns(between.Expression, columns, groupKeys);
                CollectAggregateFinalSourceColumns(between.Low, columns, groupKeys);
                CollectAggregateFinalSourceColumns(between.High, columns, groupKeys);
                break;
            case CaseWhen caseWhen:
                foreach (var branch in caseWhen.Branches)
                {
                    CollectAggregateFinalSourceColumns(branch.Condition, columns, groupKeys);
                    CollectAggregateFinalSourceColumns(branch.Result, columns, groupKeys);
                }

                if (caseWhen.ElseExpression != null)
                    CollectAggregateFinalSourceColumns(caseWhen.ElseExpression, columns, groupKeys);

                break;
            case Coalesce coalesce:
                foreach (var coalesceExpression in coalesce.Expressions)
                    CollectAggregateFinalSourceColumns(coalesceExpression, columns, groupKeys);

                break;
            case ArrayAccess arrayAccess:
                CollectAggregateFinalSourceColumns(arrayAccess.Array, columns, groupKeys);
                CollectAggregateFinalSourceColumns(arrayAccess.Index, columns, groupKeys);
                break;
        }
    }

    private static string CreateAggregateColumnValueName(ColumnRef columnRef)
    {
        return string.IsNullOrWhiteSpace(columnRef.Alias)
            ? columnRef.ColumnName
            : $"{columnRef.Alias}.{columnRef.ColumnName}";
    }

    private static void AddAggregateCapturedValue(
        IDictionary<string, AggregateCapturedValue> capturedValues,
        HashSet<string> ambiguousKeys,
        string? lookupKey,
        string valueName,
        Type valueType)
    {
        if (string.IsNullOrWhiteSpace(lookupKey) || ambiguousKeys.Contains(lookupKey))
            return;

        var capturedValue = new AggregateCapturedValue(valueName, valueType);
        if (!capturedValues.TryGetValue(lookupKey, out var existing))
        {
            capturedValues[lookupKey] = capturedValue;
            return;
        }

        if (string.Equals(existing.ValueName, valueName, StringComparison.OrdinalIgnoreCase) &&
            existing.ValueType == valueType)
        {
            return;
        }

        capturedValues.Remove(lookupKey);
        ambiguousKeys.Add(lookupKey);
    }
}
