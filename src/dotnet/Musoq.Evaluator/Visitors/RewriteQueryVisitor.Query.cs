using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    public void Visit(QueryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var context = CreateQueryRewriteContext(node);

        Scope[MetaAttributes.MethodName] = $"ComputeTable_{context.From.Alias}_{_queryIndex++}";

        LoadRefreshMethods(context);

        var directApplyCandidate = new DirectApplyChainCandidate(
            context.Select,
            context.From,
            context.OrderBy,
            context.GroupBy,
            context.Window,
            context.Qualify);
        context.PreserveDirectApplyChain = ShouldPreserveDirectApplyChain(directApplyCandidate);

        SplitJoinChainIfNeeded(node, context);

        if (ShouldSplitGroupedQuery(context))
            PushGroupedQuerySplit(context);
        else
            PushResultQuerySplit(context);

        Scope.ScopeSymbolTable.AddSymbol(MetaAttributes.AllQueryContexts, context.AliasesPositionsSymbol);

        _joinedTables.Clear();
    }

    private QueryRewriteContext CreateQueryRewriteContext(QueryNode node)
    {
        var orderBy = node.OrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var qualify = node.Qualify != null ? Nodes.Pop() as QualifyNode : null;
        var window = node.Window != null ? Nodes.Pop() as WindowNode : null;
        var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;

        var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
        var take = node.Take != null ? Nodes.Pop() as TakeNode : null;

        var select = (SelectNode)Nodes.Pop();
        var where = node.Where != null ? Nodes.Pop() as WhereNode : null;
        var from = (Parser.ExpressionFromNode)Nodes.Pop();

        return new QueryRewriteContext(select, from)
        {
            OrderBy = orderBy,
            Qualify = qualify,
            Window = window,
            GroupBy = groupBy,
            Skip = skip,
            Take = take,
            Where = where,
            ScoreWhere = where,
            ScoreOrderBy = orderBy
        };
    }

    private void LoadRefreshMethods(QueryRewriteContext context)
    {
        if (!Scope.ScopeSymbolTable.SymbolIsOfType<RefreshMethodsSymbol>(
                context.From.Alias.ToRefreshMethodsSymbolName()))
        {
            return;
        }

        context.UsedRefreshMethods = Scope.ScopeSymbolTable
            .GetSymbol<RefreshMethodsSymbol>(context.From.Alias.ToRefreshMethodsSymbolName())
            .RefreshMethods;
    }

    private static bool ShouldSplitGroupedQuery(QueryRewriteContext context)
    {
        return context.GroupBy != null &&
               (!context.PreserveDirectApplyChain || context.Window != null || context.Qualify != null);
    }
}
