using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Lowering.Tables;

internal sealed record TableCompletionRequest(
    IReadOnlyList<RowShape> Shapes,
    List<ExecutionNode> Nodes,
    ExecutionVariable ResultTable,
    GeneratedRowShape ResultShape,
    IReadOnlyList<PostOperation> PostOperations,
    bool IsDistinct = false,
    TableProjection? FinalProjection = null);
