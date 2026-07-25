using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

namespace Musoq.Evaluator.IR.Execution.Lowering;

/// <summary>
/// Composition boundary for physical-to-execution lowering.  Construction of
/// facts, scopes, domain services, and dispatch is kept outside the public
/// compatibility builder.
/// </summary>
internal sealed class PhysicalLoweringFacade
{
    private readonly PhysicalLoweringImplementation _implementation;

    internal PhysicalLoweringFacade(
        ExecutionShapeResolver shapeResolver,
        ExecutionPlanningArtifacts executionArtifacts,
        SchemaRegistry? schemaRegistry = null,
        CompilationOptions? compilationOptions = null,
        CteExecutionPlan? cteExecutionPlan = null)
    {
        _implementation = new PhysicalLoweringImplementation(
            shapeResolver,
            executionArtifacts,
            schemaRegistry,
            compilationOptions,
            cteExecutionPlan);
    }

    internal PhysicalLoweringFacade(
        ExecutionShapeResolver shapeResolver,
        SchemaRegistry? schemaRegistry,
        CompilationOptions? compilationOptions,
        CteExecutionPlan? cteExecutionPlan,
        ExecutionPlanningArtifacts executionArtifacts)
    {
        _implementation = new PhysicalLoweringImplementation(
            shapeResolver,
            schemaRegistry,
            compilationOptions,
            cteExecutionPlan,
            executionArtifacts);
    }

    internal ExecutionPlanBuildResult Build(PhysicalNode physicalPlan, string identifier) =>
        _implementation.Build(physicalPlan, identifier);
}
