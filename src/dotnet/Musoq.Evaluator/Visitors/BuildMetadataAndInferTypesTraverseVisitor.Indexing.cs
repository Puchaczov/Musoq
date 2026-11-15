using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    private readonly Func<ArrayIndexNode, bool>? _bindUnsupportedIndex;

    public BuildMetadataAndInferTypesTraverseVisitor(BuildMetadataAndInferTypesVisitor visitor)
        : this((IAwareExpressionVisitor)visitor)
    {
        _bindUnsupportedIndex = visitor.TryBindUnsupportedIndex;
    }

    public override void Visit(ArrayIndexNode node)
    {
        node.Array.Accept(this);
        if (_bindUnsupportedIndex?.Invoke(node) == true)
            return;
        node.Index.Accept(this);
        node.Accept(Visitor);
    }
}
