using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowKernelPlan(
    string Signature,
    ExecutionWindowKernelPlanStrategy Strategy,
    IReadOnlyList<ExecutionNode> Kernels) : ExecutionNode;
