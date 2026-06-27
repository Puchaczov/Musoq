using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionTryGetNonEnumeratedCountCapacityHint(
    ExecutionVariable Collection,
    string CountVariableName) : ExecutionCapacityHint;
