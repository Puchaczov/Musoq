namespace Musoq.Evaluator.IR.Execution;

/// <summary>A formal operand inside a generated buffer kernel, never a movable query scalar.</summary>
public sealed record ExecutionByteBufferArgument(int Index, ExecutionTypeRef ArgumentType)
    : ExecutionExpression(ArgumentType);
