using Musoq.Evaluator.Utils;
using Musoq.Parser;

namespace Musoq.Evaluator.Visitors
{
    public interface IScopeAwareExpressionVisitor : IExpressionVisitor
    {
        void SetScope(Scope scope);
    }
}