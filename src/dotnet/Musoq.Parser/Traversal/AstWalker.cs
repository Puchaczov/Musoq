using Musoq.Parser.Nodes;

namespace Musoq.Parser.Traversal;

internal abstract class AstWalker
{
    public void Walk(Node? node)
    {
        if (node == null)
            return;

        if (!Enter(node))
            return;

        foreach (var child in AstChildren.Of(node))
            Walk(child.Node);

        Leave(node);
    }

    protected virtual bool Enter(Node node)
    {
        return true;
    }

    protected virtual void Leave(Node node)
    {
    }
}
