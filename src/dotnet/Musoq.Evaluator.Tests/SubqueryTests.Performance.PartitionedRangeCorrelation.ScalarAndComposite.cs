using System.Collections.Generic;
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
        var table = CreateAndRunVirtualMachine(query, CreatePartitionedRangeSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("SmallerCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("HasEarlier", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["MATCH", "Y"],
            ["NULL_PART", "N"],
            ["NULL_RANGE", "N"],
            ["WRONG_COMPOSITE", "N"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("ValueTuple<string, string>?", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("CreateAsOfEqualityKey", inspection.GeneratedCSharpCode);
        Assert.Contains("PredicateRangeMark", inspection.PlanningText);
        AssertNoPerRowSubqueryExecution(inspection);
    }
}
