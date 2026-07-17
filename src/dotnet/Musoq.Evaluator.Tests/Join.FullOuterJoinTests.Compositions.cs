using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class JoinFullOuterJoinTests
{
    [TestMethod]
    public void FullOuterJoin_InCteClassifiedGroupedAndRanked_ShouldPreserveRowPresence()
    {
        const string query = @"
with joined as (
    select
        case
            when b is missing then 'LeftOnly'
            when a is missing then 'RightOnly'
            else 'Matched'
        end as State,
        a.Id as LeftId,
        b.Id as RightId
    from #A.entities() a
    full outer join #B.entities() b on a.Id = b.Id
), counts as (
    select State, Count(State) as Cnt,
           Max(LeftId) as SampleLeftId,
           Max(RightId) as SampleRightId
    from joined
    group by State
)
select State, Cnt, SampleLeftId, SampleRightId,
       RowNumber() over (order by Cnt desc, State) as Rank
from counts
order by Cnt desc, State";
        var sources = CreateSources(
            [
                new BasicEntity("left-only") { Id = 1 },
                new BasicEntity("matched-1") { Id = 2 },
                new BasicEntity("matched-2") { Id = 2 },
                new BasicEntity("matched-3") { Id = 2 }
            ],
            [
                new BasicEntity("matched") { Id = 2 },
                new BasicEntity("right-only-1") { Id = 3 },
                new BasicEntity("right-only-2") { Id = 3 }
            ]);

        var table = Run(query, sources);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("State", typeof(string)),
            ("Cnt", typeof(long)),
            ("SampleLeftId", typeof(int?)),
            ("SampleRightId", typeof(int?)),
            ("Rank", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Matched", 3L, 2, 2, 1L],
            ["RightOnly", 2L, null, 3, 2L],
            ["LeftOnly", 1L, 1, null, 3L]);
    }
}
