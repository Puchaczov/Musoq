using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed partial class SubqueryCorrelationAnalyzer
{
    private static HashSet<string> CollectAliasesFromNode(Node node)
    {
        var aliases = CreateAliasSet();

        switch (node)
        {
            case QueryNode query:
                AddAliases(aliases, CollectAliases(query.From));
                break;

            case SingleSetNode singleSet:
                AddAliases(aliases, CollectAliasesFromNode(singleSet.Query));
                break;

            case SetOperatorNode setOperator:
                AddAliases(aliases, CollectAliasesFromNode(setOperator.Left));
                AddAliases(aliases, CollectAliasesFromNode(setOperator.Right));
                break;

            case CteExpressionNode cte:
                AddAliases(aliases, CollectAliasesFromNode(cte.OuterExpression));
                break;

            case CteInnerExpressionNode cteInner:
                AddAliases(aliases, CollectAliasesFromNode(cteInner.Value));
                break;
        }

        return aliases;
    }
    private static HashSet<string> CollectAliases(FromNode node)
    {
        var aliases = CreateAliasSet();
        CollectAliases(node, aliases);
        return aliases;
    }

    private static void CollectAliases(FromNode node, HashSet<string> aliases)
    {
        switch (node)
        {
            case null:
                return;

            case ExpressionFromNode expressionFrom:
                CollectAliases(expressionFrom.Expression, aliases);
                return;

            case JoinNode join:
                CollectAliases(join.Join, aliases);
                return;

            case ApplyNode apply:
                CollectAliases(apply.Apply, aliases);
                return;

            case JoinFromNode joinFrom:
                CollectAliases(joinFrom.Source, aliases);
                CollectAliases(joinFrom.With, aliases);
                return;

            case ApplyFromNode applyFrom:
                CollectAliases(applyFrom.Source, aliases);
                CollectAliases(applyFrom.With, aliases);
                return;

            case JoinSourcesTableFromNode joinSources:
                CollectAliases(joinSources.First, aliases);
                CollectAliases(joinSources.Second, aliases);
                return;

            case ApplySourcesTableFromNode applySources:
                CollectAliases(applySources.First, aliases);
                CollectAliases(applySources.Second, aliases);
                return;

            case JoinInMemoryWithSourceTableFromNode joinInMemory:
                AddAlias(aliases, joinInMemory.InMemoryTableAlias);
                CollectAliases(joinInMemory.SourceTable, aliases);
                return;

            case ApplyInMemoryWithSourceTableFromNode applyInMemory:
                AddAlias(aliases, applyInMemory.InMemoryTableAlias);
                CollectAliases(applyInMemory.SourceTable, aliases);
                return;

            case InMemoryTableFromNode inMemory:
                AddAlias(aliases, string.IsNullOrWhiteSpace(inMemory.Alias) ? inMemory.VariableName : inMemory.Alias);
                return;

            default:
                AddAlias(aliases, node.Alias);
                return;
        }
    }

    private static void AddAliases(HashSet<string> target, IEnumerable<string> aliases)
    {
        foreach (var alias in aliases)
            AddAlias(target, alias);
    }

    private static void AddAlias(HashSet<string> aliases, string alias)
    {
        if (!string.IsNullOrWhiteSpace(alias))
            aliases.Add(alias);
    }

    private static HashSet<string> CreateAliasSet()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> CreateAliasSet(IEnumerable<string> values)
    {
        return new HashSet<string>(values.Where(value => !string.IsNullOrWhiteSpace(value)),
            StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> CopyAliasSet(IEnumerable<string> values)
    {
        return CreateAliasSet(values);
    }
}
