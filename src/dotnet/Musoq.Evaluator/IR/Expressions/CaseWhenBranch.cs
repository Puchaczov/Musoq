namespace Musoq.Evaluator.IR.Expressions;

public sealed record CaseWhenBranch(IrExpression Condition, IrExpression Result);
