using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class StarTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenStarUnfoldToMultipleColumns_AndExplicitColumnIsUsedWithinWhere_ShouldPass()
    {
        const string query = @"select * from #A.entities() a where a.Month = 'january'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("a.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].Values[5]);
    }

    [TestMethod]
    public void WhenMultipleStarsUnfoldToMultipleColumns_ShouldPass()
    {
        const string query = @"select *, * from #A.entities() a where a.Month = 'january'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(18, table.Columns.Count());

        Assert.AreEqual("a.Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual(50m, table[0].Values[4]);

        Assert.AreEqual("a.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].Values[5]);

        Assert.AreEqual("a.Money", table.Columns.ElementAt(13).ColumnName);
        Assert.AreEqual(50m, table[0].Values[13]);
    }

    [TestMethod]
    public void WhenStarUnfoldToMultipleColumns_AndExplicitColumnIsUsedAsAnotherColumn_ShouldPass()
    {
        const string query = @"select *, a.Month, Month from #A.entities() a where a.Month = 'january'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(11, table.Columns.Count());

        Assert.AreEqual("a.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].Values[5]);

        Assert.AreEqual("a.Month", table.Columns.ElementAt(9).ColumnName);
        Assert.AreEqual("january", table[0].Values[9]);

        Assert.AreEqual("Month", table.Columns.ElementAt(10).ColumnName);
        Assert.AreEqual("january", table[0].Values[10]);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_ShouldPass()
    {
        const string query = @"select a.* from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
        Assert.AreEqual(9, table.Columns.Count());
    }

    [TestMethod]
    public void WhenAliasedStarsUnfoldToMultipleColumns_ShouldPass()
    {
        const string query = @"select a.*, a.* from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
        Assert.AreEqual(18, table.Columns.Count());
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_AndExplicitColumnUsed_ShouldPass()
    {
        const string query = @"select a.*, a.Month from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
        Assert.AreEqual(10, table.Columns.Count());

        Assert.AreEqual("a.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("a.Month", table.Columns.ElementAt(9).ColumnName);
    }

    [TestMethod]
    public void WhenStarUnfoldToMultipleOfTwoTablesColumns_TwoTablesUsed_ShouldPass()
    {
        const string query = @"select * from #A.entities() a inner join #B.entities() b on a.Month = b.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                ]
            },
            {
                "#B", [
                    new BasicEntity("january", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(18, table.Columns.Count());

        Assert.AreEqual("a.Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual("a.Month", table.Columns.ElementAt(5).ColumnName);

        Assert.AreEqual("b.Money", table.Columns.ElementAt(13).ColumnName);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(14).ColumnName);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_TwoTablesUsed_ShouldPass()
    {
        const string query = @"select a.*, b.* from #A.entities() a inner join #B.entities() b on a.Month = b.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                ]
            },
            {
                "#B", [
                    new BasicEntity("january", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(18, table.Columns.Count());

        Assert.AreEqual("a.Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual("a.Month", table.Columns.ElementAt(5).ColumnName);

        Assert.AreEqual("b.Money", table.Columns.ElementAt(13).ColumnName);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(14).ColumnName);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_FirstTableUnfolded_ShouldPass()
    {
        const string query = @"select a.* from #A.entities() a inner join #B.entities() b on a.Month = b.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                ]
            },
            {
                "#B", [
                    new BasicEntity("january", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("a.Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual("a.Month", table.Columns.ElementAt(5).ColumnName);

        Assert.AreEqual(50m, table[0].Values[4]);
        Assert.AreEqual("january", table[0].Values[5]);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_SecondTableUnfolded_ShouldPass()
    {
        const string query = @"select b.* from #A.entities() a inner join #B.entities() b on a.Month = b.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                ]
            },
            {
                "#B", [
                    new BasicEntity("january", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("b.Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(5).ColumnName);

        Assert.AreEqual(150m, table[0].Values[4]);
        Assert.AreEqual("january", table[0].Values[5]);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_AndExplicitColumnUsed_TwoTablesUsed_ShouldPass()
    {
        const string query =
            @"select a.*, a.Month, b.*, b.Month from #A.entities() a inner join #B.entities() b on a.Month = b.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m)
                ]
            },
            {
                "#B", [
                    new BasicEntity("january", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(20, table.Columns.Count());

        Assert.AreEqual("a.Money", table.Columns.ElementAt(4).ColumnName);
        Assert.AreEqual("a.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("a.Month", table.Columns.ElementAt(9).ColumnName);

        Assert.AreEqual("b.Money", table.Columns.ElementAt(14).ColumnName);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(15).ColumnName);
        Assert.AreEqual("b.Month", table.Columns.ElementAt(19).ColumnName);

        Assert.AreEqual(50m, table[0].Values[4]);
        Assert.AreEqual("january", table[0].Values[5]);

        Assert.AreEqual(150m, table[0].Values[14]);
        Assert.AreEqual("january", table[0].Values[15]);

        Assert.AreEqual("january", table[0].Values[9]);
        Assert.AreEqual("january", table[0].Values[19]);
    }

    [TestMethod]
    public void WhenStarUnfoldToMultipleColumns_AndStarIsUsedWithinSelect_ShouldPass()
    {
        const string query = @"with p as (select * from #A.entities() a where a.Month = 'january') select * from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("a.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].Values[5]);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_AndStarIsUsedWithinSelect_ShouldPass()
    {
        const string query = @"with p as (select a.* from #A.entities() a where a.Month = 'january') select * from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("a.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].Values[5]);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_AndAliasedStarIsUsedWithinSelect_ShouldPass()
    {
        const string query = @"with p as (select a.* from #A.entities() a where a.Month = 'january') select p.* from p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("a.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].Values[5]);
    }

    [TestMethod]
    public void WhenAliasedStarUnfoldToMultipleColumns_AndAliasedStarIsUsedWithinAliasedFrom_ShouldPass()
    {
        const string query =
            @"with p as (select a.* from #A.entities() a where a.Month = 'january') select p.* from p p";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("january", 50m),
                    new BasicEntity("february", 100m),
                    new BasicEntity("march", 150m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());

        Assert.AreEqual("p.a.Month", table.Columns.ElementAt(5).ColumnName);
        Assert.AreEqual("january", table[0].Values[5]);
    }
}