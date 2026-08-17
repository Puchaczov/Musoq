using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSourceBinding
{
    public ExecutionSourceBinding(
        string SchemaName,
        string MethodName,
        string RuntimeContextId,
        int SchemaFromIndex,
        IReadOnlyList<ExecutionExpression> Arguments,
        IReadOnlyList<FieldBinding> Fields,
        ExecutionColumnMetadata? InferredColumnsMetadata = null,
        ExecutionTypeRef? SourceType = null)
    {
        this.SchemaName = SchemaName;
        this.MethodName = MethodName;
        this.RuntimeContextId = RuntimeContextId;
        this.SchemaFromIndex = SchemaFromIndex;
        this.Arguments = ExecutionIrCollections.Freeze(Arguments);
        this.Fields = ExecutionIrCollections.Freeze(Fields);
        this.InferredColumnsMetadata = InferredColumnsMetadata;
        this.SourceType = SourceType;
    }

    public string SchemaName { get; init; }

    public string MethodName { get; init; }

    public string RuntimeContextId { get; init; }

    public int SchemaFromIndex { get; init; }

    public IReadOnlyList<ExecutionExpression> Arguments { get; init; }

    public IReadOnlyList<FieldBinding> Fields { get; init; }

    public ExecutionColumnMetadata? InferredColumnsMetadata { get; init; }

    public ExecutionTypeRef? SourceType { get; init; }

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
            ExecutionClrBindingFactory.FromClr(sourceType))
    {
    }
}
