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
        ExecutionTypeRef? SourceType = null,
        ExecutionQueryRowSourceTransfer? QueryRowSourceTransfer = null,
        ExecutionTypeRef? SourceConstructionType = null,
        bool SourceConstructionSupportsContext = false,
        string? SourceConstructorStableId = null,
        ExecutionCallableRef? SourceConstructor = null,
        IReadOnlyList<ExecutionStructuralLimitPlan?>? StructuralArgumentLimits = null)
    {
        this.SchemaName = SchemaName;
        this.MethodName = MethodName;
        this.RuntimeContextId = RuntimeContextId;
        this.SchemaFromIndex = SchemaFromIndex;
        this.Arguments = ExecutionIrCollections.Freeze(Arguments);
        this.Fields = ExecutionIrCollections.Freeze(Fields);
        this.InferredColumnsMetadata = InferredColumnsMetadata;
        this.SourceType = SourceType;
        this.QueryRowSourceTransfer = QueryRowSourceTransfer;
        this.SourceConstructionType = SourceConstructionType;
        this.SourceConstructionSupportsContext = SourceConstructionSupportsContext;
        this.SourceConstructorStableId = SourceConstructorStableId;
        this.SourceConstructor = SourceConstructor;
        this.StructuralArgumentLimits = StructuralArgumentLimits == null
            ? Array.Empty<ExecutionStructuralLimitPlan?>()
            : ExecutionIrCollections.Freeze(StructuralArgumentLimits);
    }

    public string SchemaName { get; init; }

    public string MethodName { get; init; }

    public string RuntimeContextId { get; init; }

    public int SchemaFromIndex { get; init; }

    public IReadOnlyList<ExecutionExpression> Arguments { get; init; }

    public IReadOnlyList<FieldBinding> Fields { get; init; }

    public ExecutionColumnMetadata? InferredColumnsMetadata { get; init; }

    public ExecutionTypeRef? SourceType { get; init; }

    public ExecutionTypeRef? SourceConstructionType { get; init; }

    public bool SourceConstructionSupportsContext { get; init; }

    public string? SourceConstructorStableId { get; init; }

    public ExecutionCallableRef? SourceConstructor { get; init; }

    /// <summary>Receiver limits aligned with source-visible argument slots.</summary>
    public IReadOnlyList<ExecutionStructuralLimitPlan?> StructuralArgumentLimits { get; init; }

    public ExecutionQueryRowSourceTransfer? QueryRowSourceTransfer { get; init; }

    internal ExecutionSourceBinding(
        string schemaName,
        string methodName,
        string runtimeContextId,
        int schemaFromIndex,
        IReadOnlyList<ExecutionExpression> arguments,
        IReadOnlyList<FieldBinding> fields,
        ExecutionColumnMetadata? inferredColumnsMetadata,
        Type sourceType,
        ExecutionQueryRowSourceTransfer? queryRowSourceTransfer = null,
        Type? sourceConstructionType = null,
        bool sourceConstructionSupportsContext = false,
        string? sourceConstructorStableId = null,
        ExecutionCallableRef? sourceConstructor = null,
        IReadOnlyList<ExecutionStructuralLimitPlan?>? structuralArgumentLimits = null)
        : this(
            schemaName,
            methodName,
            runtimeContextId,
            schemaFromIndex,
            arguments,
            fields,
            inferredColumnsMetadata,
            ExecutionClrBindingFactory.FromClr(sourceType),
            queryRowSourceTransfer,
            sourceConstructionType == null ? null : ExecutionClrBindingFactory.FromClr(sourceConstructionType),
            sourceConstructionSupportsContext,
            sourceConstructorStableId,
            sourceConstructor,
            structuralArgumentLimits)
    {
    }
}
