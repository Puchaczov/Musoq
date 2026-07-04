using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void WhenAsOfJoinWithDateTimeColumn_ShouldMatchByTime()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof join #B.entities() b on a.Time >= b.Time";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Error1", Time = new DateTime(2025, 1, 15, 14, 30, 0) },
                    new BasicEntity { Name = "Error2", Time = new DateTime(2025, 1, 15, 10, 0, 0) }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Commit1", Time = new DateTime(2025, 1, 15, 14, 0, 0) },
                    new BasicEntity { Name = "Commit2", Time = new DateTime(2025, 1, 15, 9, 0, 0) },
                    new BasicEntity { Name = "Commit3", Time = new DateTime(2025, 1, 14, 12, 0, 0) }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Error1", "Commit1"],
            ["Error2", "Commit2"]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithStringColumn_ShouldMatchLexicographically()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof join #B.entities() b on a.City >= b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", City = "M" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", City = "A" },
                    new BasicEntity { Name = "B2", City = "K" },
                    new BasicEntity { Name = "B3", City = "Z" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1", "B2"]);
    }
}
