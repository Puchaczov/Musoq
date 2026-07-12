using System;

namespace Musoq.Evaluator.IR.Execution;

internal abstract partial class ExecutionIrRewriter
{
    public virtual ExecutionExpression RewriteExpression(ExecutionExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return expression switch
        {
            ExecutionFieldRead fieldRead => RewriteFieldRead(fieldRead),
            ExecutionScriptParameterRead parameterRead => RewriteScriptParameterRead(parameterRead),
            ExecutionScriptVariableRead variableRead => RewriteScriptVariableRead(variableRead),
            ExecutionLiteral literal => RewriteLiteral(literal),
            ExecutionBinary binary => RewriteBinary(binary),
            ExecutionUnary unary => RewriteUnary(unary),
            ExecutionMethodCall methodCall => RewriteMethodCall(methodCall),
            ExecutionStrictCast strictCast => RewriteStrictCast(strictCast),
            ExecutionMethodTargetReuseCandidate candidate => RewriteMethodTargetReuseCandidate(candidate),
            ExecutionArrayAccess arrayAccess => RewriteArrayAccess(arrayAccess),
            ExecutionIndexedHashRowCreate indexedCreate => RewriteIndexedHashRowCreate(indexedCreate),
            ExecutionIndexedHashRowRowRead rowRead => RewriteIndexedHashRowRowRead(rowRead),
            ExecutionIndexedHashRowIndexRead indexRead => RewriteIndexedHashRowIndexRead(indexRead),
            ExecutionIsNullCheck isNull => RewriteIsNullCheck(isNull),
            ExecutionRowPresence rowPresence => RewriteRowPresence(rowPresence),
            ExecutionInCheck inCheck => RewriteInCheck(inCheck),
            ExecutionCollectionInCheck collectionInCheck => RewriteCollectionInCheck(collectionInCheck),
            ExecutionPatternMatch patternMatch => RewritePatternMatch(patternMatch),
            ExecutionBetween between => RewriteBetween(between),
            ExecutionCaseWhen caseWhen => RewriteCaseWhen(caseWhen),
            ExecutionCoalesce coalesce => RewriteCoalesce(coalesce),
            ExecutionRowStream rows => RewriteRowStream(rows),
            ExecutionScalarRowStream rows => RewriteScalarRowStream(rows),
            ExecutionStoredTable storedTable => RewriteStoredTable(storedTable),
            ExecutionStoredTableRows storedTableRows => RewriteStoredTableRows(storedTableRows),
            ExecutionVariableRead variableRead => RewriteVariableRead(variableRead),
            ExecutionRowContextsRead rowContextsRead => RewriteRowContextsRead(rowContextsRead),
            ExecutionNullContextArray nullContextArray => RewriteNullContextArray(nullContextArray),
            ExecutionContextArray contextArray => RewriteContextArray(contextArray),
            ExecutionCompositeKey compositeKey => RewriteCompositeKey(compositeKey),
            ExecutionValueTupleKey valueTupleKey => RewriteValueTupleKey(valueTupleKey),
            ExecutionWindowValueRead windowValueRead => RewriteWindowValueRead(windowValueRead),
            ExecutionAggregateCall aggregateCall => RewriteAggregateCall(aggregateCall),
            ExecutionAggregateResultRef aggregateResultRef => RewriteAggregateResultRef(aggregateResultRef),
            ExecutionWindowResultRef windowResultRef => RewriteWindowResultRef(windowResultRef),
            ExecutionGroupKeyRead groupKeyRead => RewriteGroupKeyRead(groupKeyRead),
            ExecutionAggregateCapturedValueRead capturedValueRead => RewriteAggregateCapturedValueRead(capturedValueRead),
            _ => throw new NotSupportedException($"Execution expression rewriter has no handler for '{expression.GetType().FullName}'.")
        };
    }

    protected virtual ExecutionExpression RewriteFieldRead(ExecutionFieldRead expression) => expression;

