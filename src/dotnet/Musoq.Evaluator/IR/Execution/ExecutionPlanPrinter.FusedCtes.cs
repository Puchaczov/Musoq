using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static string FormatFusedCteOutputs(IReadOnlyList<ExecutionFusedCteOutput> outputs)
    {
        return string.Join(
            ", ",
            outputs.Select(static output => output.StoreRows
                ? $"{output.Table.Name} -> _tableResults[{output.TableIndex}]"
                : $"{output.Table.Name} -> sidecar-only"));
    }
}
