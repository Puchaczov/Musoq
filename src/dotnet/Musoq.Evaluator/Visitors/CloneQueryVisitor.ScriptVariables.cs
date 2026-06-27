using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(ScriptVariableDeclarationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var initializer = Nodes.Pop();
        Nodes.Push(new ScriptVariableDeclarationNode(node.Name, node.TypeName, node.IsNullable, initializer, node.Span));
    }

    public override void Visit(ScriptVariableReferenceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ScriptVariableReferenceNode(node.Name, node.ReturnType, node.Span));
    }
}