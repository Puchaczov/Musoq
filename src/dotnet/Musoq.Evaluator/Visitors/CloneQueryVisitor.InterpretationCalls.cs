using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(InterpretCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var dataSource = Nodes.Pop();
        Nodes.Push(new InterpretCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(ParseCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var dataSource = Nodes.Pop();
        Nodes.Push(new ParseCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(TryInterpretCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var dataSource = Nodes.Pop();
        Nodes.Push(new TryInterpretCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(TryParseCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var dataSource = Nodes.Pop();
        Nodes.Push(new TryParseCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(PartialInterpretCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var dataSource = Nodes.Pop();
        Nodes.Push(new PartialInterpretCallNode(dataSource, node.SchemaName, node.ReturnType));
    }

    public override void Visit(InterpretAtCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var offset = Nodes.Pop();
        var dataSource = Nodes.Pop();
        Nodes.Push(new InterpretAtCallNode(dataSource, offset, node.SchemaName, node.ReturnType));
    }

    private void CloneBinaryNodeWithSpan<T>(T node, string operationName, Func<Node, Node, Node> factory)
        where T : BinaryNode
    {
        var nodes = SafePopMultiple(Nodes, 2, operationName);
        Nodes.Push(factory(nodes[0], nodes[1]).WithSpan(node.Span));
    }

    private void CloneBinaryNode(Func<Node, Node, Node> factory)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(factory(left, right));
    }
}
