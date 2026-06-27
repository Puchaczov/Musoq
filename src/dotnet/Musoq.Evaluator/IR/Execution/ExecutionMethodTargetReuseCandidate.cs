namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionMethodTargetReuseCandidate(
    ExecutionMethodCall MethodCall,
    ExecutionMethodTargetScope? Scope = null) : ExecutionExpression(MethodCall.ReturnType);
