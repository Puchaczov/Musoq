using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(FieldNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var expression = SafePop(Nodes, VisitorOperationNames.VisitFieldNode);
        Nodes.Push(new FieldNode(expression, node.FieldOrder, node.FieldName, node.HasExplicitFieldName));
    }

    public override void Visit(FieldOrderedNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var expression = SafePop(Nodes, VisitorOperationNames.VisitFieldOrderedNode);
        Nodes.Push(new FieldOrderedNode(expression, node.FieldOrder, node.FieldName, node.HasExplicitFieldName, node.Order, node.NullOrdering));
    }

    public override void Visit(SelectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = CreateFields(node.Fields);

        CollectSelectFieldAliases(fields);

        Nodes.Push(new SelectNode(fields.ToArray(), node.IsDistinct));
    }

    public override void Visit(GroupSelectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = CreateFields(node.Fields);

        CollectSelectFieldAliases(fields);

        Nodes.Push(new GroupSelectNode(fields.ToArray()));
    }
}
