using System;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class RewritePartsWithProperNullHandlingVisitor : CloneQueryVisitor
{
    private readonly Type _nullableType;

    public RewritePartsWithProperNullHandlingVisitor(Type nullableType)
    {
        _nullableType = nullableType;
    }
    
    public Node RewrittenNode => Nodes.Peek();

    public override void Visit(NullNode node)
    {
        Nodes.Push(new NullNode(_nullableType));
    }
}