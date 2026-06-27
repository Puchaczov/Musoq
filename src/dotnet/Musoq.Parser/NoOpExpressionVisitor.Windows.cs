using Musoq.Parser.Nodes;

namespace Musoq.Parser;

public abstract partial class NoOpExpressionVisitor
{
    public virtual void Visit(WindowFunctionNode node) { }

    public virtual void Visit(WindowSpecificationNode node) { }

    public virtual void Visit(WindowFrameNode node) { }

    public virtual void Visit(WindowFrameBoundNode node) { }

    public virtual void Visit(WindowDefinitionNode node) { }

    public virtual void Visit(WindowNode node) { }

    public virtual void Visit(QualifyNode node) { }
}
