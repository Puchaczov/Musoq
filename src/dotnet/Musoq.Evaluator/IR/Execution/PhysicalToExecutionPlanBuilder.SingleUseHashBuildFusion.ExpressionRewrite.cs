using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static bool TryRewritePayloadKeyToProducerKey(
        ExecutionExpression expression,
        string payloadAlias,
        GeneratedRowShape rowShape,
        IReadOnlyList<ExecutionRowValue> rowValues,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        var fieldMap = CreatePayloadFieldValueMap(rowShape.Fields, rowValues);
        return TryRewritePayloadKeyToProducerKey(expression, payloadAlias, fieldMap, out rewritten) &&
               !ReferencesExecutionAlias(rewritten, payloadAlias);
    }

    private static Dictionary<string, ExecutionExpression> CreatePayloadFieldValueMap(
        IReadOnlyList<FieldBinding> fields,
        IReadOnlyList<ExecutionRowValue> payloadValues)
    {
        var map = new Dictionary<string, ExecutionExpression>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < fields.Count && index < payloadValues.Count; index++)
        {
            var field = fields[index];
            AddPayloadFieldValue(map, field.Name, payloadValues[index].Value);
            AddPayloadFieldValue(map, field.QualifiedName, payloadValues[index].Value);

            if (field.AccessStrategy is GeneratedFieldAccess generated)
                AddPayloadFieldValue(map, generated.FieldName, payloadValues[index].Value);
        }

        return map;
    }

    private static void AddPayloadFieldValue(
        IDictionary<string, ExecutionExpression> map,
        string key,
        ExecutionExpression value)
    {
        if (!string.IsNullOrWhiteSpace(key))
            map.TryAdd(key, value);
    }

    private static bool TryRewritePayloadKeyToProducerKey(
        ExecutionExpression expression,
        string payloadAlias,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        switch (expression)
        {
            case ExecutionFieldRead fieldRead
                when string.Equals(fieldRead.Alias, payloadAlias, StringComparison.OrdinalIgnoreCase):
                return TryRewritePayloadFieldRead(fieldRead, fieldMap, out rewritten);
            case ExecutionFieldRead:
            case ExecutionLiteral:
            case ExecutionVariableRead:
            case ExecutionScriptParameterRead:
            case ExecutionScriptVariableRead:
                rewritten = expression;
                return true;
            case ExecutionBinary binary:
                return TryRewriteBinaryPayloadKey(binary, payloadAlias, fieldMap, out rewritten);
            case ExecutionUnary unary:
                return TryRewriteUnaryPayloadKey(unary, payloadAlias, fieldMap, out rewritten);
            case ExecutionStrictCast strictCast:
                return TryRewriteStrictCastPayloadKey(strictCast, payloadAlias, fieldMap, out rewritten);
            case ExecutionMethodCall { Target: null, Cache: null } method:
                return TryRewriteMethodPayloadKey(method, payloadAlias, fieldMap, out rewritten);
            case ExecutionIsNullCheck isNull:
                return TryRewriteIsNullPayloadKey(isNull, payloadAlias, fieldMap, out rewritten);
            case ExecutionCoalesce coalesce:
                return TryRewriteCoalescePayloadKey(coalesce, payloadAlias, fieldMap, out rewritten);
            case ExecutionValueTupleKey valueTupleKey:
                return TryRewriteValueTuplePayloadKey(valueTupleKey, payloadAlias, fieldMap, out rewritten);
            default:
                rewritten = null;
                return false;
        }
    }

    private static bool TryRewritePayloadFieldRead(
        ExecutionFieldRead fieldRead,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (fieldRead.AccessStrategy is GeneratedFieldAccess generated &&
            fieldMap.TryGetValue(generated.FieldName, out rewritten))
        {
            return true;
        }

        if (fieldMap.TryGetValue(fieldRead.FieldName, out rewritten))
            return true;

        rewritten = null;
        return false;
    }

    private static bool TryRewriteBinaryPayloadKey(
        ExecutionBinary binary,
        string payloadAlias,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewritePayloadKeyToProducerKey(binary.Left, payloadAlias, fieldMap, out var left) ||
            !TryRewritePayloadKeyToProducerKey(binary.Right, payloadAlias, fieldMap, out var right))
        {
            rewritten = null;
            return false;
        }

        rewritten = binary with { Left = left, Right = right };
        return true;
    }

    private static bool TryRewriteUnaryPayloadKey(
        ExecutionUnary unary,
        string payloadAlias,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewritePayloadKeyToProducerKey(unary.Operand, payloadAlias, fieldMap, out var operand))
        {
            rewritten = null;
            return false;
        }

        rewritten = unary with { Operand = operand };
        return true;
    }

    private static bool TryRewriteMethodPayloadKey(
        ExecutionMethodCall method,
        string payloadAlias,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewritePayloadKeyExpressions(method.Arguments, payloadAlias, fieldMap, out var arguments))
        {
            rewritten = null;
            return false;
        }

        ExecutionExpression? injectedSource = null;
        if (method.InjectedSource != null &&
            !TryRewritePayloadKeyToProducerKey(method.InjectedSource, payloadAlias, fieldMap, out injectedSource))
        {
            rewritten = null;
            return false;
        }

        rewritten = method with
        {
            Arguments = arguments,
            InjectedSource = injectedSource
        };
        return true;
    }

    private static bool TryRewriteStrictCastPayloadKey(
        ExecutionStrictCast strictCast,
        string payloadAlias,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewritePayloadKeyToProducerKey(strictCast.Expression, payloadAlias, fieldMap, out var expression))
        {
            rewritten = null;
            return false;
        }

        rewritten = strictCast with { Expression = expression };
        return true;
    }

    private static bool TryRewriteIsNullPayloadKey(
        ExecutionIsNullCheck isNull,
        string payloadAlias,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewritePayloadKeyToProducerKey(isNull.Expression, payloadAlias, fieldMap, out var expression))
        {
            rewritten = null;
            return false;
        }

        rewritten = isNull with { Expression = expression };
        return true;
    }

    private static bool TryRewriteCoalescePayloadKey(
        ExecutionCoalesce coalesce,
        string payloadAlias,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewritePayloadKeyExpressions(coalesce.Expressions, payloadAlias, fieldMap, out var expressions))
        {
            rewritten = null;
            return false;
        }

        rewritten = coalesce with { Expressions = expressions };
        return true;
    }

    private static bool TryRewriteValueTuplePayloadKey(
        ExecutionValueTupleKey valueTupleKey,
        string payloadAlias,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewritePayloadKeyExpressions(valueTupleKey.Parts, payloadAlias, fieldMap, out var parts))
        {
            rewritten = null;
            return false;
        }

        rewritten = valueTupleKey with { Parts = parts };
        return true;
    }

    private static bool TryRewritePayloadKeyExpressions(
        IReadOnlyList<ExecutionExpression> expressions,
        string payloadAlias,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        [NotNullWhen(true)] out IReadOnlyList<ExecutionExpression>? rewritten)
    {
        var values = new ExecutionExpression[expressions.Count];
        for (var index = 0; index < expressions.Count; index++)
        {
            if (!TryRewritePayloadKeyToProducerKey(expressions[index], payloadAlias, fieldMap, out var rewrittenExpression))
            {
                rewritten = null;
                return false;
            }

            values[index] = rewrittenExpression;
        }

        rewritten = values;
        return true;
    }
}
