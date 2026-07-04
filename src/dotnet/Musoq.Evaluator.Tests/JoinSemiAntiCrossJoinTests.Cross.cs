using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class JoinSemiAntiCrossJoinTests
{
    [TestMethod]
    public void CrossJoin_WhenFiltered_ShouldProduceCartesianRowsBeforeWhere()
    {
        var query = @"
select a.Name, b.Name
from #A.entities() a
cross join #B.entities() b
where b.Id = 1";
        var table = RunJoinQuery(query);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A1", "B1"],
            ["A1", "B1Duplicate"],
            ["A2", "B1"],
            ["A2", "B1Duplicate"],
            ["A3", "B1"],
            ["A3", "B1Duplicate"]);
    }

    [TestMethod]
    public void CrossJoin_WhenRightSideIsEmpty_ShouldReturnNoRows()
    {
        var sources = CreateJoinSources([]);
        var table = CreateAndRunVirtualMachine<BasicEntity>(
            "select a.Name from #A.entities() a cross join #B.entities() b",
            sources).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void CrossJoin_WhenFollowedByInnerJoin_ShouldPreserveAliasesAndRows()
    {
        var query = @"
select a.Name, b.Name, c.Name
from #A.entities() a
cross join #B.entities() b
join #C.entities() c on b.Id = c.Id
where a.Id = 1";
        var table = CreateAndRunVirtualMachine<BasicEntity>(query, CreateThreeWayJoinSources()).Run(TokenSource.Token);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)),
            ("c.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A1", "B1", "C1"],
            ["A1", "B1Duplicate", "C1"],
            ["A1", "B3", "C3"]);
    }
}
