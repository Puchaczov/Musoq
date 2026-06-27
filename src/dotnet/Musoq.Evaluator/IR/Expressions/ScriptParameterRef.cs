namespace Musoq.Evaluator.IR.Expressions;

public sealed record ScriptParameterRef(string Name, Type ReturnType) : IrExpression(ReturnType);
