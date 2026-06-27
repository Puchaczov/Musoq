using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using ExecutionStrategyPlan = Musoq.Evaluator.IR.Planning.ExecutionStrategyPlan;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    internal PhysicalToExecutionPlanBuilder(
        ExecutionShapeResolver shapeResolver,
        ExecutionPlanningArtifacts executionArtifacts,
        SchemaRegistry? schemaRegistry = null,
        CompilationOptions? compilationOptions = null,
        CteExecutionPlan? cteExecutionPlan = null)
        : this(shapeResolver, schemaRegistry, compilationOptions, cteExecutionPlan, executionArtifacts)
    {
    }

    internal PhysicalToExecutionPlanBuilder(
        ExecutionShapeResolver shapeResolver,
        SchemaRegistry? schemaRegistry,
        CompilationOptions? compilationOptions,
        CteExecutionPlan? cteExecutionPlan,
        ExecutionPlanningArtifacts executionArtifacts)
    {
        _shapeResolver = shapeResolver ?? throw new ArgumentNullException(nameof(shapeResolver));
        _schemaRegistry = schemaRegistry;
        _compilationOptions = compilationOptions ?? new CompilationOptions();
        _cteExecutionPlan = cteExecutionPlan;
        _executionPlanningArtifacts = executionArtifacts ?? throw new ArgumentNullException(nameof(executionArtifacts));
        _sourceInteractionPlans = executionArtifacts.SourceInteractionPlansBySourceId ??
            new Dictionary<string, SourceInteractionPlan>(StringComparer.Ordinal);
    }

    private ExecutionStrategyPlan ResolveExecutionStrategies()
    {
        return _executionPlanningArtifacts.ExecutionStrategies;
    }
}
