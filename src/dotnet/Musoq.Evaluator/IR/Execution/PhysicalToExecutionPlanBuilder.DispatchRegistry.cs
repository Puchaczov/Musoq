using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    internal static readonly IReadOnlyList<string> PlanLoweringDescriptorNames =
    [
        "multi-statement",
        "cte",
        "desc",
        "set-operation",
        "aggregate",
        "window",
        "pipeline"
    ];

    internal static readonly IReadOnlyList<string> TableLoweringDescriptorNames =
    [
        "multi-statement-table",
        "set-operation-table",
        "window-table",
        "pipeline-table",
        "aggregate-table"
    ];

    private PhysicalLoweringRegistry CreatePhysicalLoweringRegistry()
    {
        return new PhysicalLoweringRegistry(CreatePlanLoweringDescriptors(), CreateTableLoweringDescriptors());
    }

    private IReadOnlyList<PhysicalPlanLoweringDescriptor> CreatePlanLoweringDescriptors()
    {
        return
        [
            new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[0], TryBuildMultiStatementPlan),
            new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[1], TryBuildCtePlan),
            new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[2], TryBuildDescPlan),
            new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[3], TryBuildSetOperationPlan),
            new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[4], TryBuildAggregatePlan),
            new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[5], TryBuildWindowPlan),
            new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[6], TryBuildPipelinePlan)
        ];
    }

    private IReadOnlyList<PhysicalTableLoweringDescriptor> CreateTableLoweringDescriptors()
    {
        return
        [
            new PhysicalTableLoweringDescriptor(TableLoweringDescriptorNames[0], TryBuildMultiStatementTable),
            new PhysicalTableLoweringDescriptor(TableLoweringDescriptorNames[1], TryBuildSetOperationTable),
            new PhysicalTableLoweringDescriptor(TableLoweringDescriptorNames[2], TryBuildWindowTable),
            new PhysicalTableLoweringDescriptor(TableLoweringDescriptorNames[3], TryBuildPipelineTable),
            new PhysicalTableLoweringDescriptor(TableLoweringDescriptorNames[4], TryBuildAggregateTable)
        ];
    }

    private ExecutionPlanBuildResult? TryBuildMultiStatementPlan(PhysicalToExecutionLoweringContext context)
    {
        return context.Plan is PhysicalMultiStatementNode multiStatement
            ? BuildMultiStatement(multiStatement, context.Identifier, context.Session)
            : null;
    }

    private ExecutionPlanBuildResult? TryBuildCtePlan(PhysicalToExecutionLoweringContext context)
    {
        return new CteLoweringCoordinator(BuildCte).TryBuild(context, out var result)
            ? result
            : null;
    }

    private ExecutionPlanBuildResult? TryBuildDescPlan(PhysicalToExecutionLoweringContext context)
    {
        return context.Plan is PhysicalDescNode desc
            ? BuildDesc(desc, context.Identifier)
            : null;
    }

    private ExecutionPlanBuildResult? TryBuildSetOperationPlan(PhysicalToExecutionLoweringContext context)
    {
        var setOperationPipeline = DecomposeSetOperationPipeline(context.Plan);
        return setOperationPipeline != null
            ? BuildSetOperation(setOperationPipeline, context.Identifier, context.Session)
            : null;
    }

    private ExecutionPlanBuildResult? TryBuildAggregatePlan(PhysicalToExecutionLoweringContext context)
    {
        return CreateAggregateLoweringCoordinator().TryBuildPlan(context, out var result)
            ? result
            : null;
    }

    private ExecutionPlanBuildResult? TryBuildWindowPlan(PhysicalToExecutionLoweringContext context)
    {
        return CreateWindowLoweringCoordinator().TryBuildPlan(context, out var result)
            ? result
            : null;
    }

    private ExecutionPlanBuildResult? TryBuildPipelinePlan(PhysicalToExecutionLoweringContext context)
    {
        var pipeline = DecomposeSupportedPipeline(context.Plan);
        return pipeline != null
            ? BuildPipeline(pipeline, context.Identifier, context.Session)
            : null;
    }

    private TableBuildResult? TryBuildMultiStatementTable(PhysicalToExecutionTableLoweringContext context)
    {
        if (context.Plan is not PhysicalMultiStatementNode multiStatement ||
            !CanBuildTableProducingMultiStatement(multiStatement))
        {
            return null;
        }

        var multiStatementIndexes = CreateMultiStatementIndexes(
            multiStatement,
            context.CteIndexes,
            context.CteShapesByName,
            ResolveStatementNamePrefix(context.ResultTableName));

        return BuildMultiStatementTable(
            multiStatement,
            context.ResultTableName,
            context.ResultShapeName,
            multiStatementIndexes,
            context.ScopeAggregateVariables,
            context.Session);
    }

    private TableBuildResult? TryBuildSetOperationTable(PhysicalToExecutionTableLoweringContext context)
    {
        var setOperationPipeline = DecomposeSetOperationPipeline(context.Plan);
        return setOperationPipeline != null
            ? BuildSetOperationTable(
                setOperationPipeline,
                context.ResultTableName,
                context.ResultShapeName,
                context.CteIndexes,
                context.CteShapesByName,
                context.SchemaFromIndex,
                context.Session)
            : null;
    }

    private TableBuildResult? TryBuildWindowTable(PhysicalToExecutionTableLoweringContext context)
    {
        return CreateWindowLoweringCoordinator().TryBuildTable(context, out var result)
            ? result
            : null;
    }

    private TableBuildResult? TryBuildPipelineTable(PhysicalToExecutionTableLoweringContext context)
    {
        var pipeline = DecomposeSupportedPipeline(context.Plan);
        return pipeline != null
            ? BuildTable(
                pipeline,
                context.ResultTableName,
                context.ResultShapeName,
                context.CteIndexes,
                context.CteShapesByName,
                context.SchemaFromIndex,
                context.Session)
            : null;
    }

    private TableBuildResult? TryBuildAggregateTable(PhysicalToExecutionTableLoweringContext context)
    {
        return CreateAggregateLoweringCoordinator().TryBuildTable(context, out var result)
            ? result
            : null;
    }
}
