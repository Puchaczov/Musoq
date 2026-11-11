using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class MultiJoinHashJoinTests
{
    [TestMethod]
    public void MultiPhaseHashJoin_CteOuterJoinGroupHavingAndOrder_ShouldMaterializeExactResult()
    {
        const string query = (
            "with joined as (\n"
            + "    select a.Country as Country,\n"
            + "           a.Name as Name,\n"
            + "           c.Population as MatchedPopulation\n"
            + "    from #A.Entities() a\n"
            + "    inner join #B.Entities() b on a.Name = b.Name\n"
            + "    left outer join #C.Entities() c on b.Name = c.Name\n"
            + ")\n"
            + "select Country,\n"
            + "       Count(Name) as MatchCount,\n"
            + "       Sum(MatchedPopulation) as TotalPopulation\n"
            + "from joined\n"
            + "group by Country\n"
            + "having Count(Name) > 0\n"
            + "order by Country"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = "A1", Country = "Poland" },
                new BasicEntity { Name = "A2", Country = "Poland" },
                new BasicEntity { Name = "A3", Country = "Germany" },
            ],
            ["#B"] =
            [
                new BasicEntity { Name = "A1" },
                new BasicEntity { Name = "A2" },
                new BasicEntity { Name = "A3" },
            ],
            ["#C"] =
            [
                new BasicEntity { Name = "A1", Population = 100 },
                new BasicEntity { Name = "A3", Population = 50 },
            ],
        };

        var table = CreateAndRunVirtualMachine(
                query,
                sources,
                new CompilationOptions(useHashJoin: true, useSortMergeJoin: false)
            )
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("MatchCount", typeof(long)),
            ("TotalPopulation", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Germany", 1L, 50m], ["Poland", 2L, 100m]);
    }
}
