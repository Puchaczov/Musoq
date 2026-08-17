using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionPatternMatch(
    ExecutionExpression Expression,
    ExecutionExpression Pattern,
    PatternKind Kind,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionPatternMatch(
        ExecutionExpression expression,
        ExecutionExpression pattern,
        PatternKind kind,
        Type returnType)
        : this(expression, pattern, kind, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
