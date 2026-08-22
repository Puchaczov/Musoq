using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class ApplyPredicateMovementPlanner
{
    private sealed partial class PlanningState
    {
        private IrExpression RewriteCteAliases(IrExpression predicate)
        {
            var boundaryAliases = _allBoundaries
                .SelectMany(static boundary => boundary.LeftAliases.Concat(boundary.RightAliases))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var rewriter = new ApplyPredicateAliasRewriter(column =>
            {
                var separator = column.ColumnName.IndexOf('.', StringComparison.Ordinal);
                if (separator <= 0 || separator >= column.ColumnName.Length - 1)
                    return null;

                var sourceAlias = column.ColumnName[..separator];
                var isKnownCteAlias = _sourceAliasesByCteAlias.TryGetValue(
                    column.Alias,
                    out var sourceAliases);
                return ((isKnownCteAlias && sourceAliases is not null && sourceAliases.Contains(sourceAlias)) ||
                        (!isKnownCteAlias &&
                         !boundaryAliases.Contains(column.Alias) &&
                         boundaryAliases.Contains(sourceAlias)))
                    ? column with
                    {
                        Alias = sourceAlias,
                        ColumnName = column.ColumnName[(separator + 1)..]
                    }
                    : null;
            });

            return rewriter.Rewrite(predicate);
        }

        private static IrExpression RewriteForBoundary(
            IrExpression predicate,
            ApplyBoundary boundary)
        {
            if (boundary.Apply.Left is not CteRefNode cteRef ||
                string.IsNullOrWhiteSpace(cteRef.Alias))
            {
                return predicate;
            }

            var sourceAliases = boundary.LeftAliases
                .Where(alias => !string.Equals(alias, cteRef.Alias, StringComparison.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (sourceAliases.Count == 0)
                return predicate;

            var rewriter = new ApplyPredicateAliasRewriter(column =>
            {
                if (!sourceAliases.Contains(column.Alias))
                    return null;

                return column with
                {
                    Alias = cteRef.Alias,
                    ColumnName = string.Concat(column.Alias, ".", column.ColumnName)
                };
            });

            return rewriter.Rewrite(predicate);
        }

        private static IReadOnlyDictionary<string, IReadOnlySet<string>> CreateSourceAliasesByCteAlias(
            LogicalNode logicalPlan)
        {
            var definitions = new Dictionary<string, LogicalNode>(StringComparer.OrdinalIgnoreCase);
            var references = new List<CteRefNode>();
            CollectCteDefinitionsAndReferences(logicalPlan, definitions, references);

            var aliasesByCteAlias = new Dictionary<string, IReadOnlySet<string>>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var reference in references)
            {
                var sourceAliases = definitions.TryGetValue(reference.CteName, out var definition)
                    ? CollectProducedAliases(definition)
                    : CollectOutputAliases(reference.OutputSchema);
                if (sourceAliases.Count == 0)
                    continue;

                aliasesByCteAlias[reference.CteName] = sourceAliases;
                if (!string.IsNullOrWhiteSpace(reference.Alias))
                    aliasesByCteAlias[reference.Alias] = sourceAliases;
            }

            return aliasesByCteAlias;
        }

        private static void CollectCteDefinitionsAndReferences(
            LogicalNode node,
            IDictionary<string, LogicalNode> definitions,
            ICollection<CteRefNode> references)
        {
            if (node is CteNode cte)
            {
                foreach (var definition in cte.Definitions)
                    definitions[definition.Name] = definition.Plan;
            }

            if (node is CteRefNode reference)
                references.Add(reference);

            foreach (var child in node.Children)
                CollectCteDefinitionsAndReferences(child, definitions, references);
        }

        private static HashSet<string> CollectOutputAliases(OutputSchema outputSchema)
        {
            var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in outputSchema.Columns)
            {
                var separator = column.Name.IndexOf('.', StringComparison.Ordinal);
                if (separator > 0)
                    aliases.Add(column.Name[..separator]);
            }

            return aliases;
        }
    }
}
