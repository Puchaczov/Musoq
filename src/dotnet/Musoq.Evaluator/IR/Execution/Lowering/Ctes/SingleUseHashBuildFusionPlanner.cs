using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record FusedHashPayloadPruningResult(
    FusedHashPayload Payload,
    IReadOnlyList<RowShape> Shapes);

internal sealed class SingleUseHashBuildFusionPlanner
{
    public static bool CanFuseProducerSource(PhysicalNode source)
    {
        return source is PhysicalSchemaScanNode or PhysicalValuesScanNode;
    }

    public static bool CanFuseProducerShape(RowShape? producerShape)
    {
        return producerShape != null && producerShape is not ExpandoAdapterShape;
    }

    public bool TryPruneFusedHashPayload(
        FusedHashPayload payload,
        IReadOnlyList<RowShape> shapes,
        ExecutionBlock matchedBody,
        string payloadAlias,
        out FusedHashPayloadPruningResult result)
    {
        result = null!;

        if (payload.Shape.Fields.Count == 0)
            return false;

        if (!TryCollectRequiredPayloadFieldNames(matchedBody, payloadAlias, out var requiredFields))
            return false;

        var prunedFields = payload.Shape.Fields
            .Where(field => field.AccessStrategy is GeneratedFieldAccess generated &&
                            requiredFields.Contains(generated.FieldName))
            .ToArray();
        if (prunedFields.Length == payload.Shape.Fields.Count)
            return false;

        var prunedShape = new HashPayloadShape(payload.Shape.TypeName, prunedFields, payload.Shape.Contexts);
        if (!TryCreatePrunedPayloadValues(payload, prunedShape, out var prunedValues))
            return false;

        result = new FusedHashPayloadPruningResult(
            new FusedHashPayload(prunedShape, prunedValues),
            ReplacePayloadShape(shapes, payload.Shape, prunedShape));
        return true;
    }

    public bool TryCollectRequiredPayloadFieldNames(
        ExecutionBlock block,
        string payloadAlias,
        out IReadOnlySet<string> requiredFields)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        if (!TryCollectRequiredPayloadFieldNames(block, payloadAlias, fields))
        {
            requiredFields = fields;
            return false;
        }

