using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class VectorizationWorkloadTests
{
    public static IEnumerable<object[]> Cases => VectorizationScenarios.All.Select(s => new object[] { s.Id });

    [TestMethod]
    [DataRow("V_StoredCounts", "BitOperations.PopCount")]
    [DataRow("V_StoredExtrema", "Vector256.Max")]
    [DataRow("V_StoredGroups", "StoredNumericColumn<int>")]
    [DataRow("V_StoredNullableExtrema", "StoredNumericColumn<int?>")]
    public void StoredAggregateSelectsItsQualifiedRepresentation(string id, string operation)
    {
        var scenario = ScenarioCatalog.Get(id);
        var inspection = InstanceCreator.CompileForInspection(
            scenario.Sql,
            "numeric-aggregate",
            FixtureCatalog.Prepare(scenario, 65).CreateSchemaProvider(),
            BenchmarkLogger.Instance,
            ExecutionSettings.Create(scenario, 65)
        );
        StringAssert.Contains(inspection.GeneratedCSharpCode, operation);
        StringAssert.Contains(inspection.OptimizedExecutionPlanText, "GeneratedColumnarBuffer");
    }

    [TestMethod]
    [DataRow("V_NumericStored_Int32")]
    [DataRow("V_NumericStored_NullableDouble")]
    public void StoredNumericWorkloadSelectsBatchedComparisonAndKeepsScalarFallbackForDivision(string id)
    {
        var scenario = ScenarioCatalog.Get(id);
        var schema = FixtureCatalog.Prepare(scenario, 65).CreateSchemaProvider();
        string Generated(string sql) =>
            InstanceCreator
                .CompileForInspection(
                    sql,
                    "numeric-filter",
                    schema,
                    BenchmarkLogger.Instance,
                    ExecutionSettings.Create(scenario, 65)
                )
                .GeneratedCSharpCode;
        StringAssert.Contains(Generated(scenario.Sql), "ExtractMostSignificantBits");
        var division = scenario.Sql.Replace(
            "(r.Reading + r.Reading) - r.Reading * r.Reading",
            "r.Reading / r.Reading",
            StringComparison.Ordinal
        );
        Assert.IsFalse(Generated(division).Contains("ExtractMostSignificantBits", StringComparison.Ordinal));
    }

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void LocalWorkloadMatchesIndependentOracle(string id)
    {
        var scenario = ScenarioCatalog.Get(id);
        const int count = 4;
        using var query = InstanceCreator.CompileForExecution(
            scenario.Sql,
            "vectorization-workload",
            FixtureCatalog.Prepare(scenario, count).CreateSchemaProvider(),
            BenchmarkLogger.Instance,
            ExecutionSettings.Create(scenario, count)
        );
        if (scenario.MembershipIds is { } keys)
            query.TypedParameters.SetCollection("ids", keys);
        ResultAssertions.Execution(Expected(scenario), query, scenario);
    }

    private static ExpectedResult Expected(Scenario scenario)
    {
        if (scenario.FixtureId == "numeric-batch")
        {
            var type = scenario.DataProfileId switch
            {
                "Int32" => typeof(int),
                "NullableInt32" => typeof(int?),
                "Int64" => typeof(long),
                "NullableInt64" => typeof(long?),
                "Single" => typeof(float),
                "NullableSingle" => typeof(float?),
                "Double" => typeof(double),
                "NullableDouble" => typeof(double?),
                _ => throw new ArgumentException(scenario.DataProfileId),
            };
            var element = Nullable.GetUnderlyingType(type) ?? type;
            var rows = new List<object?[]>();
            if (element != type)
                rows.Add([0, null]);
            rows.Add([2, Convert.ChangeType(0, element, System.Globalization.CultureInfo.InvariantCulture)]);
            rows.Add([3, Convert.ChangeType(-3, element, System.Globalization.CultureInfo.InvariantCulture)]);
            return new([("Id", typeof(int)), ("Score", type)], rows.ToArray());
        }
        if (scenario.MembershipIds is { } keys)
        {
            var ids =
                keys.Count == 0 ? Array.Empty<int>()
                : scenario.DataProfileId == "rare" ? [0]
                : new[] { 0, 1, 2, 3 };
            return new(
                [("Name", typeof(string)), ("Id", typeof(int))],
                ids.Select(id =>
                        new object?[] { id.ToString("D8", System.Globalization.CultureInfo.InvariantCulture), id }
                    )
                    .ToArray()
            );
        }
        if (scenario.Id.StartsWith("V_Tag", StringComparison.Ordinal))
        {
            var width = int.Parse(scenario.Id.AsSpan(5), System.Globalization.CultureInfo.InvariantCulture);
            var value = new string('x', width);
            return new(
                [("Id", typeof(int)), ("Tag", typeof(string))],
                [
                    [0, value + "[y]"],
                    [1, value],
                    [2, value],
                    [3, value],
                ]
            );
        }
        if (scenario.Id.EndsWith("Project", StringComparison.Ordinal))
            return new(
                [("Id", typeof(int)), ("Score", typeof(int))],
                [
                    [2, 5],
                    [3, 7],
                ]
            );
        if (scenario.Id.EndsWith("Extrema", StringComparison.Ordinal))
            return new(
                [("Minimum", typeof(int?)), ("Maximum", typeof(int?))],
                [
                    [scenario.Id.EndsWith("NullableExtrema", StringComparison.Ordinal) ? 1 : 0, 3],
                ]
            );
        if (scenario.Id.EndsWith("Counts", StringComparison.Ordinal))
            return new(
                [("Eligible", typeof(long)), ("Recent", typeof(long))],
                [
                    [2L, 4L],
                ]
            );
        if (scenario.Id.EndsWith("Groups", StringComparison.Ordinal))
            return new(
                [
                    ("Bucket", typeof(int)),
                    ("Eligible", typeof(long)),
                    ("Recent", typeof(long)),
                    ("Minimum", typeof(int?)),
                    ("Maximum", typeof(int?)),
                ],
                [
                    [0, 0L, 1L, 0, 0],
                    [1, 0L, 1L, 1, 1],
                    [2, 1L, 1L, 2, 2],
                    [3, 1L, 1L, 3, 3],
                ]
            );
        Assert.AreEqual("V_Window", scenario.Id);
        return Expectations.Get("B16_RowsFrame");
    }
}
