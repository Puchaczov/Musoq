namespace Musoq.Evaluator.IR.Expressions;

public sealed record CaseWhen(
    CaseWhenBranch[] Branches,
    IrExpression? ElseExpression,
    Type ReturnType) : IrExpression(ReturnType);
