namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCollectionInCheck(
    ExecutionExpression Expression,
    ExecutionScriptParameterRead Collection,
    ExecutionTypeRef ElementType,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionCollectionInCheck(
        ExecutionExpression expression,
        ExecutionScriptParameterRead collection,
        Type elementType,
        Type returnType)
        : this(expression, collection, ExecutionClrBindingFactory.FromClr(elementType), ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
