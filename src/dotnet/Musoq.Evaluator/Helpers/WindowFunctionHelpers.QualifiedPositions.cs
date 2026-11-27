using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    public static IEnumerable<TRow> QualifiedRows<TRow>(IEnumerable<TRow> rows, bool[] included)
    {
        var index = 0;
        foreach (var row in rows)
            if (included[index++])
                yield return row;
    }

    public static long[] NumberQualifiedRows(long[] sortedPositions, bool[] included)
    {
        var positions = new long[included.Length];
        var survivor = 0;
        for (var index = 0; index < positions.Length; index++)
            if (included[index])
                positions[index] = sortedPositions[survivor++];
        return positions;
    }
}
