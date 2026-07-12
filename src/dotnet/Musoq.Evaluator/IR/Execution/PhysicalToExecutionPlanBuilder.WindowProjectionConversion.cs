using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static BuildResult<ExecutionExpression> ConvertWindowProjectionExpression(
        ProjectedField field,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        if (field.Expression is WindowFunctionRef windowRef)
            return ConvertWindowFunctionRef(windowRef, windowResults, windowIndex);

        var aggregateRead = ResolveWindowAggregateSourceRead(field.Expression, sourceLookup, aggregateSourceFields);
        if (aggregateRead != null)
            return BuildResult<ExecutionExpression>.Success(aggregateRead);

        var expression = ConvertWindowExpression(field.Expression, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        return expression.Supported
            ? expression
            : BuildResult<ExecutionExpression>.Unsupported(
                $"Execution IR window lowering cannot convert projection {field.OutputName}={IrExpressionPrinter.Print(field.Expression)}. {expression.UnsupportedReason}");
    }

    private static BuildResult<ExecutionExpression> ConvertWindowExpression(
        IrExpression expression,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        switch (expression)
        {
            case WindowFunctionRef windowRef: return ConvertWindowFunctionRef(windowRef, windowResults, windowIndex);
            case AggregateRef:
                var aggregateRead = ResolveWindowAggregateSourceRead(expression, sourceLookup, aggregateSourceFields);
                return aggregateRead != null
                    ? BuildResult<ExecutionExpression>.Success(aggregateRead)
                    : BuildResult<ExecutionExpression>.Unsupported(
                        $"Execution IR window lowering cannot bind aggregate expression {IrExpressionPrinter.Print(expression)} to the window source.");
            case BinaryOp binary:
                var left = ConvertWindowExpression(binary.Left, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
                if (!left.Supported)
                    return BuildResult<ExecutionExpression>.Unsupported(left.UnsupportedReason);

                var right = ConvertWindowExpression(binary.Right, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
                if (!right.Supported)
                    return BuildResult<ExecutionExpression>.Unsupported(right.UnsupportedReason);

                return BuildResult<ExecutionExpression>.Success(new ExecutionBinary(binary.Kind, left.Value, right.Value, binary.ReturnType));
            case UnaryOp unary:
                var operand = ConvertWindowExpression(unary.Operand, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
                if (!operand.Supported)
                    return BuildResult<ExecutionExpression>.Unsupported(operand.UnsupportedReason);

                return BuildResult<ExecutionExpression>.Success(new ExecutionUnary(unary.Kind, operand.Value, unary.ReturnType));
            case ArrayAccess arrayAccess:
                return ConvertWindowArrayAccess(arrayAccess, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            case IsNullCheck isNull:
                return ConvertWindowIsNullCheck(isNull, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            case InCheck inCheck:
                return ConvertWindowInCheck(inCheck, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            case CollectionInCheck collectionInCheck: return ConvertWindowCollectionInCheck(collectionInCheck, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            case PatternMatch patternMatch:
                return ConvertWindowPatternMatch(patternMatch, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            case Between between:
                return ConvertWindowBetween(between, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            case CaseWhen caseWhen:
                return ConvertWindowCaseWhen(caseWhen, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            case Coalesce coalesce:
                return ConvertWindowCoalesce(coalesce, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            case MethodCall methodCall:
                return ConvertWindowMethodCall(methodCall, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            case StrictCast strictCast:
                return ConvertWindowStrictCast(strictCast, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            default:
                var converted = ExecutionExpressionConverter.Convert(expression, sourceLookup);
                return BuildResult<ExecutionExpression>.Success(converted);
        }
    }

    private static BuildResult<ExecutionExpression> ConvertWindowArrayAccess(
        ArrayAccess arrayAccess,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        var array = ConvertWindowExpression(arrayAccess.Array, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        if (!array.Supported)
            return array;

        var index = ConvertWindowExpression(arrayAccess.Index, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        if (!index.Supported)
            return index;

        return BuildResult<ExecutionExpression>.Success(new ExecutionArrayAccess(
            array.Value,
            index.Value,
            arrayAccess.ElementType,
            arrayAccess.ReturnType));
    }

    private static BuildResult<ExecutionExpression> ConvertWindowIsNullCheck(
        IsNullCheck isNull,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        var expression = ConvertWindowExpression(isNull.Expression, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        return expression.Supported
            ? BuildResult<ExecutionExpression>.Success(new ExecutionIsNullCheck(expression.Value, isNull.IsNegated, isNull.ReturnType))
            : expression;
    }

    private static BuildResult<ExecutionExpression> ConvertWindowInCheck(
        InCheck inCheck,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        var expression = ConvertWindowExpression(inCheck.Expression, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        if (!expression.Supported)
            return expression;

        var values = ConvertWindowExpressions(inCheck.Values, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        return values.Supported
            ? BuildResult<ExecutionExpression>.Success(new ExecutionInCheck(expression.Value, values.Value, inCheck.ReturnType))
            : BuildResult<ExecutionExpression>.Unsupported(values.UnsupportedReason);
    }

    private static BuildResult<ExecutionExpression> ConvertWindowPatternMatch(
        PatternMatch patternMatch,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        var expression = ConvertWindowExpression(patternMatch.Expression, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        if (!expression.Supported)
            return expression;

        var pattern = ConvertWindowExpression(patternMatch.Pattern, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        return pattern.Supported
            ? BuildResult<ExecutionExpression>.Success(new ExecutionPatternMatch(expression.Value, pattern.Value, patternMatch.Kind, patternMatch.ReturnType))
            : pattern;
    }

    private static BuildResult<ExecutionExpression> ConvertWindowBetween(
        Between between,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        var expression = ConvertWindowExpression(between.Expression, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        if (!expression.Supported)
            return expression;

        var low = ConvertWindowExpression(between.Low, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        if (!low.Supported)
            return low;

        var high = ConvertWindowExpression(between.High, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        return high.Supported
            ? BuildResult<ExecutionExpression>.Success(new ExecutionBetween(expression.Value, low.Value, high.Value, between.ReturnType))
            : high;
    }

    private static BuildResult<ExecutionExpression> ConvertWindowCaseWhen(
        CaseWhen caseWhen,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        var branches = new List<ExecutionCaseWhenBranch>(caseWhen.Branches.Length);
        foreach (var branch in caseWhen.Branches)
        {
            var condition = ConvertWindowExpression(branch.Condition, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            if (!condition.Supported)
                return condition;

            var result = ConvertWindowExpression(branch.Result, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            if (!result.Supported)
                return result;

            branches.Add(new ExecutionCaseWhenBranch(condition.Value, result.Value));
        }

        var elseExpression = caseWhen.ElseExpression == null
            ? null
            : ConvertWindowExpression(caseWhen.ElseExpression, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        if (elseExpression is { Supported: false })
            return BuildResult<ExecutionExpression>.Unsupported(elseExpression.UnsupportedReason);

        return BuildResult<ExecutionExpression>.Success(new ExecutionCaseWhen(
            branches,
            elseExpression?.Value,
            caseWhen.ReturnType));
    }

    private static BuildResult<ExecutionExpression> ConvertWindowCoalesce(
        Coalesce coalesce,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        var expressions = ConvertWindowExpressions(coalesce.Expressions, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        return expressions.Supported
            ? BuildResult<ExecutionExpression>.Success(new ExecutionCoalesce(expressions.Value, coalesce.ReturnType))
            : BuildResult<ExecutionExpression>.Unsupported(expressions.UnsupportedReason);
    }

    private static BuildResult<IReadOnlyList<ExecutionExpression>> ConvertWindowExpressions(
        IReadOnlyList<IrExpression> expressions,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        var converted = new List<ExecutionExpression>(expressions.Count);
        foreach (var expression in expressions)
        {
            var result = ConvertWindowExpression(expression, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            if (!result.Supported)
                return BuildResult<IReadOnlyList<ExecutionExpression>>.Unsupported(result.UnsupportedReason);

            converted.Add(result.Value);
        }

        return BuildResult<IReadOnlyList<ExecutionExpression>>.Success(converted);
    }

    private static BuildResult<ExecutionExpression> ConvertWindowMethodCall(
        MethodCall methodCall,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        var aggregateRead = ResolveWindowAggregateSourceRead(methodCall, sourceLookup, aggregateSourceFields);
        if (aggregateRead != null)
            return BuildResult<ExecutionExpression>.Success(aggregateRead);

        var arguments = new List<ExecutionExpression>(methodCall.Arguments.Count);
        foreach (var argument in methodCall.Arguments)
        {
            var converted = ConvertWindowExpression(argument, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
            if (!converted.Supported)
                return BuildResult<ExecutionExpression>.Unsupported(converted.UnsupportedReason);

            arguments.Add(converted.Value);
        }

        return BuildResult<ExecutionExpression>.Success(new ExecutionMethodCall(
            methodCall.Method,
            arguments,
            methodCall.Alias,
            methodCall.ReturnType,
            ExecutionExpressionConverter.CreateInjectedSourceExpression(methodCall.Method, methodCall.Alias, sourceLookup)));
    }

    private static BuildResult<ExecutionExpression> ConvertWindowStrictCast(
        StrictCast strictCast,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        var expression = ConvertWindowExpression(strictCast.Expression, sourceLookup, aggregateSourceFields, windowResults, windowIndex);
        return expression.Supported
            ? BuildResult<ExecutionExpression>.Success(new ExecutionStrictCast(
                expression.Value,
                strictCast.TargetTypeName,
                strictCast.ReturnType))
            : expression;
    }

}
