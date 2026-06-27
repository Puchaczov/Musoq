using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionConstantCapacityHint(int Capacity) : ExecutionCapacityHint;
