using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenScalarUsesEqualityAndRangeCorrelation_ShouldProbeOnlyItsPartition()
    {
        const string query = @"
            SELECT a.Name, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                  AND b.Population < a.Population
            ) AS SmallerCity
            FROM #A.entities() a
            ORDER BY a.Name";
        var sources = CreatePartitionedRangeSources();

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["DE_MATCH", "D_SMALL"],
            new object?[] { "NULL_PART", null },
            new object?[] { "PL_LOW", null },
            ["PL_MATCH", "P_SMALL"]);
        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalSortMergeJoin [LeftSingle]", inspection.PhysicalPlanText);
        Assert.Contains("[partitions:", inspection.PhysicalPlanText);
        Assert.Contains("ScalarRangeSingle", inspection.PlanningText);
        Assert.Contains("CreateRangeIndex", inspection.ExecutionPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenRangeMarkUsesCompositeNullablePartition_ShouldPreserveSqlNullSemantics()
    {
        const string query = @"
            SELECT a.Name,
                   CASE WHEN EXISTS (
                       SELECT b.Name FROM #B.entities() b
                       WHERE b.Country = a.Country
                         AND b.City = a.City
                         AND b.NullableValue < a.NullableValue
                   ) THEN 'Y' ELSE 'N' END AS HasEarlier
            FROM #A.entities() a
            ORDER BY a.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = "MATCH", Country = "P", City = "C", NullableValue = 20 },
                new BasicEntity { Name = "NULL_PART", Country = null, City = "C", NullableValue = 20 },
                new BasicEntity { Name = "NULL_RANGE", Country = "P", City = "C", NullableValue = null },
                new BasicEntity { Name = "WRONG_COMPOSITE", Country = "P", City = "D", NullableValue = 20 }
            ],
            ["#B"] =
            [
                new BasicEntity { Name = "P_C", Country = "P", City = "C", NullableValue = 10 },
                new BasicEntity { Name = "NULL_C", Country = null, City = "C", NullableValue = 10 },
                new BasicEntity { Name = "P_D", Country = "P", City = "D", NullableValue = null }
            ]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);
        var actual = table.Select(row => $"{row.Values[0]}:{row.Values[1]}").ToArray();

        CollectionAssert.AreEqual(
            new[] { "MATCH:Y", "NULL_PART:N", "NULL_RANGE:N", "WRONG_COMPOSITE:N" },
            actual);
        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("ValueTuple<string, string>?", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("CreateAsOfEqualityKey", inspection.GeneratedCSharpCode);
        Assert.Contains("PredicateRangeMark", inspection.PlanningText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

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
        var actual = table.Select(row => $"{row.Values[0]}:{row.Values[1]}").ToArray();

        CollectionAssert.AreEqual(
            new[] { $"ABOVE:{(expectedAbove ? "Y" : "N")}", $"EQUAL:{(expectedEqual ? "Y" : "N")}" },
            actual);
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

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["NULL_PART"], ["PL_LOW"]);
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

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["PL_MATCH"]);
        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalSortMergeJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("PredicateRangeSemiJoin", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

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
