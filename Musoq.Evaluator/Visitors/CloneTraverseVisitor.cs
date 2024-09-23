using Musoq.Parser;

namespace Musoq.Evaluator.Visitors;

public class CloneTraverseVisitor(IExpressionVisitor visitor) : RawTraverseVisitor<IExpressionVisitor>(visitor);