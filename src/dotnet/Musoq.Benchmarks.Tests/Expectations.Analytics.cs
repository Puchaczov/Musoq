namespace Musoq.Benchmarks.Tests;

public static partial class Expectations
{
    private static void AddAnalytics(Dictionary<string, ExpectedResult> results)
    {
        var id = ("Id", typeof(int));
        var total = ("Total", typeof(decimal?));
        results.Add("B13_Apply", new([id, ("Value", typeof(int)), ("Position", typeof(int))], [[0, 0, 0], [0, 1, 1], [1, 1, 0], [1, 2, 1], [2, 2, 0], [2, 3, 1], [3, 3, 0], [3, 4, 1]]));
        results.Add("B13_OuterApply", new([id, ("Match", typeof(int?))], [[0, null], [1, null], [2, null], [3, null]]));
        results.Add("B14_Group", new([id, total, ("Present", typeof(long))], [[0, 1m, 0L], [1, 2m, 0L], [2, 3m, 1L], [3, 4m, 0L]]));
        results.Add("B14_WholeAggregate", new([("Count", typeof(long)), total, ("Average", typeof(decimal?))], [[4L, 10m, 2.5m]]));
        results.Add("B15_RankingOffset", new([id, ("Position", typeof(long)), ("Previous", typeof(decimal?))], [[0, 1L, null], [1, 2L, 1m], [2, 3L, 2m], [3, 4L, 3m]]));
        results.Add("B16_RowsFrame", new([id, total, ("First", typeof(decimal?))], [[0, 1m, 1m], [1, 3m, 1m], [2, 5m, 2m], [3, 7m, 3m]]));
        results.Add("B16_RangeFrame", new([id, total], [[0, 1m], [1, 3m], [2, 5m], [3, 7m]]));
        results.Add("B17_MultipleWindows", new([id, ("Position", typeof(long)), total], [[0, 1L, 1m], [1, 1L, 3m], [2, 1L, 6m], [3, 1L, 10m]]));
        results.Add("B18_Pivot", new([("K0", typeof(decimal?)), ("K1", typeof(decimal?)), ("K2", typeof(decimal?)), ("K3", typeof(decimal?))], [[1m, 2m, 3m, 4m]]));
        results.Add("B18_Unpivot", new([id, ("Label", typeof(string)), ("Value", typeof(int))], [[0, "Grouped", 0], [0, "Original", 0], [1, "Grouped", 1], [1, "Original", 1], [2, "Grouped", 2], [2, "Original", 2], [3, "Grouped", 3], [3, "Original", 3]]));
        results.Add("B19_KeyedUnion", new([id, ("Name", typeof(string))], [[0, "item-0"], [1, "item-1"], [2, "item-2"], [3, "item-3"]]));
        results.Add("B19_Except", new([id], [[3]]));
        results.Add("B19_Intersect", new([id], [[0], [1], [2]]));
        results.Add("B20_TopOffset", new([id, ("Name", typeof(string))], [[2, "item-2"], [1, "item-1"]]));
        results.Add("B21_CteReuse", new([id, total], [[0, 2m], [1, 4m], [2, 6m], [3, 8m]]));
        results.Add("B22_Recursive", new([id], [[0], [1], [2], [3]]));
    }
}
