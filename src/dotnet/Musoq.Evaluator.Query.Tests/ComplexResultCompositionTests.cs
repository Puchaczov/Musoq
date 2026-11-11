using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class ComplexResultCompositionTests : BasicEntityTestBase
{
    [TestMethod]
    public void Run_WhenAllResultPhasesAreComposed_ShouldApplyPagingLast()
    {
        const string query = (
            "with expanded as (\n"
            + "    select a.City as City, child.Name as ChildName\n"
            + "    from #A.entities() a\n"
            + "    cross apply a.Children child\n"
            + "    union all\n"
            + "    select a.City as City, child.Name as ChildName\n"
            + "    from #A.entities() a\n"
            + "    cross apply a.Children child\n"
            + "), uniqueRows as (\n"
            + "    select distinct City, ChildName from expanded\n"
            + "), grouped as (\n"
            + "    select City, Count(ChildName) as ChildCount\n"
            + "    from uniqueRows\n"
            + "    group by City\n"
            + "    having Count(ChildName) > 1\n"
            + ")\n"
            + "select City, ChildCount,\n"
            + "       RowNumber() over (order by City) as Rank\n"
            + "from grouped\n"
            + "qualify RowNumber() over (order by City) <= 2\n"
            + "order by City\n"
            + "skip 1 take 1"
        );
        var table = CreateAndRunVirtualMachine(
                query,
                new Dictionary<string, IEnumerable<BasicEntity>>
                {
                    ["#A"] =
                    [
                        new BasicEntity { City = "GDA" },
                        new BasicEntity { City = "KRA" },
                        new BasicEntity { City = "WAW" },
                    ],
                }
            )
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("ChildCount", typeof(long)),
            ("Rank", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["KRA", 2L, 2L]);
    }
}
