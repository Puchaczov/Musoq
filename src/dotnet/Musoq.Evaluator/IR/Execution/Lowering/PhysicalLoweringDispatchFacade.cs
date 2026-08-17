using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution.Lowering;

internal sealed record PhysicalLoweringHandlers(
    Func<PhysicalToExecutionLoweringContext, LoweringAttempt<ExecutionPlan>> TryBuildMultiStatementPlan,
    Func<PhysicalToExecutionLoweringContext, LoweringAttempt<ExecutionPlan>> TryBuildCtePlan,
    Func<PhysicalToExecutionLoweringContext, LoweringAttempt<ExecutionPlan>> TryBuildDescPlan,
    Func<PhysicalToExecutionLoweringContext, LoweringAttempt<ExecutionPlan>> TryBuildSetOperationPlan,
    Func<PhysicalToExecutionLoweringContext, LoweringAttempt<ExecutionPlan>> TryBuildAggregatePlan,
    Func<PhysicalToExecutionLoweringContext, LoweringAttempt<ExecutionPlan>> TryBuildWindowPlan,
    Func<PhysicalToExecutionLoweringContext, LoweringAttempt<ExecutionPlan>> TryBuildPipelinePlan,
    Func<PhysicalToExecutionTableLoweringContext, LoweringAttempt<LoweredTable>> TryBuildMultiStatementTable,
    Func<PhysicalToExecutionTableLoweringContext, LoweringAttempt<LoweredTable>> TryBuildSetOperationTable,
    Func<PhysicalToExecutionTableLoweringContext, LoweringAttempt<LoweredTable>> TryBuildWindowTable,
    Func<PhysicalToExecutionTableLoweringContext, LoweringAttempt<LoweredTable>> TryBuildPipelineTable,
    Func<PhysicalToExecutionTableLoweringContext, LoweringAttempt<LoweredTable>> TryBuildAggregateTable);

internal sealed class PhysicalLoweringDispatchFacade
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

    private readonly PhysicalLoweringRegistry _registry;

    public PhysicalLoweringDispatchFacade(PhysicalLoweringHandlers handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        _registry = new PhysicalLoweringRegistry(
            CreatePlanLoweringDescriptors(handlers),
            CreateTableLoweringDescriptors(handlers));
    }

    public LoweringAttempt<ExecutionPlan> TryBuildPlan(
        PhysicalToExecutionLoweringContext context) =>
        _registry.TryBuildPlan(context);

    public LoweringAttempt<LoweredTable> TryBuildTable(
        PhysicalToExecutionTableLoweringContext context) =>
        _registry.TryBuildTable(context);

    public LoweringScope CreateScope(PhysicalLoweringFacts facts) =>
        new(facts);

    public ExecutionPlanBuildResult BuildPlan(
        PhysicalToExecutionLoweringContext context,
        Func<PhysicalNode, ExecutionPlanBuildResult> createUnsupported)
    {
        ArgumentNullException.ThrowIfNull(createUnsupported);
        var attempt = TryBuildPlan(context);
        return attempt.Kind switch
        {
            LoweringAttemptKind.Built => ExecutionPlanBuildResult.CreateSupported(attempt.RequireValue()),
            LoweringAttemptKind.Unsupported => ExecutionPlanBuildResult.CreateUnsupported(
                attempt.RequireUnsupportedReason()),
            _ => createUnsupported(context.Plan)
        };
    }

    public TableBuildResult BuildTable(
        PhysicalToExecutionTableLoweringContext context,
        Func<PhysicalNode, TableBuildResult> createUnsupported)
    {
        ArgumentNullException.ThrowIfNull(createUnsupported);
        var attempt = TryBuildTable(context);
        return attempt.Kind switch
        {
            LoweringAttemptKind.Built => attempt.RequireValue().ToCompatibilityResult(),
            LoweringAttemptKind.Unsupported => TableBuildResult.Unsupported(
                attempt.RequireUnsupportedReason()),
            _ => createUnsupported(context.Plan)
        };
    }

    private static IReadOnlyList<PhysicalPlanLoweringDescriptor> CreatePlanLoweringDescriptors(
        PhysicalLoweringHandlers handlers) =>
    [
        new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[0], handlers.TryBuildMultiStatementPlan),
        new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[1], handlers.TryBuildCtePlan),
        new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[2], handlers.TryBuildDescPlan),
        new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[3], handlers.TryBuildSetOperationPlan),
        new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[4], handlers.TryBuildAggregatePlan),
        new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[5], handlers.TryBuildWindowPlan),
        new PhysicalPlanLoweringDescriptor(PlanLoweringDescriptorNames[6], handlers.TryBuildPipelinePlan)
    ];

    private static IReadOnlyList<PhysicalTableLoweringDescriptor> CreateTableLoweringDescriptors(
        PhysicalLoweringHandlers handlers) =>
    [
        new PhysicalTableLoweringDescriptor(TableLoweringDescriptorNames[0], handlers.TryBuildMultiStatementTable),
        new PhysicalTableLoweringDescriptor(TableLoweringDescriptorNames[1], handlers.TryBuildSetOperationTable),
        new PhysicalTableLoweringDescriptor(TableLoweringDescriptorNames[2], handlers.TryBuildWindowTable),
        new PhysicalTableLoweringDescriptor(TableLoweringDescriptorNames[3], handlers.TryBuildPipelineTable),
        new PhysicalTableLoweringDescriptor(TableLoweringDescriptorNames[4], handlers.TryBuildAggregateTable)
    ];
}
