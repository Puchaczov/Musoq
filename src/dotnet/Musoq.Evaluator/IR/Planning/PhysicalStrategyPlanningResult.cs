using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PhysicalStrategyPlanningResult(
    PhysicalStrategyPlan Strategies,
    IReadOnlyList<PlanningDecision> Decisions);
