using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenRangeCorrelatedExistsProducesValue_ShouldUseBoundedRangeMarkProbe()
    {
        const string query = @"
            SELECT a.City,
                   CASE WHEN EXISTS (
                       SELECT b.City FROM #B.entities() b
                       WHERE b.Population < a.Population
                   ) THEN 'Y' ELSE 'N' END AS HasSmaller
            FROM #A.entities() a
            ORDER BY a.City";
        var sources = CreateRangeCorrelationSources();

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("HasSmaller", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["HIGH", "Y"],
            ["LOW", "N"],
            ["MID", "Y"]);
        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> PredicateRangeMark", inspection.PlanningText);
        Assert.Contains("PhysicalSortMergeJoin [LeftMark]", inspection.PhysicalPlanText);
        Assert.Contains("CreateRangeIndex", inspection.ExecutionPlanText);
        Assert.Contains("RangeProbe", inspection.ExecutionPlanText);
        Assert.Contains("Break", inspection.ExecutionPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenRangeCorrelatedScalarHasZeroOrOneMatch_ShouldUseRangeSingleAndNullExtend()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Population < a.Population
            ) AS SmallerCity
            FROM #A.entities() a
            ORDER BY a.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("LOW", "X", 50),
                new BasicEntity("MID", "X", 150)
            ],
            ["#B"] = [new BasicEntity("SMALL", "X", 100)]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { "LOW", null },
            ["MID", "SMALL"]);
        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> ScalarRangeSingle", inspection.PlanningText);
        Assert.Contains("PhysicalSortMergeJoin [LeftSingle]", inspection.PhysicalPlanText);
        Assert.Contains("RangeProbeNoMatch", inspection.ExecutionPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenRangeCorrelatedScalarHasMultipleMatches_ShouldThrowAtIndexedProbe()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Population < a.Population
            ) AS SmallerCity
            FROM #A.entities() a";
        var sources = CreateRangeCorrelationSources();
        var vm = CreateAndRunVirtualMachine(query, sources);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _ = vm.Run(TestContext.CancellationToken).Count);

        Assert.AreEqual("Scalar subquery returned more than one row.", exception.Message);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateRangeCorrelationSources() => new()
    {
        ["#A"] =
        [
            new BasicEntity("LOW", "X", 50),
            new BasicEntity("MID", "X", 150),
            new BasicEntity("HIGH", "X", 300)
        ],
        ["#B"] =
        [
            new BasicEntity("SMALL", "X", 100),
            new BasicEntity("MEDIUM", "X", 200)
        ]
    };
}
