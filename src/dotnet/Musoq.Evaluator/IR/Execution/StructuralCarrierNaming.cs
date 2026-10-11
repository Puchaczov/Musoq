using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class StructuralCarrierNaming
{
    internal static string Create(int definitionIndex, IReadOnlyList<int> path)
    {
        var suffix = path.Count == 0 ? "root" : string.Join("_", path.Select(static index =>
            index < 0 ? "a" : index.ToString(CultureInfo.InvariantCulture)));
        return "__musoqStructuralCarrier_" + definitionIndex.ToString(CultureInfo.InvariantCulture) + "_" + suffix;
    }
}
