using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal static class FinalProjectionSinkPlanner
{
    public static FinalProjectionSinkPlans Plan(ExecutionPlan plan, TableViaRowsResultInfo resultInfo)
    {
        var output = FinalOutputPlanner.Plan(plan, resultInfo);
        var typedOutput = output;
        if (output.IsSupported && !output.UsesGeneratedRow &&
            HasTypedLibraryWindow(plan))
        {
            typedOutput = output with { RowRepresentation = FinalOutputRowRepresentation.SelectValueShape };
        }
        return new FinalProjectionSinkPlans(
            AnalyzeDirectProjection(plan, resultInfo, FinalProjectionSinkTarget.TableRows),
            AnalyzeDirectProjection(plan, resultInfo, FinalProjectionSinkTarget.TypedRows),
            AnalyzePostOperations(plan, resultInfo, FinalProjectionSinkTarget.TypedRows),
            FinalOutputPlanner.SelectTableRows(plan, output),
            typedOutput);
    }

    private static bool HasTypedLibraryWindow(ExecutionPlan plan)
        => ExecutionIrAnalysis.CollectNodes<ExecutionWindowAggregateKernel>(plan.Body)
               .Any(static kernel => kernel.Descriptor.LibraryKernel is not null) ||
           ExecutionIrAnalysis.CollectNodes<ExecutionComputePluginWindow>(plan.Body)
               .Any(static kernel => kernel.LibraryGet is not null) ||
           ExecutionIrAnalysis.CollectNodes<ExecutionComputeRankingWindow>(plan.Body)
               .Any(static kernel => kernel.LibraryGet is not null);

    public static FinalProjectionSinkPlan AnalyzeDirectProjection(
        ExecutionPlan plan,
        TableViaRowsResultInfo resultInfo,
        FinalProjectionSinkTarget target)
    {
        return FinalProjectionDirectProjectionAnalyzer.Analyze(plan, resultInfo, target);
    }

    public static FinalProjectionSinkPlan AnalyzePostOperations(
        ExecutionPlan plan,
        TableViaRowsResultInfo resultInfo,
        FinalProjectionSinkTarget target)
    {
        return FinalProjectionPostOperationAnalyzer.Analyze(plan, resultInfo, target);
    }
}
