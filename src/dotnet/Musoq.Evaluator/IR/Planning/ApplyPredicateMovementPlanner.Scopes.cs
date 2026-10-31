using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class ApplyPredicateMovementPlanner
{
    private sealed partial class PlanningState
    {
        private void CollectScopes(LogicalNode node, QueryScope scope)
        {
            _scopes[node] = scope;
            scope.Nodes.Add(node);
            switch (node)
            {
                case CteNode cte:
                    foreach (var definition in cte.Definitions)
                        CollectScopes(definition.Plan, new QueryScope());
                    CollectScopes(cte.Query, new QueryScope());
                    return;
                case SetOperationNode or RecursiveCteNode:
                    foreach (var branch in node.Children)
                        CollectScopes(branch, new QueryScope());
                    return;
            }

            foreach (var child in node.Children)
                CollectScopes(child, scope);
        }

        private void InitializeScopes(
            LogicalNode logicalPlan,
            IReadOnlyDictionary<string, SourcePlanProperties> sources,
            IReadOnlyDictionary<string, SourcePlanResult>? sourcePlanResults,
            IReadOnlyDictionary<string, SourcePredicatePlan>? sourcePredicatePlans)
        {
            var definitions = new Dictionary<string, LogicalNode>(StringComparer.OrdinalIgnoreCase);
            CollectCteDefinitionsAndReferences(logicalPlan, definitions, new List<CteRefNode>());
            var sourcesByAlias = sources.Values.ToLookup(static source => source.Alias, StringComparer.OrdinalIgnoreCase);
            foreach (var scope in _scopes.Values.Distinct())
            {
                var sourceIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var node in scope.Nodes)
                {
                    if (node is not CteRefNode && GetProducedAlias(node) is { Length: > 0 } alias)
                        IncrementAlias(scope.ProducedAliasCounts, alias);
                    if (node is not SchemaScanNode scan)
                        continue;
                    if (scan.SourceContextId is { } sourceId)
                        sourceIds.Add(sourceId);
                    else
                        sourceIds.UnionWith(sourcesByAlias[scan.Alias].Select(static source => source.SourceContextId));
                }

                foreach (var sourceId in sourceIds)
                    if (sources.TryGetValue(sourceId, out var source))
                        IncrementAlias(scope.SourceAliasCounts, source.Alias);
                scope.SourceAliasesByCteAlias = CreateSourceAliasesByCteAlias(definitions, scope.Nodes.OfType<CteRefNode>());
                scope.SourceAcceptedPredicateTexts = CreateSourceAcceptedPredicateTexts(sourcePlanResults, sourcePredicatePlans, sourceIds);
            }
        }

        private static void IncrementAlias(Dictionary<string, int> counts, string alias) =>
            counts[alias] = counts.TryGetValue(alias, out var count) ? count + 1 : 1;
    }

    private sealed class QueryScope
    {
        public List<LogicalNode> Nodes { get; } = [];
        public Dictionary<string, int> ProducedAliasCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> SourceAliasCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<ApplyBoundary> Boundaries { get; } = [];
        public HashSet<string> BoundaryAliases { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> SourceAcceptedPredicateTexts { get; set; } = new(StringComparer.Ordinal);
        public IReadOnlyDictionary<string, IReadOnlySet<string>> SourceAliasesByCteAlias { get; set; } =
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
    }
}
