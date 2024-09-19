using Musoq.Parser;

namespace Musoq.Evaluator.Visitors;

public class RewriteWhereConditionWithUpdatedColumnAccessTraverser(IExpressionVisitor visitor) : CloneTraverseVisitor(visitor);