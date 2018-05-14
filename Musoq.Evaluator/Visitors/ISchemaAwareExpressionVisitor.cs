using System.Text;
using Musoq.Evaluator.Utils;
using Musoq.Parser;

namespace Musoq.Evaluator.Visitors
{
    public interface IToCSharpTranslationExpressionVisitor : IScopeAwareExpressionVisitor
    {
        void QueryBegins();
        void QueryEnds();

        void SetQueryIdentifier(string identifier);

        void SetCodePattern(StringBuilder code);
        void SetJoinsAmount(int amount);

        void SetMethodAccessType(MethodAccessType type);
        void SelectBegins();
        void SelectEnds();
        void TurnOnAggregateMethodsToColumnAcceess();
        void TurnOffAggregateMethodsToColumnAcceess();
    }

    public interface IScopeAwareExpressionVisitor : IExpressionVisitor
    {
        void SetScope(Scope scope);
    }
}