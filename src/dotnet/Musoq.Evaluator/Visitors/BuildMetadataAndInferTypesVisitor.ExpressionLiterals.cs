using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(StringNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(string).Assembly);
        Nodes.Push(new StringNode(node.Value));
    }

    public override void Visit(DecimalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(decimal).Assembly);
        Nodes.Push(new DecimalNode(node.Value));
    }

    public override void Visit(IntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(int).Assembly);
        Nodes.Push(new IntegerNode(node.ObjValue));
    }

    public override void Visit(HexIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(long).Assembly);
        Nodes.Push(new HexIntegerNode(node.ObjValue));
    }

    public override void Visit(BinaryIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(long).Assembly);
        Nodes.Push(new BinaryIntegerNode(node.ObjValue));
    }

    public override void Visit(OctalIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(long).Assembly);
        Nodes.Push(new OctalIntegerNode(node.ObjValue));
    }

    public override void Visit(BooleanNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(bool).Assembly);
        Nodes.Push(new BooleanNode(node.Value));
    }

    public override void Visit(WordNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(string).Assembly);
        Nodes.Push(new WordNode(node.Value));
    }

    public override void Visit(NullNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new NullNode(node.ReturnType));
    }
}
