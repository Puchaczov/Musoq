using Musoq.Parser.Nodes;

namespace Musoq.Parser.Traversal;

/// <summary>
///     Performs a depth-first traversal over parser nodes.
/// </summary>
public abstract class AstWalker
{
    /// <summary>
    ///     Visits <paramref name="node" /> and all of its descendants.
    /// </summary>
    /// <param name="node">The root node to visit, or <see langword="null" />.</param>
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

    /// <summary>
    ///     Called before a node's children are visited.
    /// </summary>
    /// <param name="node">The node being entered.</param>
    /// <returns><see langword="true" /> to visit children; otherwise, <see langword="false" />.</returns>
    protected virtual bool Enter(Node node)
    {
        return true;
    }

    /// <summary>
    ///     Called after a node's children have been visited.
    /// </summary>
    /// <param name="node">The node being left.</param>
    protected virtual void Leave(Node node)
    {
    }
}
