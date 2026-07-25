using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal static partial class RecursiveCteInvariantPlanner
{
    private static Dictionary<string, int> CountInvariantReferences(
        PhysicalNode member,
        IEnumerable<PhysicalRecursiveCteInvariantDefinition> definitions)
    {
        var counts = definitions.ToDictionary(
            static definition => definition.Name,
            static _ => 0,
            StringComparer.Ordinal);
        CountInvariantReferences(member, counts);
        return counts;
    }

    private static void CountInvariantReferences(PhysicalNode node, IDictionary<string, int> counts)
    {
        if (node is PhysicalCteRefNode cteRef && counts.TryGetValue(cteRef.CteName, out var count))
            counts[cteRef.CteName] = count + 1;

        foreach (var child in node.Children)
            CountInvariantReferences(child, counts);
    }

    private static PhysicalNode SelectHashIndexes(
        PhysicalNode node,
        IList<PhysicalRecursiveCteInvariantDefinition> definitions,
        IReadOnlyDictionary<string, int> referenceCounts)
    {
        if (node is PhysicalHashJoinNode { Kind: JoinKind.Inner } hashJoin)
        {
            var left = SelectHashIndexes(hashJoin.Left, definitions, referenceCounts);
            var right = SelectHashIndexes(hashJoin.Right, definitions, referenceCounts);
            var rewritten = hashJoin with { Left = left, Right = right };
            var definitionIndex = FindDirectInvariantDefinition(left, right, definitions, referenceCounts);
            if (definitionIndex >= 0)
            {
                var definition = definitions[definitionIndex];
                var buildKeys = rewritten.BuildKeys;
                var probeKeys = rewritten.ProbeKeys;
                if (!ExpressionsReferenceAliases(buildKeys, definition.SourceAliases) &&
                    ExpressionsReferenceAliases(probeKeys, definition.SourceAliases))
                {
                    (buildKeys, probeKeys) = (probeKeys, buildKeys);
                    rewritten = rewritten with { BuildKeys = buildKeys, ProbeKeys = probeKeys };
                }

                if (ExpressionsReferenceAliases(buildKeys, definition.SourceAliases))
                {
                    definitions[definitionIndex] = definition with
                    {
                        StorageKind = definition.StorageKind == PhysicalRecursiveCteInvariantStorageKind.ExistingRows
                            ? PhysicalRecursiveCteInvariantStorageKind.ExistingHashIndex
                            : PhysicalRecursiveCteInvariantStorageKind.HashIndex,
                        HashKeys = buildKeys,
                        HashProbeKeys = probeKeys
                    };
                }
            }

            return rewritten;
        }

        return PhysicalPlanRewriter.RewriteChildren(
            node,
            child => SelectHashIndexes(child, definitions, referenceCounts));
    }

    private static int FindDirectInvariantDefinition(
        PhysicalNode left,
        PhysicalNode right,
        IList<PhysicalRecursiveCteInvariantDefinition> definitions,
        IReadOnlyDictionary<string, int> referenceCounts)
    {
        for (var index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            if (referenceCounts[definition.Name] != 1)
                continue;

            if (left is PhysicalCteRefNode leftRef && leftRef.CteName == definition.Name ||
                right is PhysicalCteRefNode rightRef && rightRef.CteName == definition.Name)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool ExpressionsReferenceAliases(
        IEnumerable<IrExpression> expressions,
        IReadOnlyCollection<string> aliases) =>
        expressions.SelectMany(AliasRefExtractor.Extract)
            .Any(alias => aliases.Contains(alias, StringComparer.OrdinalIgnoreCase));

    private static IEnumerable<IrExpression> EnumerateExpressionsRecursively(PhysicalNode node)
    {
        foreach (var expression in EnumerateExpressions(node))
            yield return expression;
        foreach (var child in node.Children)
        foreach (var expression in EnumerateExpressionsRecursively(child))
            yield return expression;
    }

    private static IEnumerable<IrExpression> EnumerateExpressions(PhysicalNode node)
    {
        return node switch
        {
            PhysicalProjectNode project => project.Fields.Select(static field => field.Expression),
            PhysicalFilterNode filter => [filter.Predicate],
            PhysicalHashJoinNode join => join.BuildKeys.Concat(join.ProbeKeys).Concat(Optional(join.Residual)),
            PhysicalNestedLoopJoinNode join => [join.OnPredicate],
            PhysicalSortMergeJoinNode join => [join.LeftKey, join.RightKey, join.Residual],
            PhysicalSchemaScanNode scan => scan.Arguments.Concat(scan.PushedPredicates),
            PhysicalInterpretSourceNode interpret => interpret.Arguments,
            PhysicalAccessMethodSourceNode access => [access.MethodCallExpression],
            PhysicalValuesScanNode values => values.Rows.SelectMany(static row => row.Fields)
                .Select(static field => field.Value),
            _ => []
        };
    }

    private static IEnumerable<IrExpression> Optional(IrExpression? expression)
    {
        if (expression != null)
            yield return expression;
    }

    private static string NormalizeColumnName(string name, string alias)
    {
        var prefix = $"{alias}.";
        return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? name[prefix.Length..] : name;
    }

    private static string Sanitize(string name) =>
        new(name.Select(static character => char.IsLetterOrDigit(character) ? character : '_').ToArray());

    private static string CreateInvariantName(string recursiveName, int ordinal) =>
        $"__recursive_{Sanitize(recursiveName)}_invariant_{ordinal.ToString(CultureInfo.InvariantCulture)}";

    private sealed record InvariantSource(string Alias, OutputSchema Schema);
}
