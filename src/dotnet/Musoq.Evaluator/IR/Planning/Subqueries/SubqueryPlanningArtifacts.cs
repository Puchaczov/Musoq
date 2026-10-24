using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

namespace Musoq.Evaluator.IR.Planning.Subqueries;

internal sealed record SubqueryPlanningArtifacts(
    PlanningResult Result,
    IPlanningShapeResolver Shapes,
    CompilationOptions Options,
    SchemaRegistry? Schemas,
    CteExecutionPlan? Ctes);
