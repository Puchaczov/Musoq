using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record FinalShapeResult(
    string TableName,
    ExecutionVariable Source,
    GeneratedRowShape Shape,
    ExecutionColumnMetadata ColumnMetadata);
