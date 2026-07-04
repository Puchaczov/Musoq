using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void WhenAsOfJoinExactMatch_ShouldReturnExactMatch()
    {
        var query = @"
select
    a.Name,
    b.Name,
    a.Population,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 50 },
                    new BasicEntity { Name = "B2", Population = 30 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)),
            ("a.Population", typeof(decimal)),
            ("b.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1", "B1", 50m, 50m]);
    }

    [TestMethod]
    public void WhenAsOfLeftOuterJoin_ShouldWorkSameAsAsOfLeftJoin()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof left outer join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 1 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { "A1", null });
    }

    [TestMethod]
    public void WhenAsOfJoinEmptyRight_ShouldReturnEmpty()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", Array.Empty<BasicEntity>()
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinEmptyRight_ShouldReturnLeftWithNulls()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof left join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", Array.Empty<BasicEntity>()
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { "A1", null });
    }

}
