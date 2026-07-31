using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record FinalShapeYieldSink(
    string TableName,
    string ShapeTypeName,
    IReadOnlyList<FieldBinding> Fields,
    string? BufferName = null,
    IReadOnlyDictionary<string, FinalShapeSourceBuffer>? SourceBuffers = null,
    bool UsesGeneratedRowCarrier = false);
