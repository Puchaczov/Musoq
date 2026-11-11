using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForExecution_WhenCteSidecarIndexesAreEnabledForFullOuterPresencePredicates_ShouldClassifyRows()
    {
        var compiled = CompileForExecution(
            (
                "with leftRows as (\n"
                + "    select 1 as Id from #system.dual()\n"
                + "    union all (Id) select 2 as Id from #system.dual()\n"
                + "),\n"
                + "rightRows as (\n"
                + "    select 2 as Id from #system.dual()\n"
                + "    union all (Id) select 3 as Id from #system.dual()\n"
                + ")\n"
                + "select\n"
                + "    case\n"
                + "        when l is missing and r is present then 'right-only'\n"
                + "        when l is present and r is missing then 'left-only'\n"
                + "        when l is present and r is present then 'matched'\n"
                + "        else 'unexpected'\n"
                + "    end as State,\n"
                + "    l.Id as LeftId,\n"
                + "    r.Id as RightId\n"
                + "from leftRows l\n"
                + "full outer join rightRows r on l.Id = r.Id\n"
                + "order by State"
            ),
            SidecarGlobalOptions
        );

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("left-only", table[0].ReadValue(0));
        Assert.AreEqual(1, table[0].ReadValue(1));
        Assert.IsNull(table[0].ReadValue(2));
        Assert.AreEqual("matched", table[1].ReadValue(0));
        Assert.AreEqual(2, table[1].ReadValue(1));
        Assert.AreEqual(2, table[1].ReadValue(2));
        Assert.AreEqual("right-only", table[2].ReadValue(0));
        Assert.IsNull(table[2].ReadValue(1));
        Assert.AreEqual(3, table[2].ReadValue(2));
    }

    [TestMethod]
    public void CompileForExecution_WhenCteSidecarIndexesAreEnabledForFullOuterResidualPredicate_ShouldKeepUnmatchedRows()
    {
        var compiled = CompileForExecution(
            (
                "with leftRows as (\n"
                + "    select 1 as Id, 'left-only' as City from #system.dual()\n"
                + "    union all (Id, City) select 2 as Id, 'reject' as City from #system.dual()\n"
                + "    union all (Id, City) select 3 as Id, 'match' as City from #system.dual()\n"
                + "),\n"
                + "rightRows as (\n"
                + "    select 2 as Id, 'reject' as City from #system.dual()\n"
                + "    union all (Id, City) select 3 as Id, 'match' as City from #system.dual()\n"
                + "    union all (Id, City) select 4 as Id, 'right-only' as City from #system.dual()\n"
                + ")\n"
                + "select\n"
                + "    case\n"
                + "        when l is missing then 'right-only'\n"
                + "        when r is missing then 'left-only'\n"
                + "        else 'matched'\n"
                + "    end as State,\n"
                + "    l.Id as LeftId,\n"
                + "    r.Id as RightId\n"
                + "from leftRows l\n"
                + "full outer join rightRows r on l.Id = r.Id and r.City = 'match'"
            ),
            SidecarGlobalOptions
        );

        var table = compiled.Run();
        var rows = Enumerable
            .Range(0, table.Count)
            .Select(index =>
                $"{table[index].ReadValue(0)}:{table[index].ReadValue(1) ?? "null"}:{table[index].ReadValue(2) ?? "null"}"
            )
            .OrderBy(static row => row)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "left-only:1:null", "left-only:2:null", "matched:3:3", "right-only:null:2", "right-only:null:4" },
            rows
        );
    }

    [TestMethod]
    public void CompileForInspection_WhenCteSidecarIndexesAreEnabledForFullOuterJoin_ShouldUseExecutionBackend()
    {
        var result = Inspect(
            (
                "with leftRows as (\n"
                + "    select 1 as Id from #system.dual()\n"
                + "),\n"
                + "rightRows as (\n"
                + "    select 2 as Id from #system.dual()\n"
                + ")\n"
                + "select l.Id as LeftId, r.Id as RightId\n"
                + "from leftRows l\n"
                + "full outer join rightRows r on l.Id = r.Id"
            ),
            SidecarGlobalOptions
        );

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanDoesNotContain("Sidecar join pipeline failed", result.ExecutionPlanText);
        AssertExecutionPlanContains("ReturnDeferredTable", result.ExecutionPlanText);
    }
}
