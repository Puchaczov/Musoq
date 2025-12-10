using Microsoft.CodeAnalysis.CSharp;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public interface IToCSharpTranslationExpressionVisitor : IScopeAwareExpressionVisitor
{
    CSharpCompilation Compilation { get; }
    
    string AccessToClassPath { get; }
    
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