using Musoq.Parser;

namespace Musoq.Evaluator.Visitors;

public class RewritePartsWithProperNullHandlingTraverseVisitor : CloneTraverseVisitor
{
    public RewritePartsWithProperNullHandlingTraverseVisitor(IExpressionVisitor visitor)
        : base(visitor)
    {
    }
}
