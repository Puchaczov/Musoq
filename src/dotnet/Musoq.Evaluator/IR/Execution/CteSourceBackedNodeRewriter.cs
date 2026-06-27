using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class CteSourceBackedNodeRewriter
{
    private readonly string _sourceAlias;
    private readonly IReadOnlyDictionary<string, ExecutionExpression> _fieldMap;
    private readonly ExecutionAppendRow _sourceAppend;

    private CteSourceBackedNodeRewriter(
        string sourceAlias,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        ExecutionAppendRow sourceAppend)
    {
        _sourceAlias = sourceAlias;
        _fieldMap = fieldMap;
        _sourceAppend = sourceAppend;
    }

    public static bool TryRewriteBlock(
        ExecutionBlock block,
        string sourceAlias,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        ExecutionAppendRow sourceAppend,
        [NotNullWhen(true)] out ExecutionBlock? rewritten)
    {
        return new CteSourceBackedNodeRewriter(sourceAlias, fieldMap, sourceAppend)
            .TryRewriteBlock(block, out rewritten);
    }

    private bool TryRewriteBlock(
        ExecutionBlock block,
        [NotNullWhen(true)] out ExecutionBlock? rewritten)
    {
        var nodes = new ExecutionNode[block.Nodes.Count];
        for (var index = 0; index < block.Nodes.Count; index++)
        {
            if (!TryRewriteNode(block.Nodes[index], out var node))
            {
                rewritten = null;
                return false;
            }

            nodes[index] = node;
        }

        rewritten = new ExecutionBlock(nodes);
        return true;
    }

    private bool TryRewriteNode(
        ExecutionNode node,
        [NotNullWhen(true)] out ExecutionNode? rewritten)
    {
        switch (node)
        {
            case ExecutionIf branch:
                if (!TryRewriteExpression(branch.Condition, out var condition) ||
                    !TryRewriteBlock(branch.Body, out var body))
                    return Fail(out rewritten);
                rewritten = branch with { Condition = condition, Body = body };
                return true;
            case ExecutionCreateHashPayload payload:
                if (!TryRewriteValues(payload.Values, out var payloadValues))
                    return Fail(out rewritten);
                rewritten = payload with { Values = payloadValues };
                return true;
            case ExecutionCteSidecarAppendRewriteCandidate candidate:
                if (!TryRewriteAppendRow(candidate.AppendRow, out var appendRow) ||
                    !TryRewriteSidecarAppendIndexes(candidate.Indexes, out var indexes))
                    return Fail(out rewritten);
                rewritten = candidate with { AppendRow = appendRow, Indexes = indexes };
                return true;
            case ExecutionHashAdd hashAdd:
                if (!TryRewriteExpression(hashAdd.Key, out var hashKey))
                    return Fail(out rewritten);
                rewritten = hashAdd with { Key = hashKey, PrecomputedKey = null };
                return true;
            case ExecutionKeySetAdd keySetAdd:
                if (!TryRewriteExpression(keySetAdd.Key, out var keySetKey))
                    return Fail(out rewritten);
                rewritten = keySetAdd with { Key = keySetKey, PrecomputedKey = null };
                return true;
            case ExecutionLet let:
                if (!TryRewriteExpression(let.Value, out var value))
                    return Fail(out rewritten);
                rewritten = let with { Value = value };
                return true;
            case ExecutionContinueIf continueIf:
                if (!TryRewriteExpression(continueIf.Condition, out var continueCondition))
                    return Fail(out rewritten);
                rewritten = continueIf with { Condition = continueCondition };
                return true;
            case ExecutionAppendRow:
                return Fail(out rewritten);
            default:
                rewritten = node;
                return true;
        }
    }

    private bool TryRewriteValues(
        IReadOnlyList<ExecutionRowValue> values,
        [NotNullWhen(true)] out IReadOnlyList<ExecutionRowValue>? rewritten)
    {
        var results = new ExecutionRowValue[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            if (!TryRewriteExpression(values[index].Value, out var value))
            {
                rewritten = null;
                return false;
            }

            results[index] = values[index] with { Value = value };
        }

        rewritten = results;
        return true;
    }

    private bool TryRewriteAppendRow(
        ExecutionAppendRow appendRow,
        [NotNullWhen(true)] out ExecutionAppendRow? rewritten)
    {
        if (!TryRewriteValues(appendRow.Values, out var values) ||
            !TryRewriteExpressionList(appendRow.Contexts, out var contexts) ||
            !TryRewriteContextLayout(appendRow.ContextLayout, out var contextLayout))
        {
            rewritten = null;
            return false;
        }

        rewritten = appendRow with
        {
            Values = values,
            Contexts = contexts,
            ContextLayout = contextLayout
        };
        return true;
    }

    private bool TryRewriteSidecarAppendIndexes(
        IReadOnlyList<ExecutionCteSidecarAppendIndexSpec> indexes,
        [NotNullWhen(true)] out IReadOnlyList<ExecutionCteSidecarAppendIndexSpec>? rewritten)
    {
        var results = new ExecutionCteSidecarAppendIndexSpec[indexes.Count];
        for (var index = 0; index < indexes.Count; index++)
        {
            var spec = indexes[index];
            if (!TryRewriteExpression(spec.Key, out var key) ||
                !TryRewriteValues(spec.PayloadValues, out var payloadValues))
            {
                rewritten = null;
                return false;
            }

            results[index] = spec with { Key = key, PayloadValues = payloadValues };
        }

        rewritten = results;
        return true;
    }

    private bool TryRewriteExpression(
        ExecutionExpression expression,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        switch (expression)
        {
            case ExecutionFieldRead fieldRead when string.Equals(fieldRead.Alias, _sourceAlias, StringComparison.OrdinalIgnoreCase):
                return CteSourceBackedFieldMap.TryResolveFieldRead(fieldRead, _fieldMap, _sourceAppend, out rewritten);
            case ExecutionVariableRead variableRead when string.Equals(variableRead.Variable.Name, _sourceAlias, StringComparison.Ordinal):
            case ExecutionRowContextsRead contextsRead when string.Equals(contextsRead.Row.Name, _sourceAlias, StringComparison.Ordinal):
                return Fail(out rewritten);
            case ExecutionFieldRead:
            case ExecutionLiteral:
            case ExecutionVariableRead:
            case ExecutionScriptParameterRead:
            case ExecutionScriptVariableRead:
                rewritten = expression;
                return true;
            case ExecutionBinary binary:
                return TryRewriteBinary(binary, out rewritten);
            case ExecutionUnary unary:
                return TryRewriteSingle(unary.Operand, operand =>
                    unary with { Operand = operand }, out rewritten);
            case ExecutionStrictCast strictCast:
                return TryRewriteSingle(strictCast.Expression, value =>
                    strictCast with { Expression = value }, out rewritten);
            case ExecutionIsNullCheck isNull:
                return TryRewriteSingle(isNull.Expression, rewrittenExpression =>
                    isNull with { Expression = rewrittenExpression }, out rewritten);
            case ExecutionInCheck inCheck:
                return TryRewriteInCheck(inCheck, out rewritten);
            case ExecutionPatternMatch pattern:
                return TryRewritePattern(pattern, out rewritten);
            case ExecutionBetween between:
                return TryRewriteBetween(between, out rewritten);
            case ExecutionCaseWhen caseWhen:
                return TryRewriteCaseWhen(caseWhen, out rewritten);
            case ExecutionCoalesce coalesce:
                return TryRewriteExpressionList(coalesce.Expressions, out var coalesceExpressions)
                    ? Succeed(coalesce with { Expressions = coalesceExpressions }, out rewritten)
                    : Fail(out rewritten);
            case ExecutionArrayAccess arrayAccess:
                return TryRewriteArrayAccess(arrayAccess, out rewritten);
            case ExecutionCompositeKey composite:
                return TryRewriteExpressionList(composite.Parts, out var compositeParts)
                    ? Succeed(composite with { Parts = compositeParts }, out rewritten)
                    : Fail(out rewritten);
            case ExecutionValueTupleKey tuple:
                return TryRewriteExpressionList(tuple.Parts, out var tupleParts)
                    ? Succeed(tuple with { Parts = tupleParts }, out rewritten)
                    : Fail(out rewritten);
            case ExecutionContextArray contextArray:
                return TryRewriteContextArray(contextArray, out rewritten);
            default:
                rewritten = null;
                return false;
        }
    }

    private bool TryRewriteBinary(
        ExecutionBinary binary,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewriteExpression(binary.Left, out var left) ||
            !TryRewriteExpression(binary.Right, out var right))
            return Fail(out rewritten);

        return Succeed(binary with { Left = left, Right = right }, out rewritten);
    }

    private bool TryRewriteInCheck(
        ExecutionInCheck inCheck,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewriteExpression(inCheck.Expression, out var expression) ||
            !TryRewriteExpressionList(inCheck.Values, out var values))
            return Fail(out rewritten);

        return Succeed(inCheck with { Expression = expression, Values = values }, out rewritten);
    }

    private bool TryRewritePattern(
        ExecutionPatternMatch pattern,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewriteExpression(pattern.Expression, out var expression) ||
            !TryRewriteExpression(pattern.Pattern, out var patternValue))
            return Fail(out rewritten);

        return Succeed(pattern with { Expression = expression, Pattern = patternValue }, out rewritten);
    }

    private bool TryRewriteBetween(
        ExecutionBetween between,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewriteExpression(between.Expression, out var expression) ||
            !TryRewriteExpression(between.Low, out var low) ||
            !TryRewriteExpression(between.High, out var high))
            return Fail(out rewritten);

        return Succeed(between with { Expression = expression, Low = low, High = high }, out rewritten);
    }

    private bool TryRewriteCaseWhen(
        ExecutionCaseWhen caseWhen,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        var branches = new ExecutionCaseWhenBranch[caseWhen.Branches.Count];
        for (var index = 0; index < caseWhen.Branches.Count; index++)
        {
            var branch = caseWhen.Branches[index];
            if (!TryRewriteExpression(branch.Condition, out var condition) ||
                !TryRewriteExpression(branch.Result, out var result))
                return Fail(out rewritten);

            branches[index] = branch with { Condition = condition, Result = result };
        }

        ExecutionExpression? elseExpression = null;
        if (caseWhen.ElseExpression != null &&
            !TryRewriteExpression(caseWhen.ElseExpression, out elseExpression))
            return Fail(out rewritten);

        return Succeed(caseWhen with { Branches = branches, ElseExpression = elseExpression }, out rewritten);
    }

    private bool TryRewriteArrayAccess(
        ExecutionArrayAccess arrayAccess,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewriteExpression(arrayAccess.Array, out var array) ||
            !TryRewriteExpression(arrayAccess.Index, out var index))
            return Fail(out rewritten);

        return Succeed(arrayAccess with { Array = array, Index = index }, out rewritten);
    }

    private bool TryRewriteContextArray(
        ExecutionContextArray contextArray,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        var segments = new ExecutionContextSegment[contextArray.Segments.Count];
        for (var index = 0; index < contextArray.Segments.Count; index++)
        {
            var segment = contextArray.Segments[index];
            if (!TryRewriteExpression(segment.Value, out var value))
                return Fail(out rewritten);

            segments[index] = segment with { Value = value };
        }

        return Succeed(contextArray with { Segments = segments }, out rewritten);
    }

    private bool TryRewriteContextLayout(
        ExecutionContextLayout? contextLayout,
        out ExecutionContextLayout? rewritten)
    {
        if (contextLayout == null)
        {
            rewritten = null;
            return true;
        }

        var segments = new ExecutionContextSegment[contextLayout.Segments.Count];
        for (var index = 0; index < contextLayout.Segments.Count; index++)
        {
            var segment = contextLayout.Segments[index];
            if (!TryRewriteExpression(segment.Value, out var value))
            {
                rewritten = null;
                return false;
            }

            segments[index] = segment with { Value = value };
        }

        rewritten = contextLayout with { Segments = segments };
        return true;
    }

    private bool TryRewriteExpressionList(
        IReadOnlyList<ExecutionExpression> expressions,
        [NotNullWhen(true)] out IReadOnlyList<ExecutionExpression>? rewritten)
    {
        var results = new ExecutionExpression[expressions.Count];
        for (var index = 0; index < expressions.Count; index++)
        {
            if (!TryRewriteExpression(expressions[index], out var expression))
            {
                rewritten = null;
                return false;
            }

            results[index] = expression;
        }

        rewritten = results;
        return true;
    }

    private bool TryRewriteSingle(
        ExecutionExpression expression,
        Func<ExecutionExpression, ExecutionExpression> create,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewriteExpression(expression, out var result))
            return Fail(out rewritten);

        return Succeed(create(result), out rewritten);
    }

    private static bool Succeed(
        ExecutionExpression value,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        rewritten = value;
        return true;
    }

    private static bool Fail<T>(out T? value)
    {
        value = default;
        return false;
    }
}
