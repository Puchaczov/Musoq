using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionEnumerableSource(
    ExecutionVariable Rows,
    ExecutionExpression Source,
    ExecutionTypeRef EnumerableType,
    ExecutionEnumerableChunkMode ChunkMode = ExecutionEnumerableChunkMode.ObjectOrReflected,
    string? EnumerableTypeName = null) : ExecutionNode
{
    internal ExecutionEnumerableSource(
        ExecutionVariable rows,
        ExecutionExpression source,
        Type enumerableType,
        ExecutionEnumerableChunkMode chunkMode = ExecutionEnumerableChunkMode.ObjectOrReflected,
        string? enumerableTypeName = null)
        : this(rows, source, ExecutionTypeRef.FromClr(enumerableType), chunkMode, enumerableTypeName)
    {
    }
}
