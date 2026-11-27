using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class SourceMethodIdentityTests : BasicEntityTestBase
{
    private const string JoinedSources = " from #A.Entities() a cross join #B.Entities() b";

    [TestMethod]
    public void RankingUsesTheReceiverOfEachSourceMethod()
    {
        const string sql =
            "select Rank() over (order by a.GetCountry()) as L,Rank() over (order by b.GetCountry()) as R"
            + JoinedSources;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new() { Country = "X" }, new() { Country = null }],
            ["#B"] = [new() { Country = "X" }],
        };
        using var query = CreateAndRunVirtualMachine(sql, sources);
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [2L, 1L], [1L, 1L]);
    }

    [TestMethod]
    public void GroupKeysUseTheReceiverOfEachSourceMethod()
    {
        const string sql =
            "select a.GetCountry() as L,b.GetCountry() as R"
            + JoinedSources
            + " group by a.GetCountry(),b.GetCountry()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new() { Country = null }],
            ["#B"] = [new() { Country = "X" }],
        };
        using var query = CreateAndRunVirtualMachine(sql, sources);
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [null, "X"]);
    }

    [TestMethod]
    public void OrderByUsesTheProjectedMethodWithTheRequestedReceiver()
    {
        const string sql =
            "select a.GetCountry() as L,b.GetCountry() as R from #A.Entities() a cross join #B.Entities() b "
            + "order by b.GetCountry() asc nulls first";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new() { Country = "X" }],
            ["#B"] = [new() { Country = "Z" }, new() { Country = null }],
        };

        using var query = CreateAndRunVirtualMachine(sql, sources);
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(result, ["X", null], ["X", "Z"]);
    }
}
