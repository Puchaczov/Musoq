namespace Musoq.Evaluator.IR.Expressions;

public sealed record WindowFunctionRef(int WindowIndex, Type ReturnType) : IrExpression(ReturnType);
