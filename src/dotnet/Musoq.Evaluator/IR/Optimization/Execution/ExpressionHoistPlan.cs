using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed record ExpressionHoistPlan(
    IReadOnlyList<ExecutionLet> Lets,
    IReadOnlyDictionary<string, ExecutionVariable> VariablesBySignature);

