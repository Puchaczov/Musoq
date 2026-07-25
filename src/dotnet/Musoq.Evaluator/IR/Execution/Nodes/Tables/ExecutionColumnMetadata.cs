using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionColumnMetadata
{
    public ExecutionColumnMetadata(
        string referenceName,
        IReadOnlyList<ExecutionColumnMetadataField> fields,
        ExecutionColumnMetadataKind kind)
    {
        ReferenceName = referenceName;
        Fields = ExecutionIrCollections.Freeze(fields);
        Kind = kind;
    }

    public string ReferenceName { get; init; }

    public IReadOnlyList<ExecutionColumnMetadataField> Fields { get; init; }

    public ExecutionColumnMetadataKind Kind { get; init; }
}
