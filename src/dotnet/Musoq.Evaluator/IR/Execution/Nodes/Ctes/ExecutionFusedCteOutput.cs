using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionFusedCteOutput(
    int TableIndex,
    ExecutionVariable Table,
    GeneratedRowShape RowShape,
    bool StoreRows = true);
