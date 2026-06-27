using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionColumnMetadata(
    string ReferenceName,
    IReadOnlyList<ExecutionColumnMetadataField> Fields,
    ExecutionColumnMetadataKind Kind);
