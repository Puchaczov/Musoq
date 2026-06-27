namespace Musoq.Evaluator.IR.Expressions;

public sealed record ScriptVariableRef(string Name, Type ReturnType) : IrExpression(ReturnType);