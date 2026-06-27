namespace Musoq.Evaluator.IR.Expressions.CollectionParameters;

public sealed record CollectionInCheck(
    IrExpression Expression,
    ScriptParameterRef Collection,
    Type ElementType,
    Type ReturnType) : IrExpression(ReturnType);
