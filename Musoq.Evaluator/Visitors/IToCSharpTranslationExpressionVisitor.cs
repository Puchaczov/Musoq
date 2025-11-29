using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public interface IToCSharpTranslationExpressionVisitor : IScopeAwareExpressionVisitor
{
    void SetQueryIdentifier(string identifier);

    MethodAccessType SetMethodAccessType(MethodAccessType type);
    
    void SetResultParallelizationImpossible();

    void IncrementMethodIdentifier();
        
    void SetInsideJoinOrApply(bool value);

    void AddNullSuspiciousSection();
        
    void RemoveNullSuspiciousSection();
    
    void InitializeCseForQuery(Node queryNode);
    
    void SetCaseWhenContext(bool isInside);
}