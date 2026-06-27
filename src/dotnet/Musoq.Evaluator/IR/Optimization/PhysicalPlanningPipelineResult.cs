using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Planning.Cardinality;
using PhysicalPlanBuilder = Musoq.Evaluator.IR.Physical.PhysicalPlanBuilder;
using PlanningContext = Musoq.Evaluator.IR.Planning.PlanningContext;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record PhysicalPlanningPipelineResult(PhysicalPlanningArtifacts Artifacts);
