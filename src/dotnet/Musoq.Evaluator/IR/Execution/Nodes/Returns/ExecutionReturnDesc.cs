using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionReturnDesc(
    string SchemaName,
    string MethodName,
    DescType Type,
    string? Column,
    IReadOnlyList<ExecutionExpression> Arguments,
    string RuntimeContextId,
    int SchemaFromIndex,
    ExecutionColumnMetadata? QueryColumnMetadata = null) : ExecutionNode;
