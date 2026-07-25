using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static bool CanUseAsOfProbeSource(RowShape sourceShape, Type rowType)
    {
        return sourceShape is SourceEntityShape or TableRowShape &&
               !rowType.IsValueType &&
               !DynamicEntityBoundary.IsDynamicMetaObjectProvider(rowType);
    }

    private static ExecutionExpression ReplaceExecutionAlias(
        ExecutionExpression expression,
        string fromAlias,
        string toAlias)
    {
        var replaced = ReplaceExecutionAliasCore(expression, fromAlias, toAlias);
        if (ReferencesExecutionAlias(replaced, fromAlias))
        {
            throw new NotSupportedException(
                $"Execution IR ASOF join lowering cannot rewrite right-side expression {expression.GetType().Name} for candidate row probing.");
        }

        return replaced;
    }

    private static ExecutionExpression ReplaceExecutionAliasCore(
        ExecutionExpression expression,
        string fromAlias,
        string toAlias)
    {
        return expression switch
        {
            ExecutionFieldRead fieldRead when string.Equals(fieldRead.Alias, fromAlias, StringComparison.OrdinalIgnoreCase) =>
                fieldRead with { Alias = toAlias },
            ExecutionFieldRead => expression,
            ExecutionLiteral => expression,
            ExecutionBinary binary => binary with
            {
                Left = ReplaceExecutionAliasCore(binary.Left, fromAlias, toAlias),
                Right = ReplaceExecutionAliasCore(binary.Right, fromAlias, toAlias)
            },
            ExecutionUnary unary => unary with
            {
                Operand = ReplaceExecutionAliasCore(unary.Operand, fromAlias, toAlias)
            },
            ExecutionStrictCast strictCast => strictCast with
            {
                Expression = ReplaceExecutionAliasCore(strictCast.Expression, fromAlias, toAlias)
            },
            ExecutionMethodCall method => method with
            {
                Arguments = method.Arguments
                    .Select(argument => ReplaceExecutionAliasCore(argument, fromAlias, toAlias))
                    .ToArray(),
                InjectedSource = method.InjectedSource == null
                    ? null
                    : ReplaceExecutionAliasCore(method.InjectedSource, fromAlias, toAlias)
            },
            ExecutionIsNullCheck isNull => isNull with
            {
                Expression = ReplaceExecutionAliasCore(isNull.Expression, fromAlias, toAlias)
            },
            ExecutionInCheck inCheck => inCheck with
            {
                Expression = ReplaceExecutionAliasCore(inCheck.Expression, fromAlias, toAlias),
                Values = inCheck.Values
                    .Select(value => ReplaceExecutionAliasCore(value, fromAlias, toAlias))
                    .ToArray()
            },
            ExecutionPatternMatch pattern => pattern with
            {
                Expression = ReplaceExecutionAliasCore(pattern.Expression, fromAlias, toAlias),
                Pattern = ReplaceExecutionAliasCore(pattern.Pattern, fromAlias, toAlias)
            },
            ExecutionBetween between => between with
            {
                Expression = ReplaceExecutionAliasCore(between.Expression, fromAlias, toAlias),
                Low = ReplaceExecutionAliasCore(between.Low, fromAlias, toAlias),
                High = ReplaceExecutionAliasCore(between.High, fromAlias, toAlias)
            },
            ExecutionCaseWhen caseWhen => caseWhen with
            {
                Branches = caseWhen.Branches
                    .Select(branch => new ExecutionCaseWhenBranch(
                        ReplaceExecutionAliasCore(branch.Condition, fromAlias, toAlias),
                        ReplaceExecutionAliasCore(branch.Result, fromAlias, toAlias)))
                    .ToArray(),
                ElseExpression = caseWhen.ElseExpression == null
                    ? null
                    : ReplaceExecutionAliasCore(caseWhen.ElseExpression, fromAlias, toAlias)
            },
            ExecutionCoalesce coalesce => coalesce with
            {
                Expressions = coalesce.Expressions
                    .Select(value => ReplaceExecutionAliasCore(value, fromAlias, toAlias))
                    .ToArray()
            },
            ExecutionCompositeKey compositeKey => compositeKey with
            {
                Parts = compositeKey.Parts
                    .Select(part => ReplaceExecutionAliasCore(part, fromAlias, toAlias))
                    .ToArray()
            },
            ExecutionValueTupleKey valueTupleKey => valueTupleKey with
            {
                Parts = valueTupleKey.Parts
                    .Select(part => ReplaceExecutionAliasCore(part, fromAlias, toAlias))
                    .ToArray()
            },
            ExecutionAggregateCall aggregateCall => aggregateCall with
            {
                Arguments = aggregateCall.Arguments
                    .Select(argument => ReplaceExecutionAliasCore(argument, fromAlias, toAlias))
                    .ToArray()
            },
            _ => expression
        };
    }
}
