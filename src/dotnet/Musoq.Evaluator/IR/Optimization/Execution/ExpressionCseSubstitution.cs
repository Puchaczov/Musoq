using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal static partial class ExpressionCseSubstitution
{
    public static ExecutionNode ReplaceSupportedNodeExpressions(
        ExecutionNode node,
        IReadOnlyDictionary<string, ExecutionVariable> variablesBySignature)
    {
        return node switch
        {
            ExecutionAppendRow appendRow => appendRow with
            {
                Values = appendRow.Values
                    .Select(value => value with { Value = Replace(value.Value, variablesBySignature) })
                    .ToArray()
            },
            ExecutionAppendRecord appendRecord => appendRecord with
            {
                Values = appendRecord.Values
                    .Select(value => value with { Value = Replace(value.Value, variablesBySignature) })
                    .ToArray()
            },
            ExecutionHashAdd hashAdd => hashAdd with
            {
                Key = Replace(hashAdd.Key, variablesBySignature)
            },
            ExecutionHashProbe hashProbe => hashProbe with
            {
                Key = Replace(hashProbe.Key, variablesBySignature)
            },
            ExecutionKeySetAdd keySetAdd => keySetAdd with
            {
                Key = Replace(keySetAdd.Key, variablesBySignature)
            },
            ExecutionKeySetProbe keySetProbe => keySetProbe with
            {
                Key = Replace(keySetProbe.Key, variablesBySignature)
            },
            ExecutionComputeRankingWindow ranking => ranking with
            {
                PartitionKey = ReplaceOptional(ranking.PartitionKey, variablesBySignature),
                OrderKeys = Replace(ranking.OrderKeys, variablesBySignature)
            },
            ExecutionComputeOffsetWindow offset => offset with
            {
                PartitionKey = ReplaceOptional(offset.PartitionKey, variablesBySignature),
                OrderKeys = Replace(offset.OrderKeys, variablesBySignature),
                Value = Replace(offset.Value, variablesBySignature),
                Offset = Replace(offset.Offset, variablesBySignature),
                DefaultValue = Replace(offset.DefaultValue, variablesBySignature)
            },
            ExecutionComputePluginWindow plugin => plugin with
            {
                PartitionKey = ReplaceOptional(plugin.PartitionKey, variablesBySignature),
                OrderKeys = Replace(plugin.OrderKeys, variablesBySignature),
                Value = Replace(plugin.Value, variablesBySignature),
                Arguments = plugin.Arguments
                    .Select(argument => Replace(argument, variablesBySignature))
                    .ToArray()
            },
            ExecutionWindowAggregateKernel kernel => kernel with
            {
                PartitionKey = ReplaceOptional(kernel.PartitionKey, variablesBySignature),
                OrderKeys = Replace(kernel.OrderKeys, variablesBySignature),
                Value = Replace(kernel.Value, variablesBySignature)
            },
            _ => node
        };
    }

    public static ExecutionBlock ReplaceAggregateBlockExpressions(
        ExecutionBlock block,
        IReadOnlyDictionary<string, ExecutionVariable> variablesBySignature)
    {
        return new ExecutionBlock(
            block.Nodes
                .Select(node => ReplaceAggregateNodeExpressions(node, variablesBySignature))
                .ToArray());
    }

    public static ExecutionExpression Replace(
        ExecutionExpression expression,
        IReadOnlyDictionary<string, ExecutionVariable> variablesBySignature)
    {
        if (ExecutionExpressionCseFacts.IsWorthHoistingExpression(expression) &&
            variablesBySignature.TryGetValue(ExecutionExpressionFingerprint.ForHoist(expression), out var variable))
        {
            return new ExecutionVariableRead(variable);
        }

        return expression switch
        {
            ExecutionBinary binary => binary with
            {
                Left = Replace(binary.Left, variablesBySignature),
                Right = Replace(binary.Right, variablesBySignature)
            },
            ExecutionUnary unary => unary with { Operand = Replace(unary.Operand, variablesBySignature) },
            ExecutionMethodCall methodCall => methodCall with
            {
                Arguments = methodCall.Arguments
                    .Select(argument => Replace(argument, variablesBySignature))
                    .ToArray(),
                InjectedSource = methodCall.InjectedSource == null
                    ? null
                    : Replace(methodCall.InjectedSource, variablesBySignature)
            },
            ExecutionMethodTargetReuseCandidate candidate => candidate with
            {
                MethodCall = (ExecutionMethodCall)Replace(candidate.MethodCall, variablesBySignature)
            },
            ExecutionStrictCast strictCast => strictCast with
            {
                Expression = Replace(strictCast.Expression, variablesBySignature)
            },
            ExecutionArrayAccess arrayAccess => arrayAccess with
            {
                Index = Replace(arrayAccess.Index, variablesBySignature)
            },
            ExecutionIsNullCheck isNull => isNull with
            {
                Expression = Replace(isNull.Expression, variablesBySignature)
            },
            ExecutionInCheck inCheck => inCheck with
            {
                Expression = Replace(inCheck.Expression, variablesBySignature),
                Values = inCheck.Values
                    .Select(value => Replace(value, variablesBySignature))
                    .ToArray()
            },
            ExecutionCollectionInCheck collectionInCheck => ReplaceCollectionInCheck(collectionInCheck, variablesBySignature),
            ExecutionPatternMatch patternMatch => patternMatch with
            {
                Expression = Replace(patternMatch.Expression, variablesBySignature),
                Pattern = Replace(patternMatch.Pattern, variablesBySignature)
            },
            ExecutionBetween between => between with
            {
                Expression = Replace(between.Expression, variablesBySignature),
                Low = Replace(between.Low, variablesBySignature),
                High = Replace(between.High, variablesBySignature)
            },
            ExecutionCaseWhen caseWhen => caseWhen with
            {
                Branches = caseWhen.Branches
                    .Select(branch => new ExecutionCaseWhenBranch(
                        Replace(branch.Condition, variablesBySignature),
                        Replace(branch.Result, variablesBySignature)))
                    .ToArray(),
                ElseExpression = caseWhen.ElseExpression == null
                    ? null
                    : Replace(caseWhen.ElseExpression, variablesBySignature)
            },
            ExecutionCoalesce coalesce => coalesce with
            {
                Expressions = coalesce.Expressions
                    .Select(value => Replace(value, variablesBySignature))
                    .ToArray()
            },
            ExecutionCompositeKey compositeKey => compositeKey with
            {
                Parts = compositeKey.Parts
                    .Select(part => Replace(part, variablesBySignature))
                    .ToArray()
            },
            ExecutionValueTupleKey valueTupleKey => valueTupleKey with
            {
                Parts = valueTupleKey.Parts
                    .Select(part => Replace(part, variablesBySignature))
                    .ToArray()
            },
            ExecutionAggregateCall aggregateCall => aggregateCall with
            {
                Arguments = aggregateCall.Arguments
                    .Select(argument => Replace(argument, variablesBySignature))
                    .ToArray()
            },
            _ => expression
        };
    }

    private static ExecutionExpression? ReplaceOptional(
        ExecutionExpression? expression,
        IReadOnlyDictionary<string, ExecutionVariable> variablesBySignature)
    {
        return expression == null
            ? null
            : Replace(expression, variablesBySignature);
    }

    private static IReadOnlyList<ExecutionWindowOrderKey> Replace(
        IReadOnlyList<ExecutionWindowOrderKey> orderKeys,
        IReadOnlyDictionary<string, ExecutionVariable> variablesBySignature)
    {
        return orderKeys
            .Select(key => key with { Expression = Replace(key.Expression, variablesBySignature) })
            .ToArray();
    }

    private static ExecutionNode ReplaceAggregateNodeExpressions(
        ExecutionNode node,
        IReadOnlyDictionary<string, ExecutionVariable> variablesBySignature)
    {
        return node switch
        {
            ExecutionGetOrAddSingleKeyAggregateGroup getOrAdd => getOrAdd with
            {
                Key = Replace(getOrAdd.Key, variablesBySignature)
            },
            ExecutionGetOrAddValueTupleAggregateGroup getOrAdd => getOrAdd with
            {
                Keys = getOrAdd.Keys
                    .Select(key => Replace(key, variablesBySignature))
                    .ToArray()
            },
            ExecutionAggregateSet aggregateSet => aggregateSet with
            {
                Arguments = aggregateSet.Arguments
                    .Select(argument => Replace(argument, variablesBySignature))
                    .ToArray(),
                AccumulatorInput = aggregateSet.AccumulatorInput == null
                    ? null
                    : Replace(aggregateSet.AccumulatorInput, variablesBySignature)
            },
            ExecutionAggregateCapturedValueSet capturedValueSet => capturedValueSet with
            {
                Value = Replace(capturedValueSet.Value, variablesBySignature)
            },
            ExecutionLet let => let with
            {
                Value = Replace(let.Value, variablesBySignature)
            },
            ExecutionIf branch => branch with
            {
                Condition = Replace(branch.Condition, variablesBySignature),
                Body = ReplaceAggregateBlockExpressions(branch.Body, variablesBySignature)
            },
            ExecutionAppendRow appendRow => appendRow with
            {
                Values = appendRow.Values
                    .Select(value => value with { Value = Replace(value.Value, variablesBySignature) })
                    .ToArray(),
                Contexts = appendRow.Contexts
                    .Select(context => Replace(context, variablesBySignature))
                    .ToArray(),
                ContextLayout = Replace(appendRow.ContextLayout, variablesBySignature)
            },
            ExecutionAppendRecord appendRecord => appendRecord with
            {
                Values = appendRecord.Values
                    .Select(value => value with { Value = Replace(value.Value, variablesBySignature) })
                    .ToArray()
            },
            _ => node
        };
    }

    private static ExecutionContextLayout? Replace(
        ExecutionContextLayout? contextLayout,
        IReadOnlyDictionary<string, ExecutionVariable> variablesBySignature)
    {
        if (contextLayout == null)
            return null;

        return contextLayout with
        {
            Segments = contextLayout.Segments
                .Select(segment => segment with
                {
                    Value = Replace(segment.Value, variablesBySignature)
                })
                .ToArray()
        };
    }
}

