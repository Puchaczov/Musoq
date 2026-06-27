using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed partial class SubqueryCorrelationAnalyzer : RawTraverseVisitor<SubqueryAliasReferenceVisitor>
{
    private readonly List<SubqueryCorrelationInfo> _subqueries = [];
    private readonly Stack<QueryScopeInfo> _queryScopes = new();
    private readonly Stack<SubqueryScopeBuilder> _subqueryScopes = new();
    private readonly Stack<IReadOnlySet<string>> _forbiddenAliasScopes = new();
    private readonly HashSet<string> _illegalOuterConsumingCteAliases = CreateAliasSet();
    private readonly HashSet<QueryNode> _existsProjectionIgnoredQueries = [];

    private int _cteDefinitionDepth;

    private SubqueryCorrelationAnalyzer(SubqueryAliasReferenceVisitor visitor)
        : base(visitor)
    {
        visitor.Bind(this);
    }

    public static SubqueryCorrelationAnalysis Analyze(Node root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var referenceVisitor = new SubqueryAliasReferenceVisitor();
        var analyzer = new SubqueryCorrelationAnalyzer(referenceVisitor);
        root.Accept(analyzer);

        return new SubqueryCorrelationAnalysis(
            analyzer._subqueries,
            CopyAliasSet(analyzer._illegalOuterConsumingCteAliases));
    }

    public override void Visit(QueryNode node)
    {
        var localAliases = CollectAliases(node.From);
        EnterQuery(localAliases);

        try
        {
            node.From.Accept(this);
            node.Where?.Accept(this);
            node.GroupBy?.Accept(this);
            if (!_existsProjectionIgnoredQueries.Contains(node))
                node.Select.Accept(this);
            node.Skip?.Accept(this);
            node.Take?.Accept(this);
            node.Window?.Accept(this);
            node.Qualify?.Accept(this);
            node.OrderBy?.Accept(this);
            node.Accept(Visitor);
        }
        finally
        {
            _queryScopes.Pop();
        }
    }

    public override void Visit(InQueryNode node)
    {
        node.Left.Accept(this);

        var scope = new SubqueryScopeBuilder(
            node,
            GetVisibleAliases(),
            _cteDefinitionDepth > 0);

        _subqueryScopes.Push(scope);
        try
        {
            node.Subquery.Accept(this);
        }
        finally
        {
            _subqueryScopes.Pop();
        }

        _subqueries.Add(scope.Build());
        node.Accept(Visitor);
    }

    public override void Visit(ExistsQueryNode node)
    {
        var scope = new SubqueryScopeBuilder(
            node,
            GetVisibleAliases(),
            _cteDefinitionDepth > 0);
        var ignoredQueries = MarkExistsProjectionQueries(node.Subquery);

        _subqueryScopes.Push(scope);
        try
        {
            node.Subquery.Accept(this);
        }
        finally
        {
            foreach (var ignoredQuery in ignoredQueries)
                _existsProjectionIgnoredQueries.Remove(ignoredQuery);
            _subqueryScopes.Pop();
        }

        _subqueries.Add(scope.Build());
        node.Accept(Visitor);
    }

    public override void Visit(ScalarSubqueryNode node)
    {
        var scope = new SubqueryScopeBuilder(
            node,
            GetVisibleAliases(),
            _cteDefinitionDepth > 0);

        _subqueryScopes.Push(scope);
        try
        {
            node.Subquery.Accept(this);
        }
        finally
        {
            _subqueryScopes.Pop();
        }

        _subqueries.Add(scope.Build());
        node.Accept(Visitor);
    }

    internal void RecordAliasReference(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return;

        if (IsCurrentLocalAlias(alias))
            return;

        if (IsForbiddenAlias(alias))
        {
            _illegalOuterConsumingCteAliases.Add(alias);
            foreach (var scope in _subqueryScopes)
                scope.AddIllegalOuterConsumingCteAlias(alias);
        }

        if (!IsOuterAlias(alias))
            return;

        foreach (var scope in _subqueryScopes)
            if (scope.OuterAliases.Contains(alias))
                scope.AddCorrelatedAlias(alias);
    }
}
