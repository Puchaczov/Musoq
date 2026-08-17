using System.Collections.Generic;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Optimization.Logical.Subqueries;

internal sealed class CorrelatedSubqueryRewriteRequestBuilder
    : RawTraverseVisitor<CorrelatedSubqueryRewriteRequestBuilder.NoOpVisitor>
{
    private readonly IReadOnlyDictionary<Node, SubqueryCorrelationInfo> _correlations;
    private readonly List<CorrelatedSubqueryRewriteRequest> _requests = [];
    private SubqueryEvaluationPhase _phase = SubqueryEvaluationPhase.Source;
    private bool _isDirectFilter;
    private bool _isNegated;

    private CorrelatedSubqueryRewriteRequestBuilder(SubqueryCorrelationAnalysis analysis)
        : base(new NoOpVisitor())
    {
        var correlations = new Dictionary<Node, SubqueryCorrelationInfo>(ReferenceEqualityComparer.Instance);
        foreach (var correlation in analysis.Subqueries)
            correlations.TryAdd(correlation.Node, correlation);
        _correlations = correlations;
    }

    public static IReadOnlyList<CorrelatedSubqueryRewriteRequest> Build(
        Node root,
        SubqueryCorrelationAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(analysis);

        var builder = new CorrelatedSubqueryRewriteRequestBuilder(analysis);
        root.Accept(builder);
        return builder._requests;
    }

    public override void Visit(QueryNode node)
    {
        VisitAtPhase(node.From, SubqueryEvaluationPhase.Source);
        if (node.Where != null)
            VisitPredicateRoot(node.Where.Expression, SubqueryEvaluationPhase.Filter);

        if (node.GroupBy != null)
        {
            foreach (var field in node.GroupBy.Fields)
                VisitAtPhase(field, SubqueryEvaluationPhase.Grouping);
            if (node.GroupBy.Having != null)
                VisitPredicateRoot(node.GroupBy.Having.Expression, SubqueryEvaluationPhase.Having);
        }

        if (node.Window != null)
            VisitAtPhase(node.Window, SubqueryEvaluationPhase.Window);
        if (node.Qualify != null)
            VisitPredicateRoot(node.Qualify.Expression, SubqueryEvaluationPhase.Qualify);

        VisitAtPhase(node.Select, SubqueryEvaluationPhase.Projection);

        if (node.OrderBy != null)
            VisitAtPhase(node.OrderBy, SubqueryEvaluationPhase.Ordering);
        if (node.Skip != null)
            VisitAtPhase(node.Skip, SubqueryEvaluationPhase.RowLimit);
        if (node.Take != null)
            VisitAtPhase(node.Take, SubqueryEvaluationPhase.RowLimit);

        node.Accept(Visitor);
    }

    public override void Visit(InQueryNode node)
    {
        node.Left.Accept(this);
        Record(node);
        node.Subquery.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(ExistsQueryNode node)
    {
        Record(node);
        node.Subquery.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(ScalarSubqueryNode node)
    {
        Record(node);
        node.Subquery.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(NotNode node)
    {
        if (node.Expression is InQueryNode or ExistsQueryNode)
        {
            WithUsage(_isDirectFilter, true, () => node.Expression.Accept(this));
            node.Accept(Visitor);
            return;
        }

        base.Visit(node);
    }

    private void VisitPredicateRoot(Node expression, SubqueryEvaluationPhase phase)
    {
        var previousPhase = _phase;
        _phase = phase;
        try
        {
            VisitDirectFilter(expression);
        }
        finally
        {
            _phase = previousPhase;
        }
    }

    private void VisitDirectFilter(Node expression)
    {
        switch (expression)
        {
            case AndNode and:
                VisitDirectFilter(and.Left);
                VisitDirectFilter(and.Right);
                return;
            case NotNode { Expression: InQueryNode or ExistsQueryNode } not:
                WithUsage(true, true, () => not.Expression.Accept(this));
                return;
            case InQueryNode or ExistsQueryNode:
                WithUsage(true, false, () => expression.Accept(this));
                return;
            default:
                WithUsage(false, false, () => expression.Accept(this));
                return;
        }
    }

    private void VisitAtPhase(Node node, SubqueryEvaluationPhase phase)
    {
        var previousPhase = _phase;
        _phase = phase;
        try
        {
            WithUsage(false, false, () => node.Accept(this));
        }
        finally
        {
            _phase = previousPhase;
        }
    }

    private void WithUsage(bool isDirectFilter, bool isNegated, Action action)
    {
        var previousDirectFilter = _isDirectFilter;
        var previousNegated = _isNegated;
        _isDirectFilter = isDirectFilter;
        _isNegated = isNegated;
        try
        {
            action();
        }
        finally
        {
            _isDirectFilter = previousDirectFilter;
            _isNegated = previousNegated;
        }
    }

    private void Record(Node node)
    {
        if (!_correlations.TryGetValue(node, out var correlation) || !correlation.IsCorrelated)
            return;

        _requests.Add(new CorrelatedSubqueryRewriteRequest(
            node,
            correlation.Facts,
            _phase,
            _isDirectFilter,
            _isNegated));
    }

    internal sealed class NoOpVisitor : NoOpExpressionVisitor;
}
