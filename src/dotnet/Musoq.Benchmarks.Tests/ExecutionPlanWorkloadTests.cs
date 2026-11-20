using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class ExecutionPlanWorkloadTests
{
    public static IEnumerable<object[]> Cases =>
        ExecutionPlanScenarios.All.Select(scenario => new object[] { scenario.Id });

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void LocalWorkloadReturnsExpectedRows(string id)
    {
        var scenario = ScenarioCatalog.Get(id);
        using var query = InstanceCreator.CompileForExecution(
            scenario.Sql,
            "execution-plan-test",
            FixtureCatalog.Prepare(scenario, 4).CreateSchemaProvider(),
            BenchmarkLogger.Instance,
            ExecutionSettings.Create(scenario, 4)
        );
        ResultAssertions.Execution(Expected(scenario.DataProfileId), query, scenario);
    }

    private static ExpectedResult Expected(string profile) =>
        profile switch
        {
            "AllPass" => new(
                [("Id", typeof(int))],
                [
                    [0],
                    [1],
                    [2],
                    [3],
                ]
            ),
            "AllFirstFails" => new([("Id", typeof(int))], []),
            "JoinSparse" => new(
                [("LeftId", typeof(int?)), ("RightId", typeof(int?))],
                [
                    [0, null],
                    [1, null],
                    [2, null],
                    [3, null],
                    [null, 0],
                    [null, 1],
                    [null, 2],
                    [null, 3],
                ]
            ),
            "JoinDense" => new(
                [("LeftId", typeof(int?)), ("RightId", typeof(int?))],
                (
                    from left in Enumerable.Range(0, 4)
                    from right in Enumerable.Range(0, 8)
                    select new object?[] { left, right }
                ).ToArray()
            ),
            "FramePeers" => new(
                [("Id", typeof(int)), ("Total", typeof(int?)), ("Present", typeof(long))],
                [
                    [0, 4, 4L],
                    [1, 4, 4L],
                    [2, 4, 4L],
                    [3, 4, 4L],
                ]
            ),
            "FrameSpaced" => new(
                [("Id", typeof(int)), ("Total", typeof(int?)), ("Present", typeof(long))],
                [
                    [0, 0, 1L],
                    [1, 4, 1L],
                    [2, 8, 1L],
                    [3, 12, 1L],
                ]
            ),
            "RankOne" => new(
                [("Id", typeof(int)), ("Ordinal", typeof(long))],
                [
                    [3, 1L],
                    [2, 2L],
                ]
            ),
            "RankMany" => new(
                [("Id", typeof(int)), ("Ordinal", typeof(long))],
                [
                    [0, 2L],
                    [1, 1L],
                    [2, 2L],
                    [3, 1L],
                ]
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(profile)),
        };
}
