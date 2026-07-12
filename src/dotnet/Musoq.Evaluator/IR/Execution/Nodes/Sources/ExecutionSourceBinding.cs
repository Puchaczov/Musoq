using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSourceBinding(
    string SchemaName,
    string MethodName,
    string RuntimeContextId,
    int SchemaFromIndex,
    IReadOnlyList<ExecutionExpression> Arguments,
    IReadOnlyList<FieldBinding> Fields,
    ExecutionColumnMetadata? InferredColumnsMetadata = null,
    ExecutionTypeRef? SourceType = null)
{
    internal ExecutionSourceBinding(
        string schemaName,
        string methodName,
        string runtimeContextId,
        int schemaFromIndex,
        IReadOnlyList<ExecutionExpression> arguments,
        IReadOnlyList<FieldBinding> fields,
        ExecutionColumnMetadata? inferredColumnsMetadata,
        Type sourceType)
        : this(
            schemaName,
            methodName,
            runtimeContextId,
            schemaFromIndex,
            arguments,
            fields,
            inferredColumnsMetadata,
            ExecutionTypeRef.FromClr(sourceType))
    {
    }
}
