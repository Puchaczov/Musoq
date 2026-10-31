namespace Musoq.Benchmarks.Tests;

public static partial class Expectations
{
    private static void AddComposed(Dictionary<string, ExpectedResult> results)
    {
        var id = ("Id", typeof(int));
        var total = ("Total", typeof(decimal?));
        results.Add("X01_SalesReporting", new([id, total, ("Position", typeof(long))], [[1, 2m, 2L], [2, 3m, 1L]]));
        results.Add("X02_JoinWindow", new([id, total, ("Sliding", typeof(decimal?))], [[0, 2m, 2m], [0, 2m, 2m], [1, 6m, 6m], [1, 6m, 6m], [2, 12m, 10m], [2, 12m, 10m]]));
        results.Add("X03_Reconciliation", new([("State", typeof(string)), ("Fallback", typeof(int)), ("Count", typeof(long))], [["matched", -1, 1L], ["matched", 1, 1L], ["matched", 2, 1L], ["missing", -1, 1L], ["right-only", -1, 1L]]));
        results.Add("X04_TemporalAnalytics", new([id, ("Previous", typeof(decimal?)), total], [[0, null, 1m], [1, 1m, 3m], [2, 2m, 6m], [3, 3m, 9m]]));
        results.Add("X05_CollectionPivot", new([id, ("Even", typeof(decimal?)), ("Odd", typeof(decimal?))], [[0, 1m, 1m], [1, 2m, 2m], [2, 3m, 3m], [3, 4m, 4m]]));
        results.Add("X06_Consolidation", new([id], [[1], [2]]));
        results.Add("X07_LogAnalytics", new([("Level", typeof(string)), total, ("Position", typeof(long))], [["INFO", 4m, 1L]]));
        results.Add("X08_PacketAnalytics", new([("Level", typeof(string)), ("Total", typeof(long?)), ("Count", typeof(long))], [["INFO", 12L, 6L]]));
        results.Add("X09_RecursiveChunks", new([id, ("Consumed", typeof(int)), ("Error", typeof(string))], [[0, 0, null], [0, 8, null], [8, 8, null], [16, 8, null], [24, 8, null], [32, 1, "Value"]]));
        results.Add("X10_StructuredPipeline", new([id, total], [[0, 1m], [1, 3m], [2, 6m]]));
    }
}
