using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class RequiredColumnUsagePlanner
{
    private sealed partial class RequiredColumnUsageCollector
    {
        private void AddUnpivotExpressions(UnpivotNode unpivot)
        {
            foreach (var keepField in unpivot.KeepFields)
                AddExpression(keepField.Expression, RequiredColumnUsageReason.Projection);

            foreach (var entry in unpivot.Entries)
                AddExpression(entry.Value, RequiredColumnUsageReason.Projection);
        }
    }
}
