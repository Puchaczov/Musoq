using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record ExpressionHoistPlan(
    IReadOnlyList<ExecutionLet> Lets,
    IReadOnlyDictionary<string, ExecutionVariable> VariablesBySignature);
