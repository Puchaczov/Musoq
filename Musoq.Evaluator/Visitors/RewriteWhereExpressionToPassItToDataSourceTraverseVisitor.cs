using Musoq.Parser;

namespace Musoq.Evaluator.Visitors;

public class RewriteWhereExpressionToPassItToDataSourceTraverseVisitor : CloneTraverseVisitor
{
    public RewriteWhereExpressionToPassItToDataSourceTraverseVisitor(IExpressionVisitor visitor)
        : base(visitor)
    {
    }
}