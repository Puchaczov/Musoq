using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    public void Visit(ScriptVariableDeclarationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var initializer = Nodes.Pop();
        Nodes.Push(StructuralNodeRebuildSupport.RebuildScriptVariable(node, initializer));

    }

    public void Visit(ScriptVariableReferenceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ScriptVariableReferenceNode(node.Name, node.ReturnType, node.Span));
    }
}