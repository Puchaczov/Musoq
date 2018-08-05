using Musoq.Parser;

namespace Musoq.Evaluator.Visitors
{
    public interface IAwareExpressionVisitor : IScopeAwareExpressionVisitor, IQueryPartAwareExpressionVisitor
    {}
}