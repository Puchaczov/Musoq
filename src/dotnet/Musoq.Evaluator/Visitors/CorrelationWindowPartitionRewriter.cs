using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private sealed class CorrelationWindowPartitionRewriter(FieldNode[] correlationFields)
        : CloneQueryVisitor
    {
        public T Rewrite<T>(T node) where T : Node
        {
            node.Accept(new CorrelationWindowPartitionTraverser(this));
            return (T)Nodes.Pop();
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
                    node.WindowName ?? throw new InvalidOperationException(
                        "Named window reference requires a window name."))
                : new WindowFunctionNode(
                    functionCall,
                    specification ?? throw new InvalidOperationException(
                        "Window function requires a window specification."));

            if (node.ReturnType is { } returnType && returnType != typeof(void))
                rewritten.SetReturnType(returnType);
            Nodes.Push(rewritten);
        }

        public override void Visit(WindowSpecificationNode node)
        {
            var orderByFields = new FieldOrderedNode[node.OrderByFields.Length];
            for (var i = node.OrderByFields.Length - 1; i >= 0; i--)
                orderByFields[i] = (FieldOrderedNode)Nodes.Pop();

            var originalPartitionFields = new FieldNode[node.PartitionFields.Length];
            for (var i = node.PartitionFields.Length - 1; i >= 0; i--)
                originalPartitionFields[i] = (FieldNode)Nodes.Pop();

            var partitionFields = PrependMissingCorrelationFields(originalPartitionFields);
            Nodes.Push(new WindowSpecificationNode(partitionFields, orderByFields, node.Frame));
        }

        private FieldNode[] PrependMissingCorrelationFields(FieldNode[] originalFields)
        {
            var existing = new HashSet<string>(
                originalFields.Select(static field => field.Expression.ToString()),
                StringComparer.OrdinalIgnoreCase);
            var missing = correlationFields
                .Where(field => !existing.Contains(field.Expression.ToString()))
                .Select(CloneCorrelationField)
                .ToArray();
            return [..missing, ..originalFields];
        }

        private static FieldNode CloneCorrelationField(FieldNode field)
        {
            if (field.Expression is not AccessColumnNode access)
                return new FieldNode(field.Expression, field.FieldOrder, string.Empty);

            return new FieldNode(
                new AccessColumnNode(
                    access.Name,
                    access.Alias,
                    access.ReturnType,
                    access.Span,
                    access.IntendedTypeName),
                field.FieldOrder,
                string.Empty);
        }
    }

    private sealed class CorrelationWindowPartitionTraverser(CorrelationWindowPartitionRewriter visitor)
        : CloneTraverseVisitor(visitor)
    {
        public override void Visit(WindowFunctionNode node)
        {
            node.FunctionCall.Accept(this);
            node.WindowSpecification?.Accept(this);
            node.Accept((CorrelationWindowPartitionRewriter)Visitor);
        }

        public override void Visit(WindowSpecificationNode node)
        {
            foreach (var field in node.PartitionFields)
                field.Accept(this);
            foreach (var field in node.OrderByFields)
                field.Accept(this);
            node.Accept((CorrelationWindowPartitionRewriter)Visitor);
        }
    }
}
