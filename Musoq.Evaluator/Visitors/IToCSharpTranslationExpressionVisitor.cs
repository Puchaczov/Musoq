namespace Musoq.Evaluator.Visitors
{
    public interface IToCSharpTranslationExpressionVisitor : IScopeAwareExpressionVisitor
    {
        void SetQueryIdentifier(string identifier);

        MethodAccessType SetMethodAccessType(MethodAccessType type);

        void IncrementMethodIdentifier();
    }
}