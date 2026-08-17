using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenSevenPartCorrelationUsesTypedTupleKey_ShouldPreserveAllPredicateAndScalarResults()
    {
        const string query = @"
            SELECT a.Name,
                   CASE WHEN EXISTS (
                       SELECT b.City FROM #B.entities() b
                       WHERE b.Name = a.Name
                         AND b.City = a.City
                         AND b.Country = a.Country
                         AND b.Population = a.Population
                         AND b.Month = a.Month
                         AND b.Money = a.Money
                         AND b.Id = a.Id
                   ) THEN 'Y' ELSE 'N' END AS ExistsResult,
                   CASE WHEN NOT EXISTS (
                       SELECT b.City FROM #B.entities() b
                       WHERE b.Name = a.Name
                         AND b.City = a.City
                         AND b.Country = a.Country
                         AND b.Population = a.Population
                         AND b.Month = a.Month
                         AND b.Money = a.Money
                         AND b.Id = a.Id
                   ) THEN 'Y' ELSE 'N' END AS NotExistsResult,
                   (
                       SELECT b.City FROM #B.entities() b
                       WHERE b.Name = a.Name
                         AND b.City = a.City
                         AND b.Country = a.Country
                         AND b.Population = a.Population
                         AND b.Month = a.Month
                         AND b.Money = a.Money
                         AND b.Id = a.Id
                   ) AS Lookup
            FROM #A.entities() a
            ORDER BY a.Name";

        var table = CreateAndRunVirtualMachine(query, CreateSevenPartSources()).Run(TestContext.CancellationToken);

        AssertWideKeyRows(table, [
            ["S7Match", "Y", "N", "S7-City"],
            ["S7NoMatch", "N", "Y", null],
            ["S7Null", "N", "Y", null]
        ]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("ValueTuple<", inspection.GeneratedCSharpCode);
        Assert.Contains("PredicateHashMark", inspection.PlanningText);
        Assert.Contains("ScalarHashSingle", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenEightPartCorrelationUsesNestedTupleKey_ShouldMatchDefaultAndFallbackExecution()
    {
        const string query = @"
            SELECT a.Name,
                   CASE WHEN EXISTS (
                       SELECT b.City FROM #B.entities() b
                       WHERE b.Name = a.Name
                         AND b.City = a.City
                         AND b.Country = a.Country
                         AND b.Population = a.Population
                         AND b.Month = a.Month
                         AND b.Money = a.Money
                         AND b.Id = a.Id
                         AND b.NullableValue = a.NullableValue
                   ) THEN 'Y' ELSE 'N' END AS ExistsResult,
                   CASE WHEN NOT EXISTS (
                       SELECT b.City FROM #B.entities() b
                       WHERE b.Name = a.Name
                         AND b.City = a.City
                         AND b.Country = a.Country
                         AND b.Population = a.Population
                         AND b.Month = a.Month
                         AND b.Money = a.Money
                         AND b.Id = a.Id
                         AND b.NullableValue = a.NullableValue
                   ) THEN 'Y' ELSE 'N' END AS NotExistsResult,
                   (
                       SELECT b.City FROM #B.entities() b
                       WHERE b.Name = a.Name
                         AND b.City = a.City
                         AND b.Country = a.Country
                         AND b.Population = a.Population
                         AND b.Month = a.Month
                         AND b.Money = a.Money
                         AND b.Id = a.Id
                         AND b.NullableValue = a.NullableValue
                   ) AS Lookup
            FROM #A.entities() a
            ORDER BY a.Name";
        var sources = CreateEightPartSources();
        var expected = new object?[][]
        {
            ["S8Match", "Y", "N", "S8-City"],
            ["S8NoMatch", "N", "Y", null],
            ["S8Null", "N", "Y", null]
        };

        var defaultTable = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);
        var fallbackTable = CreateAndRunVirtualMachine(
                query,
                sources,
                new CompilationOptions(
                    useHashJoin: false,
                    useSortMergeJoin: false,
                    usePrimitiveTypeValidation: false))
            .Run(TestContext.CancellationToken);

        AssertWideKeyRows(defaultTable, expected);
        AssertWideKeyRows(fallbackTable, expected);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PredicateHashMark", inspection.PlanningText);
        Assert.Contains("ScalarHashSingle", inspection.PlanningText);
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("CreateNullableHashJoinKey", StringComparison.Ordinal));
        Assert.Contains("ValueTuple<", inspection.GeneratedCSharpCode);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    private static void AssertWideKeyRows(Table table, params object?[][] expected)
    {
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("ExistsResult", typeof(string)),
            ("NotExistsResult", typeof(string)),
            ("Lookup", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, expected);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSevenPartSources() => new()
    {
        ["#A"] =
        [
            WideKey("S7Match", "S7-City", "PL", 10m, "Jan", 20m, 7),
            WideKey("S7NoMatch", "S7-City", "PL", 10m, "Jan", 20m, 8),
            WideKey("S7Null", "S7-City", null, 10m, "Jan", 20m, 7)
        ],
        ["#B"] =
        [
            WideKey("S7Match", "S7-City", "PL", 10m, "Jan", 20m, 7),
            WideKey("S7Null", "S7-City", null, 10m, "Jan", 20m, 7)
        ]
    };

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateEightPartSources() => new()
    {
        ["#A"] =
        [
            WideKey("S8Match", "S8-City", "PL", 10m, "Jan", 20m, 7, 1),
            WideKey("S8NoMatch", "S8-City", "PL", 10m, "Jan", 20m, 7, 2),
            WideKey("S8Null", "S8-City", "PL", 10m, "Jan", 20m, 7, null)
        ],
        ["#B"] =
        [
            WideKey("S8Match", "S8-City", "PL", 10m, "Jan", 20m, 7, 1),
            WideKey("S8Null", "S8-City", "PL", 10m, "Jan", 20m, 7, null)
        ]
    };

    private static BasicEntity WideKey(
        string name,
        string city,
        string? country,
        decimal population,
        string month,
        decimal money,
        int id,
        int? nullableValue = null) => new()
    {
        Name = name,
        City = city,
        Country = country,
        Population = population,
        Month = month,
        Money = money,
        Id = id,
        NullableValue = nullableValue
    };
}
