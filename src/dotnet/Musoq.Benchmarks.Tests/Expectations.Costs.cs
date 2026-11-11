namespace Musoq.Benchmarks.Tests;

public static partial class Expectations
{
    private static void AddCosts(Dictionary<string, ExpectedResult> results)
    {
        var id = ("Id", typeof(int));
        results.Add(
            "B26_ComputedProjection_Accepted",
            new(
                [id, ("Value", typeof(long?))],
                [
                    [0, 0L],
                    [1, 2L],
                    [2, 4L],
                    [3, 6L],
                ]
            )
        );
        results.Add(
            "B26_ComputedProjection_Residual",
            new(
                [id, ("Value", typeof(long?))],
                [
                    [0, 0L],
                    [1, 2L],
                    [2, 4L],
                    [3, 6L],
                ]
            )
        );
        results.Add("B01_Scan_Unordered", results["B01_Scan"]);
        results.Add("B01_Wide_Unordered", results["B01_Wide"]);
        results.Add("B02_Filter_Selectivity0", new([id], []));
        results.Add(
            "B02_Filter_Selectivity1",
            new(
                [id],
                [
                    [0],
                ]
            )
        );
        results.Add(
            "B02_Filter_Selectivity50",
            new(
                [id],
                [
                    [0],
                    [1],
                    [2],
                    [3],
                ]
            )
        );
        results.Add(
            "B02_Filter_Selectivity100",
            new(
                [id],
                [
                    [0],
                    [1],
                    [2],
                    [3],
                ]
            )
        );
        results.Add(
            "B07_StringKeyJoin",
            new(
                [id, ("Match", typeof(int))],
                [
                    [0, 0],
                    [1, 1],
                    [2, 2],
                    [3, 3],
                ]
            )
        );
        results.Add(
            "B07_NullableCompositeJoin",
            new(
                [id, ("Match", typeof(int))],
                [
                    [1, 1],
                    [2, 2],
                    [3, 3],
                ]
            )
        );
        results.Add("B10_PartitionedRangeExists", new([id], []));
        long[] work = [-2270045977830737844L, -5788477046585707825L, 9003479855735434098L, -1288988622825652747L];
        var scalars = new ExpectedResult(
            [id, ("First", typeof(long)), ("Second", typeof(long))],
            work.Select((value, index) => new object?[] { index, value, value }).ToArray()
        );
        results.Add("B03_StableScalarReuse", scalars);
        results.Add("B03_VolatileScalarControl", scalars);
        results.Add(
            "B13_ApplyInvariant",
            new(
                [id, ("Item", typeof(int)), ("Work", typeof(long))],
                work.SelectMany(
                        (value, index) => Enumerable.Range(0, 8).Select(item => new object?[] { index, item, value })
                    )
                    .ToArray()
            )
        );
        results.Add(
            "B14_CompositeGroups",
            new(
                [("Key", typeof(string)), ("Bucket", typeof(int)), ("Total", typeof(decimal?))],
                [
                    ["group-00000000", 0, 1m],
                    ["group-00000000", 1, 2m],
                    ["group-00000000", 2, 3m],
                    ["group-00000000", 3, 4m],
                ]
            )
        );
        results.Add(
            "B16_PartitionedRowsFrame",
            new(
                [id, ("Total", typeof(decimal?))],
                [
                    [0, 1m],
                    [1, 2m],
                    [2, 3m],
                    [3, 4m],
                ]
            )
        );
        results.Add(
            "B27_Binary4KiB",
            new(
                [id, ("Value", typeof(int)), ("TailLength", typeof(int?))],
                [
                    [0, 1, 4092],
                    [1, 2, 4092],
                    [2, 3, 4092],
                    [3, 4, 4092],
                ]
            )
        );
        results.Add(
            "B30_Text1KiB",
            new(
                [id, ("Level", typeof(string)), ("Number", typeof(int?)), ("LabelLength", typeof(int?))],
                [
                    [0, "INFO", 0, 1017],
                    [1, "INFO", 1, 1017],
                    [2, "INFO", 2, 1017],
                    [3, "INFO", 3, 1017],
                ]
            )
        );
    }
}
