using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Evaluator.Visitors;

internal sealed class SubqueryFallbackSelection(SubqueryToCteRewriteVisitor owner, SubqueryCorrelationAnalysis analysis)
{
    internal QueryNode Select(QueryNode query)
    {
        var selection = new FallbackSelection(owner, analysis);
        query.Accept(new FallbackSelectionTraversal(selection));
        return selection.Result;
    }

    private static bool HasUnqualifiedReference(Node node) =>
        node switch
        {
            DotNode { Root: IdentifierNode, Expression: IdentifierNode } => false,
            AccessColumnNode column => string.IsNullOrEmpty(column.Alias),
            IdentifierNode => true,
            _ => ParserNodeChildTraversal.EnumerateChildren(node).Any(HasUnqualifiedReference),
        };

    private static bool NeedsBoundPredicateShape(Node query, SubqueryApplyKind kind, bool correlated)
    {
        if (kind is not (SubqueryApplyKind.In or SubqueryApplyKind.Exists))
            return false;
        if (query is not QueryNode body)
            return correlated || kind == SubqueryApplyKind.Exists && query is UnionAllNode;
        if (correlated && (body.Skip != null || body.Take != null || body.GroupBy != null || body.Qualify != null))
            return true;

        // Before binding, a source-supplied method in the projection may be a global aggregate.
        return (correlated || kind == SubqueryApplyKind.Exists)
            && ParserNodeChildTraversal.ContainsProjectionCall(body.Select);
    }

    private sealed class FallbackSelection(SubqueryToCteRewriteVisitor owner, SubqueryCorrelationAnalysis analysis)
        : CloneQueryVisitor
    {
        internal QueryNode Result => (QueryNode)Nodes.Pop();
        internal int GuardDepth { get; set; }
        internal Node? DirectFilter { get; set; }

        public override void Visit(WindowFunctionNode node) => Nodes.Push(SubqueryWindowCloner.Clone(node, Nodes));

        public override void Visit(ScalarSubqueryNode node)
        {
            if (node is SubqueryApplyNode)
            {
                Nodes.Push(node);
                return;
            }
            Nodes.Push(Select(node, node.Subquery, SubqueryApplyKind.Scalar, null));
        }

        public override void Visit(InQueryNode node)
        {
            var operand = Nodes.Pop();
            Nodes.Push(Select(node, node.Subquery, SubqueryApplyKind.In, operand));
        }

        public override void Visit(ExistsQueryNode node)
        {
            if (node is not QuantifiedSubqueryNode quantified)
            {
                Nodes.Push(Select(node, node.Subquery, SubqueryApplyKind.Exists, null));
                return;
            }
            var operand = Nodes.Pop();
            if (
                ReferenceEquals(DirectFilter, node)
                && GuardDepth == 0
                && QuantifiedFilterPredicate.Create(quantified, operand) is { } predicate
            )
            {
                Nodes.Push(predicate);
                return;
            }
            owner.RecordFallback(node, "Quantified value requires three-valued reduction after inner result shaping.");
            var kind =
                quantified.Quantifier == TokenType.All ? SubqueryApplyKind.All
                : quantified.Comparison == TokenType.Equality ? SubqueryApplyKind.In
                : SubqueryApplyKind.Any;
            Nodes.Push(
                new SubqueryApplyNode(node.Subquery, kind, operand)
                {
                    Comparison = quantified.Comparison,
                }.CopySpansFrom(node)
            );
        }

        private Node Select(Node original, Node query, SubqueryApplyKind kind, Node? operand)
        {
            var reason = GetFallbackReason(original, query, kind);
            if (reason is not null)
            {
                owner.RecordFallback(original, reason);
                return new SubqueryApplyNode(query, kind, operand).CopySpansFrom(original);
            }
            return original switch
            {
                InQueryNode => new InQueryNode(operand!, query).CopySpansFrom(original),
                _ => original,
            };
        }

        private string? GetFallbackReason(Node original, Node query, SubqueryApplyKind kind)
        {
            if (kind == SubqueryApplyKind.Scalar && SubqueryBindingScope.GetSubqueryFields(query).Length != 1)
                throw SubqueryDiagnosticFactory.InvalidSubquery(
                    "scalar subquery validation",
                    "Scalar subquery must return exactly one column.",
                    original
                );
            var correlation = analysis.Subqueries.FirstOrDefault(info => ReferenceEquals(info.Node, original));
            if (HasUnqualifiedReference(query))
                return "Unqualified references require lexical scope binding before selecting an execution strategy.";
            if (GuardDepth > 0)
                return "The enclosing evaluation guard requires on-demand execution.";
            if (correlation is { IsCorrelated: true } && ParserNodeChildTraversal.Contains<WindowFunctionNode>(query))
                return "The window frame must be evaluated over the rows matching the current outer binding.";
            if (original is InQueryNode membership && !QuantifiedFilterPredicate.IsSimple(membership.Left))
                return "Membership requires its left operand to be evaluated once.";
            if (NeedsBoundPredicateShape(query, kind, correlation is { IsCorrelated: true }))
                return "Predicate truth depends on the completed inner result for this binding.";
            if (kind == SubqueryApplyKind.Scalar && correlation is { IsCorrelated: true })
            {
                var projection = SubqueryBindingScope.GetSubqueryFields(query)[0].Expression;
                if (SubqueryCorrelationUtilities.ReferencesAnyAlias(projection, correlation.CorrelatedAliases))
                    return "The projection contains typed outer references.";
                if (ResultRowNumberProjection.ContainsRowNumber(projection))
                    return "Scalar RowNumber() depends on the ordering and paging of each outer binding.";
                if (
                    query is QueryNode { GroupBy: null } body
                    && SubqueryToCteRewriteVisitor.RequiresResultMaterialization(body)
                    && ParserNodeChildTraversal.ContainsProjectionCall(body.Select)
                )
                    return "The scalar result is shaped after evaluating a potentially aggregate projection.";
            }
            if (
                kind == SubqueryApplyKind.In
                && correlation is { IsCorrelated: true, Facts.HasEqualityKeys: false }
                && !owner.RewriteRequests.Any(request =>
                    ReferenceEquals(request.Node, original) && request.IsDirectFilter
                )
            )
                return "The membership value requires per-binding comparison without an equality partition.";
            return owner.ProbeSubquerySpecialization(original, analysis);
        }
    }

