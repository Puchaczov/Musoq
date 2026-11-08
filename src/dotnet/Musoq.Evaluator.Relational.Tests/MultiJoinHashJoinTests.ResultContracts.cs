using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class MultiJoinHashJoinTests
{
    [TestMethod]
    public void MultiPhaseHashJoin_CteOuterJoinGroupHavingAndOrder_ShouldMaterializeExactResult()
    {
        const string query = """
                             with joined as (
                                 select a.Country as Country,
                                        a.Name as Name,
                                        c.Population as MatchedPopulation
                                 from #A.Entities() a
                                 inner join #B.Entities() b on a.Name = b.Name
                                 left outer join #C.Entities() c on b.Name = c.Name
                             )
                             select Country,
                                    Count(Name) as MatchCount,
                                    Sum(MatchedPopulation) as TotalPopulation
                             from joined
                             group by Country
                             having Count(Name) > 0
                             order by Country
                             """;

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = "A1", Country = "Poland" },
                new BasicEntity { Name = "A2", Country = "Poland" },
                new BasicEntity { Name = "A3", Country = "Germany" }
            ],
            ["#B"] =
            [
                new BasicEntity { Name = "A1" },
                new BasicEntity { Name = "A2" },
                new BasicEntity { Name = "A3" }
            ],
            ["#C"] =
            [
                new BasicEntity { Name = "A1", Population = 100 },
                new BasicEntity { Name = "A3", Population = 50 }
            ]
        };

        var table = CreateAndRunVirtualMachine(
            query,
            sources,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false))
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("MatchCount", typeof(long)),
            ("TotalPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Germany", 1L, 50m],
            ["Poland", 2L, 100m]);
    }
}
