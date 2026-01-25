using Musoq.Parser;

namespace Musoq.Evaluator.Visitors;

public class RewriteToUpdatedColumnAccessTraverser(IExpressionVisitor visitor) : CloneTraverseVisitor(visitor);
