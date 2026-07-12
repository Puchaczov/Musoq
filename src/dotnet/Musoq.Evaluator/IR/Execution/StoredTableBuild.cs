using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record StoredTableBuild(
    int TableIndex,
    IReadOnlyList<ExecutionNode> Nodes,
    ExecutionVariable Table,
    IReadOnlyList<CapturedLocal> Captures);
