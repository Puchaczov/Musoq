using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void WhenAsOfJoinWithGreaterOrEqual_ShouldReturnClosestMatch()
    {
        var query = @"
select
    a.Name,
    a.Population,
    b.Name,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 50 },
                    new BasicEntity { Name = "A3", Population = 10 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 },
                    new BasicEntity { Name = "B2", Population = 40 },
                    new BasicEntity { Name = "B3", Population = 5 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("a.Population", typeof(decimal)),
            ("b.Name", typeof(string)),
            ("b.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A1", 100m, "B1", 90m],
            ["A2", 50m, "B2", 40m],
            ["A3", 10m, "B3", 5m]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithGreaterThan_ShouldReturnStrictlyLessMatch()
    {
        var query = @"
select
    a.Name,
    a.Population,
    b.Name,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population > b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 },
                    new BasicEntity { Name = "B2", Population = 50 },
                    new BasicEntity { Name = "B3", Population = 30 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("a.Population", typeof(decimal)),
            ("b.Name", typeof(string)),
            ("b.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A1", 100m, "B2", 50m],
            ["A2", 50m, "B3", 30m]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithLessThanOrEqual_ShouldReturnClosestGreaterOrEqual()
    {
        var query = @"
select
    a.Name,
    a.Population,
    b.Name,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population <= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 10 },
                    new BasicEntity { Name = "A2", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 20 },
                    new BasicEntity { Name = "B2", Population = 60 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("a.Population", typeof(decimal)),
            ("b.Name", typeof(string)),
            ("b.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A1", 10m, "B1", 20m],
            ["A2", 50m, "B2", 60m]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithLessThan_ShouldReturnClosestStrictlyGreater()
    {
        var query = @"
select
    a.Name,
    a.Population,
    b.Name,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population < b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 10 },
                    new BasicEntity { Name = "A2", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 10 },
                    new BasicEntity { Name = "B2", Population = 50 },
                    new BasicEntity { Name = "B3", Population = 60 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("a.Population", typeof(decimal)),
            ("b.Name", typeof(string)),
            ("b.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A1", 10m, "B2", 50m],
            ["A2", 50m, "B3", 60m]);
    }

    [TestMethod]
    public void WhenAsOfJoinNoMatch_ShouldReturnEmpty()
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
        TableMaterializationTestHelper.AssertRowsInOrder(table);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinNoMatch_ShouldReturnLeftWithNulls()
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
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A1", null]);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithMatch_ShouldReturnClosestMatch()
    {
        var query = @"
select
    a.Name,
    a.Population,
    b.Name,
    b.Population
from #A.entities() a
asof left join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 1 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 50 },
                    new BasicEntity { Name = "B2", Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("a.Population", typeof(decimal)),
            ("b.Name", typeof(string)),
            ("b.Population", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A1", 100m, "B1", 50m],
            ["A2", 1m, null, null]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithEqualityAndInequality_ShouldPartitionByEqualityKey()
    {
        var query = @"
select
    a.Name,
    a.Country,
    a.Population,
    b.Name,
    b.Country,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Country = b.Country and a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "US", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "UK", Population = 80 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "US", Population = 90 },
                    new BasicEntity { Name = "B2", Country = "US", Population = 50 },
                    new BasicEntity { Name = "B3", Country = "UK", Population = 70 },
                    new BasicEntity { Name = "B4", Country = "UK", Population = 30 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("a.Country", typeof(string)),
            ("a.Population", typeof(decimal)),
            ("b.Name", typeof(string)),
            ("b.Country", typeof(string)),
            ("b.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A1", "US", 100m, "B1", "US", 90m],
            ["A2", "UK", 80m, "B3", "UK", 70m]);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithEqualityNoMatch_ShouldReturnNulls()
    {
        var query = @"
select
    a.Name,
    a.Country,
    b.Name
from #A.entities() a
asof left join #B.entities() b on a.Country = b.Country and a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "US", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "FR", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "US", Population = 90 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("a.Country", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A1", "US", "B1"],
            ["A2", "FR", null]);
    }
}