    protected virtual ExecutionExpression RewriteScriptParameterRead(ExecutionScriptParameterRead expression) => expression;

    protected virtual ExecutionExpression RewriteScriptVariableRead(ExecutionScriptVariableRead expression) => expression;

    protected virtual ExecutionExpression RewriteLiteral(ExecutionLiteral expression) => expression;

    protected virtual ExecutionExpression RewriteBinary(ExecutionBinary expression)
    {
        var left = RewriteExpression(expression.Left);
        var right = RewriteExpression(expression.Right);
        return ReferenceEquals(left, expression.Left) && ReferenceEquals(right, expression.Right)
            ? expression
            : expression with { Left = left, Right = right };
    }

    protected virtual ExecutionExpression RewriteUnary(ExecutionUnary expression)
    {
        var operand = RewriteExpression(expression.Operand);
        return ReferenceEquals(operand, expression.Operand) ? expression : expression with { Operand = operand };
    }

    protected virtual ExecutionExpression RewriteMethodCall(ExecutionMethodCall expression)
    {
        var arguments = RewriteExpressionList(expression.Arguments);
        var injectedSource = RewriteOptionalExpression(expression.InjectedSource);
        return ReferenceEquals(arguments, expression.Arguments) &&
               ReferenceEquals(injectedSource, expression.InjectedSource)
            ? expression
            : expression with { Arguments = arguments, InjectedSource = injectedSource };
    }

    protected virtual ExecutionExpression RewriteStrictCast(ExecutionStrictCast expression)
    {
        var value = RewriteExpression(expression.Expression);
        return ReferenceEquals(value, expression.Expression) ? expression : expression with { Expression = value };
    }

    protected virtual ExecutionExpression RewriteMethodTargetReuseCandidate(ExecutionMethodTargetReuseCandidate expression)
    {
        var methodCall = (ExecutionMethodCall)RewriteMethodCall(expression.MethodCall);
        return ReferenceEquals(methodCall, expression.MethodCall)
            ? expression
            : expression with { MethodCall = methodCall };
    }

    protected virtual ExecutionExpression RewriteArrayAccess(ExecutionArrayAccess expression)
    {
        var array = RewriteExpression(expression.Array);
        var index = RewriteExpression(expression.Index);
        return ReferenceEquals(array, expression.Array) && ReferenceEquals(index, expression.Index)
            ? expression
            : expression with { Array = array, Index = index };
    }

    protected virtual ExecutionExpression RewriteIndexedHashRowCreate(ExecutionIndexedHashRowCreate expression) => expression;

    protected virtual ExecutionExpression RewriteIndexedHashRowRowRead(ExecutionIndexedHashRowRowRead expression) => expression;

    protected virtual ExecutionExpression RewriteIndexedHashRowIndexRead(ExecutionIndexedHashRowIndexRead expression) => expression;

    protected virtual ExecutionExpression RewriteIsNullCheck(ExecutionIsNullCheck expression)
    {
        var value = RewriteExpression(expression.Expression);
        return ReferenceEquals(value, expression.Expression) ? expression : expression with { Expression = value };
    }

    protected virtual ExecutionExpression RewriteRowPresence(ExecutionRowPresence expression)
    {
        var source = RewriteExpression(expression.PresenceSource);
        return ReferenceEquals(source, expression.PresenceSource) ? expression : expression with { PresenceSource = source };
    }

    protected virtual ExecutionExpression RewriteInCheck(ExecutionInCheck expression)
    {
        var value = RewriteExpression(expression.Expression);
        var values = RewriteExpressionList(expression.Values);
        return ReferenceEquals(value, expression.Expression) && ReferenceEquals(values, expression.Values)
            ? expression
            : expression with { Expression = value, Values = values };
    }

