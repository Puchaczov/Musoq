using Musoq.Parser;

namespace Musoq.Evaluator.Visitors;

public class RewritePartsWithProperNullHandlingTraverseVisitor(IExpressionVisitor visitor)
    : CloneTraverseVisitor(visitor);
