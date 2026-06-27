using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;

namespace Musoq.Evaluator.IR.Physical.SourcePlanning;

internal sealed record SourcePlanPhysicalRewriteResult(
    PhysicalNode PhysicalPlan,
    IReadOnlyDictionary<string, SourcePlanResult> SourcePlanResultsBySourceId);
