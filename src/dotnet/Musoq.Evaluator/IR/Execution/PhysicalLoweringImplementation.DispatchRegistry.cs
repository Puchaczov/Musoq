using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    internal static IReadOnlyList<string> PlanLoweringDescriptorNames =>
        PhysicalLoweringDispatchFacade.PlanLoweringDescriptorNames;

    internal static IReadOnlyList<string> TableLoweringDescriptorNames =>
        PhysicalLoweringDispatchFacade.TableLoweringDescriptorNames;

    private PhysicalLoweringDispatchFacade CreatePhysicalLoweringDispatchFacade() =>
        new(new PhysicalLoweringHandlers(
            TryBuildMultiStatementPlan,
            TryBuildCtePlan,
            TryBuildDescPlan,
            TryBuildSetOperationPlan,
            TryBuildAggregatePlan,
            TryBuildWindowPlan,
            TryBuildPipelinePlan,
            TryBuildMultiStatementTable,
            TryBuildSetOperationTable,
            TryBuildWindowTable,
            TryBuildPipelineTable,
            TryBuildAggregateTable));

    private AggregatePlanLowerer CreateAggregatePlanLowerer()
    {
        return new AggregatePlanLowerer(new AggregateLoweringService(this));
    }

    private WindowPlanLowerer CreateWindowPlanLowerer()
    {
        return new WindowPlanLowerer(new WindowLoweringService(this));
    }

    private JoinPlanLowerer CreateJoinPlanLowerer() => new(new JoinLoweringService(this));

    private CtePlanLowerer CreateCtePlanLowerer() => new(new CteLoweringService(this));

    private PipelinePlanLowerer CreatePipelinePlanLowerer()
        => new(new PipelineLoweringService(this));

    private LoweringAttempt<ExecutionPlan> TryBuildMultiStatementPlan(PhysicalToExecutionLoweringContext context)
    {
        return _multiStatementPlanLowerer.TryBuildPlan(context);
    }

    private LoweringAttempt<ExecutionPlan> TryBuildCtePlan(PhysicalToExecutionLoweringContext context)
    {
        return _ctePlanLowerer.TryBuild(context);
    }

    private LoweringAttempt<ExecutionPlan> TryBuildDescPlan(PhysicalToExecutionLoweringContext context)
    {
        return _descPlanLowerer.TryBuild(context);
    }

    private LoweringAttempt<ExecutionPlan> TryBuildSetOperationPlan(PhysicalToExecutionLoweringContext context)
    {
        return _pipelinePlanLowerer.TryBuildSetOperationPlan(context);
    }

    private LoweringAttempt<ExecutionPlan> TryBuildAggregatePlan(PhysicalToExecutionLoweringContext context)
    {
        return _aggregatePlanLowerer.TryBuildPlan(context);
    }

    private LoweringAttempt<ExecutionPlan> TryBuildWindowPlan(PhysicalToExecutionLoweringContext context)
    {
        return _windowPlanLowerer.TryBuildPlan(context);
    }

    private LoweringAttempt<ExecutionPlan> TryBuildPipelinePlan(PhysicalToExecutionLoweringContext context)
    {
        return _pipelinePlanLowerer.TryBuildPipelinePlan(context);
    }

    private LoweringAttempt<LoweredTable> TryBuildMultiStatementTable(PhysicalToExecutionTableLoweringContext context)
    {
        return _multiStatementPlanLowerer.TryBuildTable(context);
    }

    private LoweringAttempt<LoweredTable> TryBuildSetOperationTable(PhysicalToExecutionTableLoweringContext context)
    {
        return _pipelinePlanLowerer.TryBuildSetOperationTable(context);
    }

    private LoweringAttempt<LoweredTable> TryBuildWindowTable(PhysicalToExecutionTableLoweringContext context)
    {
        return _windowPlanLowerer.TryBuildTable(context);
    }

    private LoweringAttempt<LoweredTable> TryBuildPipelineTable(PhysicalToExecutionTableLoweringContext context)
    {
        return _pipelinePlanLowerer.TryBuildPipelineTable(context);
    }

    private LoweringAttempt<LoweredTable> TryBuildAggregateTable(PhysicalToExecutionTableLoweringContext context)
    {
        return _aggregatePlanLowerer.TryBuildTable(context);
    }
}
