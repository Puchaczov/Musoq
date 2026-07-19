using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenCompositeExistsSubqueryUsesReferenceAndValueKeys_ShouldUseTypedHashKeyAndPreserveNullSemantics()
    {
        const string query = @"
            SELECT a.Name FROM #A.entities() a
            WHERE EXISTS (
                SELECT b.Name FROM #B.entities() b
                WHERE b.Country = a.Country
                  AND b.Population = a.Population
            )";
        var sources = CreateCompositeKeySources();
        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);
        var inspection = CompileSubqueryForInspection(query);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["match"]);
        Assert.Contains("ValueTuple<int, string, decimal>", inspection.ExecutionPlanText);
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("CreateNullableHashJoinKey", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("Dictionary<object, HashJoinBucket<", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenCompositeNotExistsSubqueryUsesReferenceAndValueKeys_ShouldTreatNullProbeAsNoMatch()
    {
        const string query = @"
            SELECT a.Name FROM #A.entities() a
            WHERE NOT EXISTS (
                SELECT b.Name FROM #B.entities() b
                WHERE b.Country = a.Country
                  AND b.Population = a.Population
            )";
        var table = CreateAndRunVirtualMachine(query, CreateCompositeKeySources()).Run(TestContext.CancellationToken);
        var inspection = CompileSubqueryForInspection(query);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["left-null"], ["no-match"]);
        Assert.Contains("ValueTuple<int, string, decimal>", inspection.ExecutionPlanText);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateCompositeKeySources() => new()
    {
        ["#A"] =
        [
            new BasicEntity { Name = "match", Country = "PL", Population = 10m },
            new BasicEntity { Name = "left-null", Country = null, Population = 20m },
            new BasicEntity { Name = "no-match", Country = "DE", Population = 30m }
        ],
        ["#B"] =
        [
            new BasicEntity { Name = "right-match", Country = "PL", Population = 10m },
            new BasicEntity { Name = "right-null", Country = null, Population = 20m }
        ]
    };
}
