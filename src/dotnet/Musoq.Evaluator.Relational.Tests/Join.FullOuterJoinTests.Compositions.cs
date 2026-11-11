using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class JoinFullOuterJoinTests
{
    [TestMethod]
    public void FullOuterJoin_BetweenInlineValuesSources_ShouldReturnMatchedRow()
    {
        const string query = (
            "select a.Id, b.Id\n"
            + "from values { ( Id: 1 ) } a\n"
            + "full outer join values { ( Id: 1 ) } b on a.Id = b.Id"
        );
        var vm = CreateAndRunVirtualMachine(query, CreateSources([], []));
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int?)), ("Id", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 1]);
    }

    [TestMethod]
    public void FullOuterJoin_InlineValuesDiff_ShouldClassifyChangedSameRemovedAndAddedRows()
    {
        const string query = (
            "SELECT CASE WHEN a IS MISSING THEN b.Id ELSE a.Id END AS Id,\n"
            + "       CASE WHEN a IS MISSING THEN 'added' WHEN b IS MISSING THEN 'removed'\n"
            + "            WHEN a.Value IS DISTINCT FROM b.Value THEN 'changed' ELSE 'same' END AS Change\n"
            + "FROM values { (Id: 1, Value: 'old'), (Id: 2, Value: 'steady'), (Id: 3, Value: 'gone') } a\n"
            + "FULL OUTER JOIN values { (Id: 1, Value: 'new'), (Id: 2, Value: 'steady'), (Id: 4, Value: 'fresh') } b ON a.Id = b.Id\n"
            + "ORDER BY Id"
        );
        var vm = CreateAndRunVirtualMachine(query, CreateSources([], []));
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int?)), ("Change", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [1, "changed"],
            [2, "same"],
            [3, "removed"],
            [4, "added"]
        );
    }

    [TestMethod]
    public void FullOuterJoin_WindowAndQualify_ShouldKeepMatchedAndUnmatchedRows()
    {
        const string query = (
            "with joined as (\n"
            + "    select\n"
            + "        case\n"
            + "            when b is missing then 'LeftOnly'\n"
            + "            when a is missing then 'RightOnly'\n"
            + "            else 'Matched'\n"
            + "        end as State,\n"
            + "        a.Id as LeftId,\n"
            + "        b.Id as RightId\n"
            + "    from #A.entities() a\n"
            + "    full outer join #B.entities() b on a.Id = b.Id\n"
            + ")\n"
            + "select State, LeftId, RightId,\n"
            + "       RowNumber() over (partition by State order by State) as Rank\n"
            + "from joined\n"
            + "qualify RowNumber() over (partition by State order by State) <= 1\n"
            + "order by State"
        );
        var table = Run(
            query,
            CreateSources(
                [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
                [new BasicEntity { Id = 2 }, new BasicEntity { Id = 3 }]
            )
        );

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("State", typeof(string)),
            ("LeftId", typeof(int?)),
            ("RightId", typeof(int?)),
            ("Rank", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LeftOnly", 1, null, 1L],
            ["Matched", 2, 2, 1L],
            ["RightOnly", null, 3, 1L]
        );
    }

    [TestMethod]
    public void FullOuterJoin_InCteClassifiedGroupedAndRanked_ShouldPreserveRowPresence()
    {
        const string query = (
            "\n"
            + "with joined as (\n"
            + "    select\n"
            + "        case\n"
            + "            when b is missing then 'LeftOnly'\n"
            + "            when a is missing then 'RightOnly'\n"
            + "            else 'Matched'\n"
            + "        end as State,\n"
            + "        a.Id as LeftId,\n"
            + "        b.Id as RightId\n"
            + "    from #A.entities() a\n"
            + "    full outer join #B.entities() b on a.Id = b.Id\n"
            + "), counts as (\n"
            + "    select State, Count(State) as Cnt,\n"
            + "           Max(LeftId) as SampleLeftId,\n"
            + "           Max(RightId) as SampleRightId\n"
            + "    from joined\n"
            + "    group by State\n"
            + ")\n"
            + "select State, Cnt, SampleLeftId, SampleRightId,\n"
            + "       RowNumber() over (order by Cnt desc, State) as Rank\n"
            + "from counts\n"
            + "order by Cnt desc, State"
        );
        var sources = CreateSources(
            [
                new BasicEntity("left-only") { Id = 1 },
                new BasicEntity("matched-1") { Id = 2 },
                new BasicEntity("matched-2") { Id = 2 },
                new BasicEntity("matched-3") { Id = 2 },
            ],
            [
                new BasicEntity("matched") { Id = 2 },
                new BasicEntity("right-only-1") { Id = 3 },
                new BasicEntity("right-only-2") { Id = 3 },
            ]
        );

        var table = Run(query, sources);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("State", typeof(string)),
            ("Cnt", typeof(long)),
            ("SampleLeftId", typeof(int?)),
            ("SampleRightId", typeof(int?)),
            ("Rank", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Matched", 3L, 2, 2, 1L],
            ["RightOnly", 2L, null, 3, 2L],
            ["LeftOnly", 1L, 1, null, 3L]
        );
    }
}
