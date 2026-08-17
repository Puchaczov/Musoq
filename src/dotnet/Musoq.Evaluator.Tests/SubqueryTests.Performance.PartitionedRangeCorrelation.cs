using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    [DataRow("b.Population < a.Population", true, false)]
    [DataRow("b.Population <= a.Population", true, true)]
    [DataRow("b.Population > a.Population", false, false)]
    [DataRow("b.Population >= a.Population", false, true)]
    public void WhenPartitionedRangeMarkUsesComparator_ShouldNormalizeBoundaries(
        string predicate,
        bool expectedAbove,
        bool expectedEqual)
    {
        var query = $@"
            SELECT a.Name,
                   CASE WHEN EXISTS (
                       SELECT b.Name FROM #B.entities() b
                       WHERE b.Country = a.Country AND {predicate}
                   ) THEN 'Y' ELSE 'N' END AS HasMatch
            FROM #A.entities() a
            ORDER BY a.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = "ABOVE", Country = "P", Population = 110 },
                new BasicEntity { Name = "EQUAL", Country = "P", Population = 100 }
            ],
            ["#B"] = [new BasicEntity { Name = "BOUNDARY", Country = "P", Population = 100 }]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("HasMatch", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["ABOVE", expectedAbove ? "Y" : "N"],
            ["EQUAL", expectedEqual ? "Y" : "N"]);
        AssertNoPerRowSubqueryExecution(CompileSubqueryForInspection(query));
    }

    [TestMethod]
    public void WhenNotExistsUsesEqualityAndRangeCorrelation_ShouldUseRangeAntiSemi()
    {
        const string query = @"
            SELECT a.Name FROM #A.entities() a
            WHERE NOT EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                  AND b.Population < a.Population
            )
            ORDER BY a.Name";
        var sources = CreatePartitionedRangeSources();

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["NULL_PART"], ["PL_LOW"]);
        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalSortMergeJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
        Assert.Contains("PredicateRangeAntiSemiJoin", inspection.PlanningText);
        Assert.Contains("RangeProbeNoMatch", inspection.ExecutionPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenNestedExistsContainsPartitionedRangeCorrelation_ShouldDecorrelateBothLevels()
    {
        const string query = @"
            SELECT a.Name FROM #A.entities() a
            WHERE EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                  AND b.Population < a.Population
                  AND EXISTS (
                      SELECT c.City FROM #C.entities() c
                      WHERE c.City = b.City
                  )
            )
            ORDER BY a.Name";
        var sources = CreatePartitionedRangeSources();
        sources["#C"] = [new BasicEntity { City = "P_SMALL" }];

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["PL_MATCH"]);
        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalSortMergeJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("PredicateRangeSemiJoin", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    [DataRow("b.NullableValue < a.NullableValue")]
    [DataRow("b.NullableValue <= a.NullableValue")]
    [DataRow("b.NullableValue > a.NullableValue")]
    [DataRow("b.NullableValue >= a.NullableValue")]
    public void WhenRangeSemiUsesComparator_ShouldReturnExactBoundaryRows(string predicate)
    {
        var query = $@"
            SELECT a.Name FROM #A.entities() a
            WHERE EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country AND {predicate}
            )
            ORDER BY a.Name";
        var table = CreateAndRunVirtualMachine(query, CreateRangeMatrixSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ExpectedRangeSemiRows(predicate));

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PredicateRangeSemiJoin", inspection.PlanningText);
        Assert.Contains("PhysicalSortMergeJoin [LeftSemi]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    [DataRow("b.NullableValue < a.NullableValue")]
    [DataRow("b.NullableValue <= a.NullableValue")]
    [DataRow("b.NullableValue > a.NullableValue")]
    [DataRow("b.NullableValue >= a.NullableValue")]
    public void WhenRangeAntiSemiUsesComparator_ShouldReturnExactNonMatchingRows(string predicate)
    {
        var query = $@"
            SELECT a.Name FROM #A.entities() a
            WHERE NOT EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country AND {predicate}
            )
            ORDER BY a.Name";
        var table = CreateAndRunVirtualMachine(query, CreateRangeMatrixSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ExpectedRangeAntiRows(predicate));

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PredicateRangeAntiSemiJoin", inspection.PlanningText);
        Assert.Contains("PhysicalSortMergeJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    [DataRow("b.NullableValue < a.NullableValue")]
    [DataRow("b.NullableValue <= a.NullableValue")]
    [DataRow("b.NullableValue > a.NullableValue")]
    [DataRow("b.NullableValue >= a.NullableValue")]
    public void WhenRangeScalarUsesComparator_ShouldReturnExactValueOrNull(string predicate)
    {
        var query = $@"
            SELECT a.Name, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country AND {predicate}
            ) AS MatchCity
            FROM #A.entities() a
            ORDER BY a.Name";
        var table = CreateAndRunVirtualMachine(query, CreateRangeMatrixSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ExpectedRangeScalarRows(predicate));

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("ScalarRangeSingle", inspection.PlanningText);
        Assert.Contains("PhysicalSortMergeJoin [LeftSingle]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    private static object?[][] ExpectedRangeSemiRows(string predicate)
    {
        return predicate switch
        {
            "b.NullableValue < a.NullableValue" => [["ABOVE"]],
            "b.NullableValue <= a.NullableValue" => [["ABOVE"], ["EQUAL"]],
            "b.NullableValue > a.NullableValue" => [["BELOW"]],
            "b.NullableValue >= a.NullableValue" => [["BELOW"], ["EQUAL"]],
            _ => throw new ArgumentOutOfRangeException(nameof(predicate), predicate, null)
        };
    }

    private static object?[][] ExpectedRangeAntiRows(string predicate)
    {
        return predicate switch
        {
            "b.NullableValue < a.NullableValue" => [["BELOW"], ["EMPTY"], ["EQUAL"], ["NULL_PART"], ["NULL_RANGE"]],
            "b.NullableValue <= a.NullableValue" => [["BELOW"], ["EMPTY"], ["NULL_PART"], ["NULL_RANGE"]],
            "b.NullableValue > a.NullableValue" => [["ABOVE"], ["EMPTY"], ["EQUAL"], ["NULL_PART"], ["NULL_RANGE"]],
            "b.NullableValue >= a.NullableValue" => [["ABOVE"], ["EMPTY"], ["NULL_PART"], ["NULL_RANGE"]],
            _ => throw new ArgumentOutOfRangeException(nameof(predicate), predicate, null)
        };
    }

    private static object?[][] ExpectedRangeScalarRows(string predicate)
    {
        return predicate switch
        {
            "b.NullableValue < a.NullableValue" =>
            [
                ["ABOVE", "BOUNDARY"],
                ["BELOW", null],
                ["EMPTY", null],
                ["EQUAL", null],
                ["NULL_PART", null],
                ["NULL_RANGE", null]
            ],
            "b.NullableValue <= a.NullableValue" =>
            [
                ["ABOVE", "BOUNDARY"],
                ["BELOW", null],
                ["EMPTY", null],
                ["EQUAL", "BOUNDARY"],
                ["NULL_PART", null],
                ["NULL_RANGE", null]
            ],
            "b.NullableValue > a.NullableValue" =>
            [
                ["ABOVE", null],
                ["BELOW", "BOUNDARY"],
                ["EMPTY", null],
                ["EQUAL", null],
                ["NULL_PART", null],
                ["NULL_RANGE", null]
            ],
            "b.NullableValue >= a.NullableValue" =>
            [
                ["ABOVE", null],
                ["BELOW", "BOUNDARY"],
                ["EMPTY", null],
                ["EQUAL", "BOUNDARY"],
                ["NULL_PART", null],
                ["NULL_RANGE", null]
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(predicate), predicate, null)
        };
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateRangeMatrixSources() => new()
    {
        ["#A"] =
        [
            new BasicEntity { Name = "ABOVE", Country = "P", NullableValue = 110 },
            new BasicEntity { Name = "BELOW", Country = "P", NullableValue = 90 },
            new BasicEntity { Name = "EMPTY", Country = "E", NullableValue = 100 },
            new BasicEntity { Name = "EQUAL", Country = "P", NullableValue = 100 },
            new BasicEntity { Name = "NULL_PART", Country = null, NullableValue = 100 },
            new BasicEntity { Name = "NULL_RANGE", Country = "P", NullableValue = null }
        ],
        ["#B"] = [new BasicEntity { City = "BOUNDARY", Country = "P", NullableValue = 100 }]
    };

    private static Dictionary<string, IEnumerable<BasicEntity>> CreatePartitionedRangeSources() => new()
    {
        ["#A"] =
        [
            new BasicEntity { Name = "PL_LOW", Country = "PL", Population = 50 },
            new BasicEntity { Name = "PL_MATCH", Country = "PL", Population = 150 },
            new BasicEntity { Name = "DE_MATCH", Country = "DE", Population = 150 },
            new BasicEntity { Name = "NULL_PART", Country = null, Population = 150 }
        ],
        ["#B"] =
        [
            new BasicEntity { City = "P_SMALL", Country = "PL", Population = 100 },
            new BasicEntity { City = "D_SMALL", Country = "DE", Population = 100 },
            new BasicEntity { City = "NULL_SMALL", Country = null, Population = 100 }
        ]
    };
}