    protected virtual ExecutionExpression RewritePatternMatch(ExecutionPatternMatch expression)
    {
        var value = RewriteExpression(expression.Expression);
        var pattern = RewriteExpression(expression.Pattern);
        return ReferenceEquals(value, expression.Expression) && ReferenceEquals(pattern, expression.Pattern)
            ? expression
            : expression with { Expression = value, Pattern = pattern };
    }

    protected virtual ExecutionExpression RewriteBetween(ExecutionBetween expression)
    {
        var value = RewriteExpression(expression.Expression);
        var low = RewriteExpression(expression.Low);
        var high = RewriteExpression(expression.High);
        return ReferenceEquals(value, expression.Expression) &&
               ReferenceEquals(low, expression.Low) &&
               ReferenceEquals(high, expression.High)
            ? expression
            : expression with { Expression = value, Low = low, High = high };
    }

    protected virtual ExecutionExpression RewriteCaseWhen(ExecutionCaseWhen expression)
    {
        var branches = RewriteList(expression.Branches, RewriteCaseWhenBranch);
        var elseExpression = RewriteOptionalExpression(expression.ElseExpression);
        return ReferenceEquals(branches, expression.Branches) &&
               ReferenceEquals(elseExpression, expression.ElseExpression)
            ? expression
            : expression with { Branches = branches, ElseExpression = elseExpression };
    }

    protected virtual ExecutionExpression RewriteCoalesce(ExecutionCoalesce expression)
    {
        var expressions = RewriteExpressionList(expression.Expressions);
        return ReferenceEquals(expressions, expression.Expressions)
            ? expression
            : expression with { Expressions = expressions };
    }

    protected virtual ExecutionExpression RewriteRowStream(ExecutionRowStream expression) => expression;

    protected virtual ExecutionExpression RewriteScalarRowStream(ExecutionScalarRowStream expression) => expression;

    protected virtual ExecutionExpression RewriteStoredTable(ExecutionStoredTable expression) => expression;

    protected virtual ExecutionExpression RewriteStoredTableRows(ExecutionStoredTableRows expression) => expression;

    protected virtual ExecutionExpression RewriteVariableRead(ExecutionVariableRead expression) => expression;

    protected virtual ExecutionExpression RewriteRowContextsRead(ExecutionRowContextsRead expression) => expression;

    protected virtual ExecutionExpression RewriteNullContextArray(ExecutionNullContextArray expression) => expression;

    protected virtual ExecutionExpression RewriteContextArray(ExecutionContextArray expression)
    {
        var segments = RewriteContextSegments(expression.Segments);
        return ReferenceEquals(segments, expression.Segments) ? expression : expression with { Segments = segments };
    }

    protected virtual ExecutionExpression RewriteCompositeKey(ExecutionCompositeKey expression)
    {
        var parts = RewriteExpressionList(expression.Parts);
        return ReferenceEquals(parts, expression.Parts) ? expression : expression with { Parts = parts };
    }

    protected virtual ExecutionExpression RewriteValueTupleKey(ExecutionValueTupleKey expression)
    {
        var parts = RewriteExpressionList(expression.Parts);
        return ReferenceEquals(parts, expression.Parts) ? expression : expression with { Parts = parts };
    }

    protected virtual ExecutionExpression RewriteWindowValueRead(ExecutionWindowValueRead expression) => expression;

    protected virtual ExecutionExpression RewriteAggregateCall(ExecutionAggregateCall expression)
    {
        var arguments = RewriteExpressionList(expression.Arguments);
        return ReferenceEquals(arguments, expression.Arguments) ? expression : expression with { Arguments = arguments };
    }

    protected virtual ExecutionExpression RewriteGroupKeyRead(ExecutionGroupKeyRead expression) => expression;

    protected virtual ExecutionExpression RewriteAggregateCapturedValueRead(ExecutionAggregateCapturedValueRead expression) => expression;

    protected virtual ExecutionExpression RewriteAggregateResultRef(ExecutionAggregateResultRef expression) => expression;

    protected virtual ExecutionExpression RewriteWindowResultRef(ExecutionWindowResultRef expression) => expression;
}
