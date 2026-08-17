namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionHashProbe(
    ExecutionVariable Hash,
    ExecutionVariable Matches,
    ExecutionExpression Key,
    ExecutionTypeRef KeyType,
    ExecutionTypeRef RowType,
    ExecutionBlock Body,
    ExecutionBlock? NoMatchBody = null,
    ExecutionVariable? MatchFound = null,
    string? GeneratedRowTypeName = null,
    string? KeyVariableName = null) : ExecutionNode
{
    internal ExecutionHashProbe(
        ExecutionVariable hash,
        ExecutionVariable matches,
        ExecutionExpression key,
        Type keyType,
        Type rowType,
        ExecutionBlock body,
        ExecutionBlock? noMatchBody = null,
        ExecutionVariable? matchFound = null,
        string? generatedRowTypeName = null,
        string? keyVariableName = null)
        : this(
            hash,
            matches,
            key,
            ExecutionClrBindingFactory.FromClr(keyType),
            ExecutionClrBindingFactory.FromClr(rowType),
            body,
            noMatchBody,
            matchFound,
            generatedRowTypeName,
            keyVariableName)
    {
    }
}
