using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class BoundaryRowShapePlanner
{
    private static void AddUnpivotColumns(PhysicalUnpivotNode unpivot, List<string> columns)
    {
        columns.AddRange(CollectColumns(unpivot.KeepFields.Select(static field => field.Expression)));
        columns.AddRange(CollectColumns(unpivot.Entries.Select(static entry => entry.Value)));
    }
}
