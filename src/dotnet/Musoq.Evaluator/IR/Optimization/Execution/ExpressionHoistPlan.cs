using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed record ExpressionHoistPlan(
    IReadOnlyList<ExecutionLet> Lets,
    IReadOnlyDictionary<string, ExecutionVariable> VariablesBySignature);

