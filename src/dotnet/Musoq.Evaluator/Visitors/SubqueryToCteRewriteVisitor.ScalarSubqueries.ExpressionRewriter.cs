using System.Collections.Generic;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private sealed class ScalarSubqueryExpressionRewriter(
        SubqueryToCteRewriteVisitor owner,
        SubqueryCorrelationAnalysis analysis,
        List<CteInnerExpressionNode> cteInnerExpressions)
        : CloneQueryVisitor, ISubqueryExpressionContextRewriter<ScalarSubqueryJoin>
    {
        private readonly List<ScalarSubqueryJoin> _joins = [];
        private readonly Dictionary<string, ScalarSubqueryJoin> _preparedSubqueries = new();

        public IReadOnlyList<ScalarSubqueryJoin> Joins => _joins;

        public int JoinCount => _joins.Count;

        public ScalarSubqueryJoin[] TakeJoinsFrom(int index)
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
            expression.Accept(new ScalarSubqueryExpressionTraverser(this));
            return Nodes.Pop();
        }

        public override void Visit(ScalarSubqueryNode node)
        {
            if (!_preparedSubqueries.TryGetValue(node.Id, out var rewrite))
            {
                rewrite = owner.PrepareScalarSubquery(node, analysis, cteInnerExpressions);
                _preparedSubqueries.Add(node.Id, rewrite);
                _joins.Add(rewrite);
            }

            Nodes.Push(rewrite.Replacement);
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
    }

    private sealed class ScalarSubqueryExpressionTraverser(ScalarSubqueryExpressionRewriter visitor)
        : CloneTraverseVisitor(visitor)
    {
        public override void Visit(ScalarSubqueryNode node)
        {
            node.Accept((ScalarSubqueryExpressionRewriter)Visitor);
        }

        public override void Visit(WindowFunctionNode node)
        {
            node.FunctionCall.Accept(this);
            node.WindowSpecification?.Accept(this);
            node.Accept((ScalarSubqueryExpressionRewriter)Visitor);
        }

        public override void Visit(WindowSpecificationNode node)
        {
            foreach (var field in node.PartitionFields)
                field.Accept(this);
            foreach (var field in node.OrderByFields)
                field.Accept(this);
            node.Accept((ScalarSubqueryExpressionRewriter)Visitor);
        }
    }
}
