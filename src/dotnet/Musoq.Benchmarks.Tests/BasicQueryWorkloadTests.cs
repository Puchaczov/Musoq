using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class BasicQueryWorkloadTests
{
    private const int Count = 104;
    public static IEnumerable<object[]> Cases => BasicQueryScenarios.All.Select(item => new object[] { item.Id });

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void LocalQueryReturnsExpectedColumnsAndValues(string id)
    {
        var scenario = ScenarioCatalog.Get(id);
        var provider = FixtureCatalog.Prepare(scenario, Count).CreateSchemaProvider();
        var options = ExecutionSettings.Create(scenario, Count);
        using var query = InstanceCreator.CompileForExecution(
            scenario.Sql,
            "basic",
            provider,
            BenchmarkLogger.Instance,
            options
        );
        if (id is "BQ_Count" or "BQ_CountSmallChunks")
        {
            ResultAssertions.Execution(
                new(
                    [("Total", typeof(long))],
                    [
                        [(long)Count],
                    ]
                ),
                query,
                scenario
            );
            return;
        }

        var ids = id switch
        {
            "BQ_Project" or "BQ_ProjectSmallChunks" or "BQ_Filter100" => Enumerable.Range(0, Count),
            "BQ_Filter0" or "BQ_SkipDeep" => [],
            "BQ_Filter1" => [0, 100],
            "BQ_Filter50" => Enumerable.Range(0, 50).Concat([100, 101, 102, 103]),
            "BQ_Top1" => [103],
            "BQ_Top10Ascending" or "BQ_Take10" => Enumerable.Range(0, 10),
            "BQ_Top10Descending" or "BQ_Top10Shuffled" => Enumerable.Range(94, 10).Reverse(),
            "BQ_TopOffset" => Enumerable.Range(84, 10).Reverse(),
            _ => throw new ArgumentOutOfRangeException(nameof(id)),
        };
        var expected = ids.Select(index => new ProjectionOutput(index, $"item-{index % 4}")).ToArray();
        ResultAssertions.Execution(
            new(
                [("Id", typeof(int)), ("Name", typeof(string))],
                expected.Select(row => new object?[] { row.Id, row.Name }).ToArray()
            ),
            query,
            scenario
        );
        if (BasicQueryScenarios.Projections.Contains(scenario))
        {
            var typed = InstanceCreator.CompileForTypedExecution<ProjectionOutput>(
                scenario.Sql,
                "basicTyped",
                provider,
                BenchmarkLogger.Instance,
                options
            );
            CollectionAssert.AreEquivalent(expected, typed.Run().ToArray());
        }
    }
}
