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
    Type? SourceType = null);
