using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed partial class SubqueryCorrelationAnalyzer
{
    private sealed record QueryScopeInfo(IReadOnlySet<string> Aliases);

    private sealed class SubqueryScopeBuilder(
        Node node,
        IReadOnlySet<string> outerAliases,
        bool isInsideCteDefinition)
    {
        private readonly HashSet<string> _localAliases = CreateAliasSet();
        private readonly HashSet<string> _correlatedAliases = CreateAliasSet();
        private readonly HashSet<string> _illegalOuterConsumingCteAliases = CreateAliasSet();

        public HashSet<string> OuterAliases { get; } = CopyAliasSet(outerAliases);

        public void AddLocalAliases(IEnumerable<string> aliases)
        {
            AddAliases(_localAliases, aliases);
        }

        public void AddCorrelatedAlias(string alias)
        {
            AddAlias(_correlatedAliases, alias);
        }

        public void AddIllegalOuterConsumingCteAlias(string alias)
        {
            AddAlias(_illegalOuterConsumingCteAliases, alias);
        }

        public SubqueryCorrelationInfo Build()
        {
            var localAliases = CopyAliasSet(_localAliases);
            var correlatedAliases = CopyAliasSet(_correlatedAliases);

            return new SubqueryCorrelationInfo(
                node,
                localAliases,
                OuterAliases,
                correlatedAliases,
                CopyAliasSet(_illegalOuterConsumingCteAliases),
                SubqueryCorrelationFactBuilder.Build(
                    node,
                    localAliases,
                    OuterAliases,
                    correlatedAliases),
                isInsideCteDefinition);
        }
    }
}
