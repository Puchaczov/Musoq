using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed class LogicalSourceAliasAnalysisPass : ILogicalNormalizationPass
{
    public string Name => "LogicalSourceAliasAnalysis";

    public OptimizationResult<LogicalNode> Optimize(LogicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var collector = new LogicalSourceAliasFactCollector();
        var facts = collector.Collect(plan);
        context.AnalysisFacts.Set(LogicalAnalysisFactKeys.SourceAndAliasFacts, facts);

        return OptimizationResult<LogicalNode>.NoChange(
            plan,
            $"Derived source context facts for {facts.SourceContexts.Length} schema scan(s) and alias-scope facts for {facts.AliasScopes.Length} logical scope(s).");
    }

    private sealed class LogicalSourceAliasFactCollector
    {
        private readonly List<LogicalSourceContextFact> _sourceContexts = [];
        private readonly List<LogicalAliasScopeFact> _aliasScopes = [];

        public LogicalSourceAliasFacts Collect(LogicalNode plan)
        {
            VisitScope(plan, "query");
            return new LogicalSourceAliasFacts(
                _sourceContexts.ToArray(),
                _aliasScopes.ToArray());
        }

        private void VisitScope(LogicalNode node, string scopePath)
        {
            var aliases = new List<string>();
            CollectScopeAliases(node, scopePath, aliases);
            AddAliasScope(scopePath, aliases);

            if (node is CteNode cte)
            {
                foreach (var definition in cte.Definitions)
                    VisitScope(definition.Plan, $"{scopePath}/cte:{definition.Name}");

                VisitScope(cte.Query, $"{scopePath}/cte-query");
            }
        }

        private void CollectScopeAliases(LogicalNode node, string scopePath, ICollection<string> aliases)
        {
            switch (node)
            {
                case AccessMethodSourceNode accessMethod:
                    AddAlias(aliases, accessMethod.Alias);
                    break;

                case CteNode:
                    return;

                case CteRefNode cteRef:
                    AddAlias(aliases, cteRef.Alias);
                    break;

                case InterpretSourceNode interpret:
                    AddAlias(aliases, interpret.Alias);
                    break;

                case PropertySourceNode property:
                    AddAlias(aliases, property.Alias);
                    break;

                case SchemaScanNode scan:
                    AddAlias(aliases, scan.Alias);
                    _sourceContexts.Add(new LogicalSourceContextFact(
                        scopePath,
                        scan.Alias,
                        scan.SourceContextId,
                        nameof(SchemaScanNode)));
                    break;

                case ValuesScanNode values:
                    AddAlias(aliases, values.Alias);
                    break;
            }

            foreach (var child in node.Children)
                CollectScopeAliases(child, scopePath, aliases);
        }

        private void AddAliasScope(string scopePath, IReadOnlyCollection<string> aliases)
        {
            var duplicateAliases = aliases
                .GroupBy(static alias => alias, StringComparer.OrdinalIgnoreCase)
                .Where(static group => group.Count() > 1)
                .Select(static group => group.Key)
                .OrderBy(static alias => alias, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _aliasScopes.Add(new LogicalAliasScopeFact(
                scopePath,
                aliases
                    .OrderBy(static alias => alias, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                duplicateAliases));
        }

        private static void AddAlias(ICollection<string> aliases, string alias)
        {
            if (!string.IsNullOrWhiteSpace(alias))
                aliases.Add(alias);
        }
    }
}

