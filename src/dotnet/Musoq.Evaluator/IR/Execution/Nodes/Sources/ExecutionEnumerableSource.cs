using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionEnumerableSource(
    ExecutionVariable Rows,
    ExecutionExpression Source,
    Type EnumerableType,
    ExecutionEnumerableChunkMode ChunkMode = ExecutionEnumerableChunkMode.ObjectOrReflected,
    string? EnumerableTypeName = null) : ExecutionNode;
