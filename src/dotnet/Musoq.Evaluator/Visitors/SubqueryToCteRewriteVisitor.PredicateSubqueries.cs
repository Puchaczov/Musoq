using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.Helpers.Subqueries.SubqueryCorrelationUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private PredicateRewriteResult RewritePredicateApplySubqueries(
        SelectNode select,
        FromNode from,
        Node? whereExpression,
        GroupByNode? groupBy,
        OrderByNode? orderBy,
        WindowNode? window,
        QualifyNode? qualify,
        SubqueryCorrelationAnalysis analysis,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        var rewriter = new PredicateSubqueryExpressionRewriter(this, analysis, cteInnerExpressions);
        var context = RewriteExpressionContext(
            new SubqueryRewriteContext(select, from, whereExpression, groupBy, orderBy, window, qualify),
            rewriter,
            static (currentFrom, join) => new Parser.JoinFromNode(
                currentFrom,
                join.CteRef,
                join.JoinExpression,
                JoinType.LeftMark));

        return new PredicateRewriteResult(
            context.Select,
            context.From,
            context.WhereExpression,
            context.GroupBy,
            context.OrderBy,
            context.Window,
            context.Qualify);
    }

    private PredicateSubqueryJoin PreparePredicateApplySubquery(
        SubqueryInfo subqueryInfo,
        SubqueryCorrelationAnalysis analysis,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        return PreparePredicateSubqueryJoin(
            subqueryInfo with
            {
                RequiresLeftJoin = true,
                Correlation = FindCorrelation(subqueryInfo.PredicateNode, analysis)
            },
            cteInnerExpressions);
    }

    private static void ValidateHavingPredicateSubqueriesCanMoveBeforeGrouping(
        IReadOnlyList<SubqueryInfo> subqueries,
        GroupByNode groupBy)
    {
        var groupKeys = CollectGroupKeyColumns(groupBy);

        foreach (var subquery in subqueries)
        {
            if (CanMoveHavingPredicateSubqueryBeforeGrouping(subquery, groupKeys))
                continue;

            throw SubqueryDiagnosticFactory.InvalidSubquery(
                "HAVING predicate subquery rewrite",
                "Predicate subqueries in HAVING can be decorrelated before grouping only when they reference grouping keys. Predicates that depend on non-grouped row values require aggregate-phase APPLY lowering.",
                subquery.PredicateNode);
        }
    }

    private static bool CanMoveHavingPredicateSubqueryBeforeGrouping(
        SubqueryInfo subquery,
        IReadOnlySet<string> groupKeys)
    {
        if (subquery is { IsIn: true, InQueryNode: { } inQueryNode } &&
            !ReferencesOnlyGroupKeys(inQueryNode.Left, groupKeys))
            return false;

        if (subquery.Correlation is not { IsCorrelated: true })
            return true;

        if (subquery.Subquery is not QueryNode { Where: not null } query)
            return false;

        var outerColumns = CollectAccessColumns(query.Where.Expression)
            .Where(column => subquery.Correlation.CorrelatedAliases.Contains(column.Alias))
            .ToArray();

        return outerColumns.Length > 0 && outerColumns.All(column => IsGroupKey(column, groupKeys));
    }

    private static bool ReferencesOnlyGroupKeys(Node expression, IReadOnlySet<string> groupKeys)
    {
        var columns = CollectAccessColumns(expression);
        return columns.Length > 0 && columns.All(column => IsGroupKey(column, groupKeys));
    }

    private static HashSet<string> CollectGroupKeyColumns(GroupByNode groupBy)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in groupBy.Fields)
        foreach (var column in CollectAccessColumns(field.Expression))
        {
            keys.Add(CreateGroupKey(column.Alias, column.Name));
            if (string.IsNullOrWhiteSpace(column.Alias))
                keys.Add(CreateGroupKey(string.Empty, column.Name));
        }

        return keys;
    }

    private static bool IsGroupKey(AccessColumnNode column, IReadOnlySet<string> groupKeys) =>
        groupKeys.Contains(CreateGroupKey(column.Alias, column.Name)) ||
        groupKeys.Contains(CreateGroupKey(string.Empty, column.Name));

    private static string CreateGroupKey(string? alias, string name) => $"{alias ?? string.Empty}.{name}";

    private sealed record PredicateRewriteResult(
        SelectNode Select,
        FromNode From,
        Node? WhereExpression,
        GroupByNode? GroupBy,
        OrderByNode? OrderBy,
        WindowNode? Window,
        QualifyNode? Qualify);

    private sealed partial class PredicateSubqueryExpressionRewriter(
        SubqueryToCteRewriteVisitor owner,
        SubqueryCorrelationAnalysis analysis,
        List<CteInnerExpressionNode> cteInnerExpressions)
        : CloneQueryVisitor, ISubqueryExpressionContextRewriter<PredicateSubqueryJoin>
    {
        private readonly List<PredicateSubqueryJoin> _joins = [];
        private readonly Dictionary<string, PredicateSubqueryJoin> _preparedSubqueries = new();

        public IReadOnlyList<PredicateSubqueryJoin> Joins => _joins;

        public int JoinCount => _joins.Count;

        public PredicateSubqueryJoin[] TakeJoinsFrom(int index)
        {
            var count = _joins.Count - index;
            if (count <= 0)
                return [];

            var joins = _joins.GetRange(index, count).ToArray();
            _joins.RemoveRange(index, count);
            return joins;
        }

        public Node Rewrite(Node expression)
        {
            expression.Accept(new PredicateSubqueryExpressionTraverser(this));
            return Nodes.Pop();
        }

        public override void Visit(InQueryNode node)
        {
            Nodes.Push(PrepareSubquery(SubqueryInfo.CreateIn(node, false)).Replacement);
        }

        public override void Visit(ExistsQueryNode node)
        {
            Nodes.Push(PrepareSubquery(SubqueryInfo.CreateExists(node, false)).Replacement);
        }

        public override void Visit(NotNode node)
        {
            switch (node.Expression)
            {
                case InQueryNode inQuery:
                    Nodes.Push(PrepareNotInSubquery(inQuery));
                    return;

                case ExistsQueryNode existsQuery:
                    Nodes.Push(PrepareSubquery(SubqueryInfo.CreateExists(existsQuery, true)).Replacement);
                    return;

                default:
                    base.Visit(node);
                    return;
            }
        }

        public override void Visit(ScalarSubqueryNode node)
        {
            Nodes.Push(node);
        }

        public override void Visit(WindowFunctionNode node)
        {
            var specification = node.WindowSpecification != null
                ? (WindowSpecificationNode)Nodes.Pop()
                : null;
            var functionCall = (AccessMethodNode)Nodes.Pop();
            var rewritten = node.IsNamedWindowReference
                ? new WindowFunctionNode(
                    functionCall,
                    node.WindowName ?? throw new InvalidOperationException("Named window reference requires a window name."))
                : new WindowFunctionNode(
                    functionCall,
                    specification ?? throw new InvalidOperationException("Window function requires a window specification."));

            if (node.ReturnType is { } returnType && returnType != typeof(void))
                rewritten.SetReturnType(returnType);

            Nodes.Push(rewritten);
        }

        public override void Visit(WindowSpecificationNode node)
        {
            var orderByFields = new FieldOrderedNode[node.OrderByFields.Length];
            for (var i = node.OrderByFields.Length - 1; i >= 0; i--)
                orderByFields[i] = (FieldOrderedNode)Nodes.Pop();

            var partitionFields = new FieldNode[node.PartitionFields.Length];
            for (var i = node.PartitionFields.Length - 1; i >= 0; i--)
                partitionFields[i] = (FieldNode)Nodes.Pop();

            Nodes.Push(new WindowSpecificationNode(partitionFields, orderByFields, node.Frame));
        }

        private PredicateSubqueryJoin PrepareSubquery(SubqueryInfo subqueryInfo)
        {
            var key = CreatePreparedKey(subqueryInfo);
            if (_preparedSubqueries.TryGetValue(key, out var rewrite))
                return rewrite;

            rewrite = owner.PreparePredicateApplySubquery(
                subqueryInfo,
                analysis,
                cteInnerExpressions);
            _preparedSubqueries.Add(key, rewrite);
            _joins.Add(rewrite);
            return rewrite;
        }

        private static string CreatePreparedKey(SubqueryInfo subqueryInfo)
            => subqueryInfo.IsNegated ? $"not:{subqueryInfo.PredicateNode.Id}" : subqueryInfo.PredicateNode.Id;
    }
}
