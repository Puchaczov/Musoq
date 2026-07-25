using System;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Execution.Lowering;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

namespace Musoq.Evaluator.IR.Execution;

public sealed class PhysicalToExecutionPlanBuilder
{
    private readonly PhysicalLoweringFacade _physicalLoweringFacade;

    internal PhysicalToExecutionPlanBuilder(
        ExecutionShapeResolver shapeResolver,
        ExecutionPlanningArtifacts executionArtifacts,
        SchemaRegistry? schemaRegistry = null,
        CompilationOptions? compilationOptions = null,
        CteExecutionPlan? cteExecutionPlan = null)
    {
        _physicalLoweringFacade = new PhysicalLoweringFacade(
            shapeResolver,
            executionArtifacts,
            schemaRegistry,
            compilationOptions,
            cteExecutionPlan);
    }

    internal PhysicalToExecutionPlanBuilder(
        ExecutionShapeResolver shapeResolver,
        SchemaRegistry? schemaRegistry,
        CompilationOptions? compilationOptions,
        CteExecutionPlan? cteExecutionPlan,
        ExecutionPlanningArtifacts executionArtifacts)
    {
        _physicalLoweringFacade = new PhysicalLoweringFacade(
            shapeResolver,
            schemaRegistry,
            compilationOptions,
            cteExecutionPlan,
            executionArtifacts);
    }

    public ExecutionPlanBuildResult Build(PhysicalNode physicalPlan, string identifier = "compiled") =>
        _physicalLoweringFacade.Build(physicalPlan, identifier);
}
