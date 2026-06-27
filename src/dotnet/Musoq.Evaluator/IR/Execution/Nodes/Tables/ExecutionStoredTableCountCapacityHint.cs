using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionStoredTableCountCapacityHint(int TableIndex) : ExecutionCapacityHint;
