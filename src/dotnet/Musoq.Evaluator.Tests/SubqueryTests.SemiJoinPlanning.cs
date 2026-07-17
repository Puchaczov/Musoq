using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenInSubquery_HasDuplicateMatches_ShouldUseLeftSemiJoinAndNotMultiplyRows()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 100),
                    new BasicEntity("WARSAW", "POLAND", 200),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        CollectionAssert.AreEqual(
            new[] { "WARSAW", "PARIS" },
            table.Select(row => (string)row.Values[0]).ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("CreateKeySet [_sq_1Keys", inspection.ExecutionPlanText);
        Assert.Contains("KeySetProbe", inspection.ExecutionPlanText);
        Assert.IsFalse(inspection.ExecutionPlanText.Contains("AggregateGroup", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenNotInSubquery_HasDuplicateMatches_ShouldUseLeftAntiSemiJoin()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City NOT IN (SELECT b.City FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 100),
                    new BasicEntity("WARSAW", "POLAND", 200)
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        CollectionAssert.AreEqual(
            new[] { "BERLIN", "PARIS" },
            table.Select(row => (string)row.Values[0]).ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
        Assert.Contains("KeySetProbe", inspection.ExecutionPlanText);
    }

    [TestMethod]
    public void WhenInSubquery_IsInsideOrBranch_ShouldKeepOuterJoinFallback()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)
               OR a.Country = 'GERMANY'";

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("PhysicalHashJoin [LeftMark]", inspection.PhysicalPlanText);
        Assert.IsFalse(inspection.PhysicalPlanText.Contains("PhysicalHashJoin [LeftSemi]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenInSubquery_FollowsExistingJoin_ShouldRebindTransitionKeys()
    {
        const string query = @"
            SELECT a.City, b.Country FROM #A.entities() a
            INNER JOIN #B.entities() b ON a.Country = b.Country
            WHERE a.City IN (SELECT c.City FROM #C.entities() c)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("KRAKOW", "POLAND", 200),
                    new BasicEntity("MUNICH", "GERMANY", 200)
                ]
            },
            {
                "#C", [new BasicEntity("WARSAW", "POLAND", 100)]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("POLAND", table[0].Values[1]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("string key = a.City;", StringComparison.Ordinal));
    }

    private QueryInspectionResult CompileSubqueryForInspection(string query)
    {
        return InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>>
                {
                    { "#A", Array.Empty<BasicEntity>() },
                    { "#B", Array.Empty<BasicEntity>() },
                    { "#C", Array.Empty<BasicEntity>() }
                }),
            new TestsLoggerResolver(),
            TestCompilationOptions);
    }
}
