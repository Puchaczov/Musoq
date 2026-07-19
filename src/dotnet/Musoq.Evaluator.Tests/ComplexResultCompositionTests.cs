using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ComplexResultCompositionTests : BasicEntityTestBase
{
    [TestMethod]
    public void Run_WhenAllResultPhasesAreComposed_ShouldApplyPagingLast()
    {
        const string query = """
                             with expanded as (
                                 select a.City as City, child.Name as ChildName
                                 from #A.entities() a
                                 cross apply a.Children child
                                 union all
                                 select a.City as City, child.Name as ChildName
                                 from #A.entities() a
                                 cross apply a.Children child
                             ), uniqueRows as (
                                 select distinct City, ChildName from expanded
                             ), grouped as (
                                 select City, Count(ChildName) as ChildCount
                                 from uniqueRows
                                 group by City
                                 having Count(ChildName) > 1
                             )
                             select City, ChildCount,
                                    RowNumber() over (order by City) as Rank
                             from grouped
                             qualify RowNumber() over (order by City) <= 2
                             order by City
                             skip 1 take 1
                             """;
        var table = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { City = "GDA" },
                new BasicEntity { City = "KRA" },
                new BasicEntity { City = "WAW" }
            ]
        }).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("ChildCount", typeof(long)),
            ("Rank", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["KRA", 2L, 2L]);
    }
}
