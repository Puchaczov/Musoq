namespace Musoq.Benchmarks.Tests;

public static partial class Expectations
{
    private static void AddRelational(Dictionary<string, ExpectedResult> results)
    {
        var id = ("Id", typeof(int));
        var match = ("Match", typeof(int));
        var nullableMatch = ("Match", typeof(int?));
        results.Add("B02_Scalar", new([id, ("Value", typeof(long?))], [[0, 2L], [1, 5L], [2, 8L], [3, 11L]]));
        results.Add("B03_Guards", new([id, ("Value", typeof(int)), ("Fallback", typeof(int))], [[0, 0, -2], [1, -1, 1], [2, 4, 2], [3, -1, -2]]));
        results.Add("B04_Patterns", new([id], [[0], [2]]));
        results.Add("B05_Membership", new([id], [[0], [2]]));
        results.Add("B06_Distinct", new([("Value", typeof(int?))], [[null], [1], [2]]));
        results.Add("B07_EqualityJoin", new([id, match], [[0, 0], [1, 1], [2, 2], [3, 3]]));
        results.Add("B07_DuplicateJoin", new([id, match], [[0, 0], [0, 0], [1, 1], [1, 1], [2, 2], [2, 2]]));
        results.Add("B08_LeftJoin", new([id, nullableMatch], [[0, 0], [1, 1], [2, 2], [3, null]]));
        results.Add("B08_FullJoin", new([("Id", typeof(int?)), nullableMatch], [[null, 4], [0, 0], [1, 1], [2, 2], [3, null]]));
        results.Add("B08_RightJoin", new([("Id", typeof(int?)), match], [[null, 4], [0, 0], [1, 1], [2, 2]]));
        results.Add("B08_SemiJoin", new([id], [[0], [1], [2]]));
        results.Add("B08_AntiJoin", new([id], [[3]]));
        results.Add("B08_ThetaJoin", new([id, match], [[0, 1], [0, 2], [1, 2]]));
        results.Add("B08_CrossJoin", new([id, match], [[0, 0], [0, 1], [0, 2], [1, 0], [1, 1], [1, 2], [2, 0], [2, 1], [2, 2], [3, 0], [3, 1], [3, 2]]));
        results.Add("B09_AsOf", new([id, nullableMatch], [[0, 0], [1, 1], [2, 2], [3, 3]]));
        results.Add("B10_Exists", new([id], [[0], [1], [2]]));
        results.Add("B10_CorrelatedFallback", new([id, nullableMatch], [[0, 0], [1, 1], [2, 2], [3, 2]]));
        results.Add("B11_ScalarAggregate", new([id, ("Total", typeof(decimal?))], [[0, 1m], [1, 2m], [2, 3m], [3, null]]));
        results.Add("B12_Quantified", new([id, ("Larger", typeof(bool?))], [[0, false], [1, false], [2, false], [3, true]]));
        results.Add("B24_Enums", new([id, ("State", typeof(int))], [[0, 1], [2, 1]]));
    }
}
