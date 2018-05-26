namespace Musoq.Evaluator.Visitors
{
    public interface IToCSharpTranslationExpressionVisitor : IScopeAwareExpressionVisitor
    {
        void SetQueryIdentifier(string identifier);

        void SetMethodAccessType(MethodAccessType type);

        void IncrementMethodIdentifier();
    }
}