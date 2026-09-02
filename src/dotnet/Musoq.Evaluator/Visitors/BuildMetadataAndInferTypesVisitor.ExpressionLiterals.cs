using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(StringNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(string).Assembly);
        PushSemanticNode(new StringNode(node.Value, node.Span));
    }

    public override void Visit(DecimalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(decimal).Assembly);
        PushSemanticNode(new DecimalNode(node.Value, node.Span));
    }

    public override void Visit(IntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(int).Assembly);
        PushSemanticNode(new IntegerNode(node.ObjValue, node.Span));
    }

    public override void Visit(HexIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(long).Assembly);
        PushSemanticNode(new HexIntegerNode(node.ObjValue, node.Span));
    }

    public override void Visit(BinaryIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(long).Assembly);
        PushSemanticNode(new BinaryIntegerNode(node.ObjValue, node.Span));
    }

    public override void Visit(OctalIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(long).Assembly);
        PushSemanticNode(new OctalIntegerNode(node.ObjValue, node.Span));
    }

    public override void Visit(BooleanNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(bool).Assembly);
        PushSemanticNode(new BooleanNode(node.Value, node.Span));
    }

    public override void Visit(WordNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(string).Assembly);
        var word = node is AggregateIdentifierNode aggregateIdentifier
            ? new AggregateIdentifierNode(aggregateIdentifier.Value, aggregateIdentifier.DisplayName)
            : new WordNode(node.Value, node.Span);
        PushSemanticNode(word);
    }

    public override void Visit(NullNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        PushSemanticNode(new NullNode(node.ReturnType, node.Span));
    }
}
