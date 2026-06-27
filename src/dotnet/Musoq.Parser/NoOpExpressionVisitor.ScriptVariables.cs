using Musoq.Parser.Nodes;

namespace Musoq.Parser;

public abstract partial class NoOpExpressionVisitor
{
    public virtual void Visit(ScriptVariableDeclarationNode node)
    {
    }

    public virtual void Visit(ScriptVariableReferenceNode node)
    {
    }
}