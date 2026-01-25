using Musoq.Parser;

namespace Musoq.Evaluator.Visitors;

public class ExtractAccessColumnFromQueryTraverseVisitor(IExpressionVisitor visitor)
    : CloneTraverseVisitor(visitor);
