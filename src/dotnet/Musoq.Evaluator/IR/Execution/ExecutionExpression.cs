namespace Musoq.Evaluator.IR.Execution;

public abstract record ExecutionExpression
{
    protected ExecutionExpression(ExecutionTypeRef returnType)
    {
        ReturnType = returnType;
    }

    public ExecutionInterpretationShape? InterpretationShape { get; init; }

    public virtual ExecutionTypeRef ReturnType { get; init; }
}