    private sealed class FallbackSelectionTraversal(FallbackSelection selection) : CloneTraverseVisitor(selection)
    {
        private bool _requiresProjectionScope;

        public override void Visit(WindowFunctionNode node)
        {
            node.FunctionCall.Accept(this);
            node.WindowSpecification?.Accept(this);
            node.Accept(selection);
        }

        public override void Visit(InNode node)
        {
            node.Left.Accept(this);
            var members = (ArgsListNode)node.Right;
            if (members.Args.Length > 0)
                members.Args[0].Accept(this);
            selection.GuardDepth++;
            for (var index = 1; index < members.Args.Length; index++)
                members.Args[index].Accept(this);
            selection.GuardDepth--;
            members.Accept(selection);
            node.Accept(selection);
        }

        public override void Visit(QueryNode node)
        {
            var previous = _requiresProjectionScope;
            _requiresProjectionScope = node.Where is not null || node.Qualify is not null || node.GroupBy is not null;
            base.Visit(node);
            _requiresProjectionScope = previous;
        }

        public override void Visit(SelectNode node)
        {
            if (_requiresProjectionScope)
                selection.GuardDepth++;
            base.Visit(node);
            if (_requiresProjectionScope)
                selection.GuardDepth--;
        }

        public override void Visit(GroupByNode node)
        {
            selection.GuardDepth++;
            foreach (var field in node.Fields)
                field.Accept(this);
            selection.GuardDepth--;
            node.Having?.Accept(this);
            node.Accept(selection);
        }

        public override void Visit(ScalarSubqueryNode node) => node.Accept(selection);

        public override void Visit(ExistsQueryNode node)
        {
            if (node is QuantifiedSubqueryNode quantified)
                quantified.Left.Accept(this);
            node.Accept(selection);
        }

        public override void Visit(WhereNode node)
        {
            selection.DirectFilter = node.Expression;
            base.Visit(node);
            selection.DirectFilter = null;
        }

        public override void Visit(InQueryNode node)
        {
            node.Left.Accept(this);
            node.Accept(selection);
        }

        public override void Visit(AndNode node) => VisitGuardedBinary(node);

        public override void Visit(OrNode node) => VisitGuardedBinary(node);

        public override void Visit(CoalesceNode node) => VisitGuardedBinary(node);

        private void VisitGuardedBinary(BinaryNode node)
        {
            node.Left.Accept(this);
            selection.GuardDepth++;
            node.Right.Accept(this);
            selection.GuardDepth--;
            node.Accept(selection);
        }

        public override void Visit(CaseNode node)
        {
            selection.GuardDepth++;
            base.Visit(node);
            selection.GuardDepth--;
        }
    }
}
