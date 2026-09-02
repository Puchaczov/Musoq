using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(FieldNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var expression = PopSemanticNode(VisitorOperationNames.VisitFieldNode);
        PushSemanticNode(new FieldNode(expression, node.FieldOrder, node.FieldName, node.HasExplicitFieldName, node.Span)
            .WithFullSpan(node.FullSpan));
    }

    public override void Visit(FieldOrderedNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var expression = PopSemanticNode(VisitorOperationNames.VisitFieldOrderedNode);
        PushSemanticNode(new FieldOrderedNode(expression, node.FieldOrder, node.FieldName, node.HasExplicitFieldName, node.Order, node.NullOrdering)
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan));
    }

    public override void Visit(SelectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = CreateFields(node.Fields);

        CollectSelectFieldAliases(fields);

        PushSemanticNode(new SelectNode(fields.ToArray(), node.IsDistinct, node.Span)
            .WithFullSpan(node.FullSpan));
    }

    public override void Visit(GroupSelectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = CreateFields(node.Fields);

        CollectSelectFieldAliases(fields);

        PushSemanticNode(new GroupSelectNode(fields.ToArray())
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan));
    }
}
