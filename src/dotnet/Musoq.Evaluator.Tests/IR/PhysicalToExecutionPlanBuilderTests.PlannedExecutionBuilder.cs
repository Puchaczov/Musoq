using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using ExecutionStrategyPlan = Musoq.Evaluator.IR.Planning.ExecutionStrategyPlan;
using PhysicalToExecutionPlanBuilder = Musoq.Evaluator.IR.Execution.PhysicalToExecutionPlanBuilder;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    private sealed class PlannedExecutionBuilder(
        ExecutionShapeResolver shapeResolver,
        CompilationOptions? compilationOptions = null,
        CteExecutionPlan? cteExecutionPlan = null,
        ExecutionStrategyPlan? fixedExecutionStrategies = null)
    {
        private readonly CompilationOptions _compilationOptions = compilationOptions ?? new CompilationOptions();

        public ExecutionPlanBuildResult Build(PhysicalNode physicalPlan, string identifier = "compiled")
        {
            var planningShapeResolver = new ExecutionPlanningShapeResolverAdapter(shapeResolver);
            var executionStrategies = fixedExecutionStrategies ??
                ExecutionStrategyPlanner.Plan(physicalPlan, _compilationOptions, cteExecutionPlan, planningShapeResolver).Strategies;
            var executionArtifacts = new ExecutionPlanningArtifacts(
                executionStrategies,
                new Dictionary<string, SourceInteractionPlan>(StringComparer.Ordinal),
                []);
            var builder = new PhysicalToExecutionPlanBuilder(
                shapeResolver,
                null,
                _compilationOptions,
                cteExecutionPlan,
                executionArtifacts);

            return builder.Build(physicalPlan, identifier);
        }
    }
}