        requiredFields = fields;
        return true;
    }

    private static bool TryCreatePrunedPayloadValues(
        FusedHashPayload payload,
        HashPayloadShape prunedShape,
        out IReadOnlyList<ExecutionRowValue> prunedValues)
    {
        var values = new List<ExecutionRowValue>(prunedShape.Fields.Count + prunedShape.Contexts.Count);
        foreach (var field in prunedShape.Fields)
        {
            var index = FindHashPayloadFieldIndex(payload.Shape, field);
            if (index < 0)
            {
                prunedValues = [];
                return false;
            }

            values.Add(payload.Values[index]);
        }

        values.AddRange(payload.Values.Skip(payload.Shape.Fields.Count));
        prunedValues = values;
        return true;
    }

    private static int FindHashPayloadFieldIndex(
        HashPayloadShape payloadShape,
        FieldBinding field)
    {
        for (var index = 0; index < payloadShape.Fields.Count; index++)
        {
            if (FieldBindingsMatch(payloadShape.Fields[index], field))
                return index;
        }

        return -1;
    }

    private static RowShape[] ReplacePayloadShape(
        IReadOnlyList<RowShape> shapes,
        HashPayloadShape oldShape,
        HashPayloadShape newShape)
    {
        return shapes
            .Select(shape => shape is HashPayloadShape payloadShape &&
                             string.Equals(payloadShape.TypeName, oldShape.TypeName, StringComparison.Ordinal)
                ? newShape
                : shape)
            .ToArray();
    }

    private static bool FieldBindingsMatch(FieldBinding left, FieldBinding right)
    {
        if (string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(left.QualifiedName, right.QualifiedName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return left.AccessStrategy is GeneratedFieldAccess leftGenerated &&
               right.AccessStrategy is GeneratedFieldAccess rightGenerated &&
               string.Equals(leftGenerated.FieldName, rightGenerated.FieldName, StringComparison.Ordinal);
    }

    private static bool TryCollectRequiredPayloadFieldNames(
        ExecutionBlock block,
        string payloadAlias,
        ISet<string> requiredFields)
    {
        foreach (var node in block.Nodes)
        {
            if (!TryCollectRequiredPayloadFieldNames(node, payloadAlias, requiredFields))
                return false;
        }

        return true;
    }

    private static bool TryCollectRequiredPayloadFieldNames(
        ExecutionNode node,
        string payloadAlias,
        ISet<string> requiredFields)
    {
        switch (node)
        {
            case ExecutionLet let:
                return TryCollectRequiredPayloadFieldNames(let.Value, payloadAlias, requiredFields);
            case ExecutionAssign assign:
                return TryCollectRequiredPayloadFieldNames(assign.Value, payloadAlias, requiredFields);
            case ExecutionContinueIf continueIf:
                return TryCollectRequiredPayloadFieldNames(continueIf.Condition, payloadAlias, requiredFields);
            case ExecutionIf branch:
                return TryCollectRequiredPayloadFieldNames(branch.Condition, payloadAlias, requiredFields) &&
                       TryCollectRequiredPayloadFieldNames(branch.Body, payloadAlias, requiredFields);
            case ExecutionForEach forEach:
                return TryCollectRequiredPayloadFieldNames(forEach.Source, payloadAlias, requiredFields) &&
                       TryCollectRequiredPayloadFieldNames(forEach.Body, payloadAlias, requiredFields);
            case ExecutionForEachWithOrdinality forEach:
                return TryCollectRequiredPayloadFieldNames(forEach.Source, payloadAlias, requiredFields) &&
                       TryCollectRequiredPayloadFieldNames(forEach.Body, payloadAlias, requiredFields);
            case ExecutionAppendRow appendRow:
                return TryCollectRequiredPayloadFieldNames(appendRow.Values.Select(static value => value.Value), payloadAlias, requiredFields) &&
                       TryCollectRequiredPayloadFieldNames(appendRow.Contexts, payloadAlias, requiredFields) &&
                       TryCollectRequiredPayloadFieldNames(GetContextLayoutExpressions(appendRow.ContextLayout), payloadAlias, requiredFields);
            case ExecutionHashProbe hashProbe:
                return TryCollectRequiredPayloadFieldNames(hashProbe.Key, payloadAlias, requiredFields) &&
                       TryCollectRequiredPayloadFieldNames(hashProbe.Body, payloadAlias, requiredFields) &&
                       (hashProbe.NoMatchBody == null ||
                        TryCollectRequiredPayloadFieldNames(hashProbe.NoMatchBody, payloadAlias, requiredFields));
            default:
                return true;
        }
    }

    private static bool TryCollectRequiredPayloadFieldNames(
        IEnumerable<ExecutionExpression> expressions,
        string payloadAlias,
        ISet<string> requiredFields)
    {
        foreach (var expression in expressions)
        {
            if (!TryCollectRequiredPayloadFieldNames(expression, payloadAlias, requiredFields))
                return false;
        }

        return true;
    }

    private static bool TryCollectRequiredPayloadFieldNames(
        ExecutionExpression expression,
        string payloadAlias,
        ISet<string> requiredFields)
    {
        switch (expression)
        {
            case ExecutionFieldRead fieldRead
                when string.Equals(fieldRead.Alias, payloadAlias, StringComparison.OrdinalIgnoreCase):
                if (fieldRead.AccessStrategy is not GeneratedFieldAccess generated)
                    return false;

                requiredFields.Add(generated.FieldName);
                return true;
            case ExecutionBinary binary:
                return TryCollectRequiredPayloadFieldNames(binary.Left, payloadAlias, requiredFields) &&
                       TryCollectRequiredPayloadFieldNames(binary.Right, payloadAlias, requiredFields);
            case ExecutionUnary unary:
                return TryCollectRequiredPayloadFieldNames(unary.Operand, payloadAlias, requiredFields);
            case ExecutionStrictCast strictCast:
                return TryCollectRequiredPayloadFieldNames(strictCast.Expression, payloadAlias, requiredFields);
            case ExecutionMethodCall method:
                return TryCollectRequiredPayloadFieldNames(method.Arguments, payloadAlias, requiredFields) &&
                       (method.InjectedSource == null ||
                        TryCollectRequiredPayloadFieldNames(method.InjectedSource, payloadAlias, requiredFields));
            case ExecutionArrayAccess arrayAccess:
                return TryCollectRequiredPayloadFieldNames(arrayAccess.Array, payloadAlias, requiredFields) &&
                       TryCollectRequiredPayloadFieldNames(arrayAccess.Index, payloadAlias, requiredFields);
            case ExecutionIsNullCheck isNull:
                return TryCollectRequiredPayloadFieldNames(isNull.Expression, payloadAlias, requiredFields);
            case ExecutionInCheck inCheck:
                return TryCollectRequiredPayloadFieldNames(inCheck.Expression, payloadAlias, requiredFields) &&
                       TryCollectRequiredPayloadFieldNames(inCheck.Values, payloadAlias, requiredFields);
            case ExecutionPatternMatch pattern:
                return TryCollectRequiredPayloadFieldNames(pattern.Expression, payloadAlias, requiredFields) &&
                       TryCollectRequiredPayloadFieldNames(pattern.Pattern, payloadAlias, requiredFields);
            case ExecutionBetween between:
                return TryCollectRequiredPayloadFieldNames(between.Expression, payloadAlias, requiredFields) &&
                       TryCollectRequiredPayloadFieldNames(between.Low, payloadAlias, requiredFields) &&
                       TryCollectRequiredPayloadFieldNames(between.High, payloadAlias, requiredFields);
            case ExecutionCaseWhen caseWhen:
                return TryCollectRequiredPayloadFieldNames(caseWhen, payloadAlias, requiredFields);
            case ExecutionCoalesce coalesce:
                return TryCollectRequiredPayloadFieldNames(coalesce.Expressions, payloadAlias, requiredFields);
            case ExecutionCompositeKey compositeKey:
                return TryCollectRequiredPayloadFieldNames(compositeKey.Parts, payloadAlias, requiredFields);
            case ExecutionValueTupleKey valueTupleKey:
                return TryCollectRequiredPayloadFieldNames(valueTupleKey.Parts, payloadAlias, requiredFields);
            case ExecutionAggregateCall aggregateCall:
                return TryCollectRequiredPayloadFieldNames(aggregateCall.Arguments, payloadAlias, requiredFields);
            default:
                return true;
        }
    }

    private static bool TryCollectRequiredPayloadFieldNames(
        ExecutionCaseWhen caseWhen,
        string payloadAlias,
        ISet<string> requiredFields)
    {
        foreach (var branch in caseWhen.Branches)
        {
            if (!TryCollectRequiredPayloadFieldNames(branch.Condition, payloadAlias, requiredFields) ||
                !TryCollectRequiredPayloadFieldNames(branch.Result, payloadAlias, requiredFields))
            {
                return false;
            }
        }

        return caseWhen.ElseExpression == null ||
               TryCollectRequiredPayloadFieldNames(caseWhen.ElseExpression, payloadAlias, requiredFields);
    }

    private static IEnumerable<ExecutionExpression> GetContextLayoutExpressions(ExecutionContextLayout? contextLayout)
    {
        return contextLayout == null
            ? []
            : contextLayout.Segments.Select(static segment => segment.Value);
    }
}
