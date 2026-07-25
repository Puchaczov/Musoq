using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionBlock RewriteFusedSiblingLoopBody(
        ExecutionBlock body,
        ExecutionVariable from,
        ExecutionVariable to)
    {
        if (string.Equals(from.Name, to.Name, StringComparison.Ordinal))
            return body;

        return new ExecutionBlock(body.Nodes.Select(node => RewriteFusedSiblingNode(node, from, to)).ToArray());
    }

    private static ExecutionNode RewriteFusedSiblingNode(
        ExecutionNode node,
        ExecutionVariable from,
        ExecutionVariable to)
    {
        return node switch
        {
            ExecutionIf branch => branch with
            {
                Condition = RewriteFusedSiblingExpression(branch.Condition, from, to),
                Body = RewriteFusedSiblingLoopBody(branch.Body, from, to)
            },
            ExecutionLet let => let with { Value = RewriteFusedSiblingExpression(let.Value, from, to) },
            ExecutionContinueIf continueIf => continueIf with
            {
                Condition = RewriteFusedSiblingExpression(continueIf.Condition, from, to)
            },
            ExecutionCreateGeneratedRow createRow => createRow with
            {
                Values = RewriteFusedSiblingValues(createRow.Values, from, to),
                Contexts = RewriteFusedSiblingExpressions(createRow.Contexts, from, to),
                ContextLayout = RewriteFusedSiblingContextLayout(createRow.ContextLayout, from, to)
            },
            ExecutionAppendRow appendRow => appendRow with
            {
                Values = RewriteFusedSiblingValues(appendRow.Values, from, to),
                Contexts = RewriteFusedSiblingExpressions(appendRow.Contexts, from, to),
                ContextLayout = RewriteFusedSiblingContextLayout(appendRow.ContextLayout, from, to)
            },
            ExecutionHashAdd hashAdd => hashAdd with
            {
                Key = RewriteFusedSiblingExpression(hashAdd.Key, from, to),
                PrecomputedKey = RewriteFusedSiblingVariable(hashAdd.PrecomputedKey, from, to)
            },
            ExecutionKeySetAdd keySetAdd => keySetAdd with
            {
                Key = RewriteFusedSiblingExpression(keySetAdd.Key, from, to),
                PrecomputedKey = RewriteFusedSiblingVariable(keySetAdd.PrecomputedKey, from, to)
            },
            ExecutionCteSidecarAppendRewriteCandidate candidate => candidate with
            {
                AppendRow = RewriteFusedSiblingAppendRow(candidate.AppendRow, from, to),
                Indexes = RewriteFusedSiblingSidecarIndexes(candidate.Indexes, from, to)
            },
            _ => node
        };
    }

    private static ExecutionAppendRow RewriteFusedSiblingAppendRow(
        ExecutionAppendRow appendRow,
        ExecutionVariable from,
        ExecutionVariable to)
    {
        return appendRow with
        {
            Values = RewriteFusedSiblingValues(appendRow.Values, from, to),
            Contexts = RewriteFusedSiblingExpressions(appendRow.Contexts, from, to),
            ContextLayout = RewriteFusedSiblingContextLayout(appendRow.ContextLayout, from, to)
        };
    }

    private static IReadOnlyList<ExecutionCteSidecarAppendIndexSpec> RewriteFusedSiblingSidecarIndexes(
        IReadOnlyList<ExecutionCteSidecarAppendIndexSpec> indexes,
        ExecutionVariable from,
        ExecutionVariable to)
    {
        return indexes
            .Select(index => index with
            {
                Key = RewriteFusedSiblingExpression(index.Key, from, to),
                PayloadValues = RewriteFusedSiblingValues(index.PayloadValues, from, to)
            })
            .ToArray();
    }

    private static IReadOnlyList<ExecutionRowValue> RewriteFusedSiblingValues(
        IReadOnlyList<ExecutionRowValue> values,
        ExecutionVariable from,
        ExecutionVariable to)
    {
        return values
            .Select(value => value with { Value = RewriteFusedSiblingExpression(value.Value, from, to) })
            .ToArray();
    }

    private static IReadOnlyList<ExecutionExpression> RewriteFusedSiblingExpressions(
        IReadOnlyList<ExecutionExpression> expressions,
        ExecutionVariable from,
        ExecutionVariable to)
    {
        return expressions
            .Select(expression => RewriteFusedSiblingExpression(expression, from, to))
            .ToArray();
    }

    private static ExecutionContextLayout? RewriteFusedSiblingContextLayout(
        ExecutionContextLayout? layout,
        ExecutionVariable from,
        ExecutionVariable to)
    {
        if (layout == null)
            return null;

        return layout with
        {
            Segments = layout.Segments
                .Select(segment => segment with
                {
                    Value = RewriteFusedSiblingExpression(segment.Value, from, to)
                })
                .ToArray()
        };
    }

    private static ExecutionExpression RewriteFusedSiblingExpression(
        ExecutionExpression expression,
        ExecutionVariable from,
        ExecutionVariable to)
    {
        return expression switch
        {
            ExecutionFieldRead fieldRead when string.Equals(fieldRead.Alias, from.Name, StringComparison.Ordinal) =>
                fieldRead with { Alias = to.Name },
            ExecutionVariableRead variableRead when HasFusedSiblingVariable(variableRead.Variable, from) =>
                new ExecutionVariableRead(to),
            ExecutionRowContextsRead contextsRead when HasFusedSiblingVariable(contextsRead.Row, from) =>
                new ExecutionRowContextsRead(to),
            ExecutionBinary binary => binary with
            {
                Left = RewriteFusedSiblingExpression(binary.Left, from, to),
                Right = RewriteFusedSiblingExpression(binary.Right, from, to)
            },
            ExecutionUnary unary => unary with
            {
                Operand = RewriteFusedSiblingExpression(unary.Operand, from, to)
            },
            ExecutionStrictCast strictCast => strictCast with
            {
                Expression = RewriteFusedSiblingExpression(strictCast.Expression, from, to)
            },
            ExecutionArrayAccess arrayAccess => arrayAccess with
            {
                Array = RewriteFusedSiblingExpression(arrayAccess.Array, from, to),
                Index = RewriteFusedSiblingExpression(arrayAccess.Index, from, to)
            },
            ExecutionIsNullCheck isNull => isNull with
            {
                Expression = RewriteFusedSiblingExpression(isNull.Expression, from, to)
            },
            ExecutionInCheck inCheck => inCheck with
            {
                Expression = RewriteFusedSiblingExpression(inCheck.Expression, from, to),
                Values = RewriteFusedSiblingExpressions(inCheck.Values, from, to)
            },
            ExecutionPatternMatch pattern => pattern with
            {
                Expression = RewriteFusedSiblingExpression(pattern.Expression, from, to),
                Pattern = RewriteFusedSiblingExpression(pattern.Pattern, from, to)
            },
            ExecutionBetween between => between with
            {
                Expression = RewriteFusedSiblingExpression(between.Expression, from, to),
                Low = RewriteFusedSiblingExpression(between.Low, from, to),
                High = RewriteFusedSiblingExpression(between.High, from, to)
            },
            ExecutionCaseWhen caseWhen => caseWhen with
            {
                Branches = caseWhen.Branches
                    .Select(branch => branch with
                    {
                        Condition = RewriteFusedSiblingExpression(branch.Condition, from, to),
                        Result = RewriteFusedSiblingExpression(branch.Result, from, to)
                    })
                    .ToArray(),
                ElseExpression = caseWhen.ElseExpression == null
                    ? null
                    : RewriteFusedSiblingExpression(caseWhen.ElseExpression, from, to)
            },
            ExecutionCoalesce coalesce => coalesce with
            {
                Expressions = RewriteFusedSiblingExpressions(coalesce.Expressions, from, to).ToArray()
            },
            ExecutionCompositeKey composite => composite with
            {
                Parts = RewriteFusedSiblingExpressions(composite.Parts, from, to)
            },
            ExecutionValueTupleKey tuple => tuple with
            {
                Parts = RewriteFusedSiblingExpressions(tuple.Parts, from, to)
            },
            ExecutionContextArray contextArray => contextArray with
            {
                Segments = RewriteFusedSiblingContextLayout(
                    new ExecutionContextLayout(contextArray.Segments),
                    from,
                    to)!.Segments
            },
            _ => expression
        };
    }

    private static ExecutionVariable? RewriteFusedSiblingVariable(
        ExecutionVariable? variable,
        ExecutionVariable from,
        ExecutionVariable to)
    {
        return variable != null && HasFusedSiblingVariable(variable, from) ? to : variable;
    }

    private static bool HasFusedSiblingVariable(ExecutionVariable variable, ExecutionVariable expected)
    {
        return string.Equals(variable.Name, expected.Name, StringComparison.Ordinal);
    }
}
