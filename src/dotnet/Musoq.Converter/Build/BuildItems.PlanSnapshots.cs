using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Converter.Build;

public partial class BuildItems
{
    public LogicalNode? InitialLogicalPlan
    {
        get => GetOptional<LogicalNode>(BuildItemKeys.InitialLogicalPlan);
        set => SetOptional(BuildItemKeys.InitialLogicalPlan, value);
    }

    public LogicalNode? OptimizedLogicalPlan
    {
        get => GetOptional<LogicalNode>(BuildItemKeys.OptimizedLogicalPlan);
        set => SetOptional(BuildItemKeys.OptimizedLogicalPlan, value);
    }

    public PhysicalNode? InitialPhysicalPlan
    {
        get => GetOptional<PhysicalNode>(BuildItemKeys.InitialPhysicalPlan);
        set => SetOptional(BuildItemKeys.InitialPhysicalPlan, value);
    }

    public PhysicalNode? OptimizedPhysicalPlan
    {
        get => GetOptional<PhysicalNode>(BuildItemKeys.OptimizedPhysicalPlan);
        set => SetOptional(BuildItemKeys.OptimizedPhysicalPlan, value);
    }

    public ExecutionPlan? InitialExecutionPlan
    {
        get => GetOptional<ExecutionPlan>(BuildItemKeys.InitialExecutionPlan);
        set => SetOptional(BuildItemKeys.InitialExecutionPlan, value);
    }

    public ExecutionPlan? OptimizedExecutionPlan
    {
        get => GetOptional<ExecutionPlan>(BuildItemKeys.OptimizedExecutionPlan);
        set => SetOptional(BuildItemKeys.OptimizedExecutionPlan, value);
    }

    public string? OptimizerTraceText
    {
        get => GetOptional<string>(BuildItemKeys.OptimizerTraceText);
        set => SetOptional(BuildItemKeys.OptimizerTraceText, value);
    }
}
