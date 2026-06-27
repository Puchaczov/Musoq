using Musoq.Parser;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    public override void Visit(UnpivotFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        SetQueryPart(QueryPart.From);
        node.Source.Accept(this);

        foreach (var entry in node.Entries)
            entry.Expression.Accept(this);

        foreach (var keepField in node.KeepFields)
            keepField.Accept(this);

        node.Accept(Visitor);
    }
}
