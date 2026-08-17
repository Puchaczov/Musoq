using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record LogicalPlanningArtifacts(LogicalNode InitialLogicalPlan, LogicalNode OptimizedLogicalPlan, OptimizationTrace OptimizerTrace);
