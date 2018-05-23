namespace Musoq.Evaluator.Visitors
{
    public interface IToCSharpTranslationExpressionVisitor : IScopeAwareExpressionVisitor
    {
        void QueryBegins();
        void QueryEnds();

        void SetQueryIdentifier(string identifier);
        void SetJoinsAmount(int amount);

        void SetMethodAccessType(MethodAccessType type);
        void TurnOnAggregateMethodsToColumnAcceess();
        void TurnOffAggregateMethodsToColumnAcceess();
    }
}