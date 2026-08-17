using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal readonly record struct SemanticNodeResult(Node Node)
{
    public static SemanticNodeResult From(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return new SemanticNodeResult(node);
    }

    public void ApplyTo(SemanticTraversalFrame frame)
    {
        frame.PushNode(Node);
    }
}
