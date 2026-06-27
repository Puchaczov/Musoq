using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static string FormatNullOrdering(NullOrdering nullOrdering)
    {
        return nullOrdering switch
        {
            NullOrdering.First => " NULLS FIRST",
            NullOrdering.Last => " NULLS LAST",
            _ => string.Empty
        };
    }
}
