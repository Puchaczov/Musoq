using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class StringConcatenationBindingTests : BasicEntityTestBase
{
    [TestMethod]
    public void GroupByAllKeepsLegacyAndNullPropagatingConcatenationsSeparate()
    {
        const string sql = "select s.Country + 'z' as L, s.Country || 'z' as Q from #A.Entities() s group by all";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new()
                {
                    Name = "A",
                    Country = null,
                    Population = 1,
                },
            ],
        };

        using var query = CreateAndRunVirtualMachine(sql, sources);
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, ["z", null]);
    }

    [TestMethod]
    public void RankingUsesEachConcatenationOperatorsNullSemantics()
    {
        const string sql =
            "select s.S, Rank() over (order by s.S + 'z' asc nulls first) as L, "
            + "Rank() over (order by s.S || 'z' asc nulls first) as Q from values { (S: null), (S: 'a') } s";

        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [null, 2L, 1L], ["a", 1L, 2L]);
    }

    [TestMethod]
    public void DistinctOrderByKeepsTheNullPropagatingConcatenation()
    {
        const string sql =
            "select distinct s.S + 'z' as L, s.S || 'z' as R from values { (S: null), (S: 'a') } s "
            + "order by s.S || 'z' asc nulls first take 1";

        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(result, ["z", null]);
    }
}
