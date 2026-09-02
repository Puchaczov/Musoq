using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Parser;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionReturnDesc : ExecutionNode
{
    public ExecutionReturnDesc(
        string schemaName,
        string methodName,
        DescType type,
        string? column,
        IReadOnlyList<ExecutionExpression> arguments,
        string runtimeContextId,
        int schemaFromIndex,
        ExecutionColumnMetadata? queryColumnMetadata = null,
        TextSpan? columnSpan = null)
    {
        SchemaName = schemaName;
        MethodName = methodName;
        Type = type;
        Column = column;
        Arguments = ExecutionIrCollections.Freeze(arguments);
        RuntimeContextId = runtimeContextId;
        SchemaFromIndex = schemaFromIndex;
        QueryColumnMetadata = queryColumnMetadata;
        ColumnSpan = columnSpan;
    }

    public string SchemaName { get; init; }
    public string MethodName { get; init; }
    public DescType Type { get; init; }
    public string? Column { get; init; }
    public IReadOnlyList<ExecutionExpression> Arguments { get; init; }
    public string RuntimeContextId { get; init; }
    public int SchemaFromIndex { get; init; }
    public ExecutionColumnMetadata? QueryColumnMetadata { get; init; }
    public TextSpan? ColumnSpan { get; init; }
}
