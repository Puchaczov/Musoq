namespace Musoq.Benchmarks.Tests;

public static partial class Expectations
{
    private static void AddProfiles(Dictionary<string, ExpectedResult> results)
    {
        results.Add("B14_Group_ManyGroups", new([("Id", typeof(int)), ("Total", typeof(decimal?)), ("Present", typeof(long))],
            [[0, 1m, 0L], [1, 2m, 0L], [2, 3m, 1L], [3, 4m, 0L]]));
        results.Add("B07_EqualityJoin_Shuffled", new([("Id", typeof(int)), ("Match", typeof(int))],
            [[0, 0], [1, 1], [2, 2], [3, 3]]));
        results.Add("B07_DuplicateJoin_Fanout8", new([("Id", typeof(int)), ("Match", typeof(int))],
        [
            [0, 0], [0, 0], [0, 0], [0, 0], [0, 0], [0, 0], [0, 0], [0, 0],
            [1, 1], [1, 1], [1, 1], [1, 1], [1, 1], [1, 1], [1, 1], [1, 1],
            [2, 2], [2, 2], [2, 2], [2, 2], [2, 2], [2, 2], [2, 2], [2, 2]
        ]));
    }
}
