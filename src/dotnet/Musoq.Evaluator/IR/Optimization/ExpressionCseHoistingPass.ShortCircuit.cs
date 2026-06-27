using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed partial class ExpressionCseHoistingPass
{
    private sealed partial class ExpressionCseRewriter
    {
        private static ExecutionIf TrySplitShortCircuitConditionAroundBodyLet(ExecutionIf branch)
        {
            if (branch.Condition is not ExecutionBinary { Kind: BinaryOpKind.And } ||
                branch.Body.Nodes.Count < 2 ||
                branch.Body.Nodes[0] is not ExecutionLet
                {
                    CacheMode: ExecutionLetCacheMode.SuppressMethodCache,
                    Value: { } letValue
                } let ||
                !ExecutionExpressionCseFacts.IsWorthHoistingExpression(letValue))
            {
                return branch;
            }

            var signature = ExecutionExpressionFingerprint.ForHoist(letValue);
            var terms = FlattenConjunction(branch.Condition).ToArray();
            if (terms.Length < 2)
                return branch;

            var matchingTermIndex = Array.FindIndex(terms, term => ContainsHoistSignature(term, signature));
            if (matchingTermIndex <= 0)
                return branch;

            var remainingTerms = terms
                .Skip(matchingTermIndex)
                .Select(term => ExpressionCseSubstitution.Replace(
                    term,
                    new Dictionary<string, ExecutionVariable>(StringComparer.Ordinal)
                    {
                        [signature] = let.Variable
                    }))
                .ToArray();
            var innerBody = branch.Body with { Nodes = branch.Body.Nodes.Skip(1).ToArray() };

            return branch with
            {
                Condition = RebuildConjunction(terms.Take(matchingTermIndex)),
                Body = new ExecutionBlock(
                [
                    let,
                    new ExecutionIf(RebuildConjunction(remainingTerms), innerBody)
                ])
            };
        }

        private static IEnumerable<ExecutionExpression> FlattenConjunction(ExecutionExpression expression)
        {
            if (expression is not ExecutionBinary { Kind: BinaryOpKind.And } binary)
            {
                yield return expression;
                yield break;
            }

            foreach (var term in FlattenConjunction(binary.Left))
                yield return term;
            foreach (var term in FlattenConjunction(binary.Right))
                yield return term;
        }

        private static ExecutionExpression RebuildConjunction(IEnumerable<ExecutionExpression> terms)
        {
            using var enumerator = terms.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new InvalidOperationException("A conjunction must contain at least one term.");

            var expression = enumerator.Current;
            while (enumerator.MoveNext())
                expression = new ExecutionBinary(BinaryOpKind.And, expression, enumerator.Current, typeof(bool));

            return expression;
        }

        private static bool ContainsHoistSignature(ExecutionExpression expression, string signature)
        {
            return ExecutionIrAnalysis.FlattenExpressions(expression)
                .Any(current =>
                    ExecutionExpressionCseFacts.IsWorthHoistingExpression(current) &&
                    string.Equals(ExecutionExpressionFingerprint.ForHoist(current), signature, StringComparison.Ordinal));
        }
    }
}
