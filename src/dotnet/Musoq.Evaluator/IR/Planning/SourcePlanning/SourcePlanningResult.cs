using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Planning.OptimizationDiagnostics;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal sealed record SourcePlanningResult(
    IReadOnlyDictionary<string, SourcePlanRequest> RequestsBySourceId,
    IReadOnlyDictionary<string, SourcePlanResult> ResultsBySourceId,
    IReadOnlyList<PlanningDecision> Decisions);
