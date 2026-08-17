using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class CteStrategyPlanner
{
    private static bool IsReadOnceCtePipelineDeterministic(SupportedPipeline pipeline)
    {
        return AreProjectedFieldsDeterministic(pipeline.Project) &&
               IsFilterDeterministic(pipeline.Filter);
    }

    private static bool AreProjectedFieldsDeterministic(PhysicalProjectNode project)
    {
        foreach (var field in project.Fields)
        {
            if (!IrExpressionDeterminism.IsDeterministic(field.Expression))
                return false;
        }

        return true;
    }

    private static bool IsFilterDeterministic(PhysicalFilterNode? filter)
    {
        return filter == null || IrExpressionDeterminism.IsDeterministic(filter.Predicate);
    }
}
