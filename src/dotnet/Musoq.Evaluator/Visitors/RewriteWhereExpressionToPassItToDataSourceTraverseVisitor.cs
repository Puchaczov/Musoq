using Musoq.Parser;

namespace Musoq.Evaluator.Visitors;

public class RewriteWhereExpressionToPassItToDataSourceTraverseVisitor(IExpressionVisitor visitor)
    : CloneTraverseVisitor(visitor);
