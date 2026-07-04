using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed record ExpressionHoistPlan(
    IReadOnlyList<ExecutionLet> Lets,
    IReadOnlyDictionary<string, ExecutionVariable> VariablesBySignature);

