using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class Join_OuterJoinTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void SimpleLeftJoinTest()
    {
        var query = "select a.Id, b.Id from #A.entities() a left outer join #B.entities() b on a.Id = b.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#B",
                [
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual("a.Id", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual(1, column.ColumnIndex);
        Assert.AreEqual("b.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        Assert.AreEqual(1, table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void SimpleLeftJoinShorthandTest()
    {
        const string query = "select a.Id, b.Id from #A.entities() a left join #B.entities() b on a.Id = b.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("xX") { Id = 1 }, new BasicEntity("yY") { Id = 2 }] },
            { "#B", [new BasicEntity("xX") { Id = 2 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        var rows = new HashSet<(int?, int?)>
        {
            ((int?)table[0][0], (int?)table[0][1]),
            ((int?)table[1][0], (int?)table[1][1])
        };

        Assert.Contains((1, null), rows, "Expected row (1, null) not found");
        Assert.Contains((2, 2), rows, "Expected row (2, 2) not found");
    }

    [TestMethod]
    public void SimpleLeftJoinShorthandUppercaseTest()
    {
        const string query = "SELECT A.Id, B.Id FROM #A.entities() A LEFT JOIN #B.entities() B ON A.Id = B.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("xX") { Id = 1 }, new BasicEntity("yY") { Id = 2 }] },
            { "#B", [new BasicEntity("xX") { Id = 2 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        var rows = new HashSet<(int?, int?)>
        {
            ((int?)table[0][0], (int?)table[0][1]),
            ((int?)table[1][0], (int?)table[1][1])
        };

        Assert.Contains((1, null), rows, "Expected row (1, null) not found");
        Assert.Contains((2, 2), rows, "Expected row (2, 2) not found");
    }

    [TestMethod]
    public void MultipleLeftJoinTest()
    {
        const string query =
            "select a.Id, b.Id, c.Id from #A.entities() a left outer join #B.entities() b on a.Id = b.Id left outer join #B.entities() c on b.Id = c.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#B",
                [
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, table.Columns.Count());

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual("a.Id", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual(1, column.ColumnIndex);
        Assert.AreEqual("b.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        column = table.Columns.ElementAt(2);
        Assert.AreEqual(2, column.ColumnIndex);
        Assert.AreEqual("c.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        Assert.AreEqual(1, table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.IsNull(table[0][2]);
    }

    [TestMethod]
    public void MultipleLeftJoinWithCTriesMatchBButFailTest()
    {
        var query =
            "select a.Id, b.Id, c.Id from #A.entities() a left outer join #B.entities() b on a.Id = b.Id left outer join #C.entities() c on b.Id = c.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#B",
                [
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("xX") { Id = 1 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, table.Columns.Count());

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual("a.Id", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual(1, column.ColumnIndex);
        Assert.AreEqual("b.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        column = table.Columns.ElementAt(2);
        Assert.AreEqual(2, column.ColumnIndex);
        Assert.AreEqual("c.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        Assert.AreEqual(1, table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.IsNull(table[0][2]);
    }

    [TestMethod]
    public void MultipleLeftJoinWithCTriesMatchBAndSucceedTest()
    {
        var query =
            "select a.Id, b.Id, c.Id from #A.entities() a left outer join #B.entities() b on a.Id = b.Id left outer join #C.entities() c on b.Id = c.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX") { Id = 1 },
                    new BasicEntity("xX") { Id = 2 }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("xX") { Id = 1 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(3, table.Columns.Count());

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual("a.Id", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual(1, column.ColumnIndex);
        Assert.AreEqual("b.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        column = table.Columns.ElementAt(2);
        Assert.AreEqual(2, column.ColumnIndex);
        Assert.AreEqual("c.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (int)entry[0] == 1 &&
            (int)entry[1] == 1 &&
            (int)entry[2] == 1
        ), "First entry should be 1, 1, 1");

        Assert.IsTrue(table.Any(entry =>
            (int)entry[0] == 2 &&
            entry[1] == null &&
            entry[2] == null
        ), "Second entry should be 2, null, null");
    }

    [TestMethod]
    public void SimpleRightJoinTest()
    {
        var query = "select a.Id, b.Id from #A.entities() a right outer join #B.entities() b on a.Id = b.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("xX") { Id = 1 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table.Columns.Count());

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual("a.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual(1, column.ColumnIndex);
        Assert.AreEqual("b.Id", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        Assert.IsNull(table[0][0]);
        Assert.AreEqual(1, table[0][1]);
    }

    [TestMethod]
    public void MultipleRightJoinWithCTriesMatchBAndSucceedForASingleTest()
    {
        var query =
            "select a.Id, b.Id, c.Id from #A.entities() a right outer join #B.entities() b on a.Id = b.Id right outer join #C.entities() c on b.Id = c.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("xX") { Id = 1 },
                    new BasicEntity("xX") { Id = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(3, table.Columns.Count());

        var column = table.Columns.ElementAt(0);
        Assert.AreEqual(0, column.ColumnIndex);
        Assert.AreEqual("a.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        column = table.Columns.ElementAt(1);
        Assert.AreEqual(1, column.ColumnIndex);
        Assert.AreEqual("b.Id", column.ColumnName);
        Assert.AreEqual(typeof(int?), column.ColumnType);

        column = table.Columns.ElementAt(2);
        Assert.AreEqual(2, column.ColumnIndex);
        Assert.AreEqual("c.Id", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry =>
            (int?)entry[0] == 1 &&
            (int?)entry[1] == 1 &&
            (int?)entry[2] == 1
        ), "First entry should be 1, 1, 1");

        Assert.IsTrue(table.Any(entry =>
            entry[0] == null &&
            entry[1] == null &&
            (int?)entry[2] == 2
        ), "Second entry should be null, null, 2");
    }

    [TestMethod]
    public void RightOuterJoinPassMethodContextTest()
    {
        var query =
            "select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id) from #A.entities() a right outer join #B.entities() b on 1 = 1 right outer join #C.entities() c on 1 = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("xX") { Id = 2 }
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("xX") { Id = 3 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1m, table[0][0]);
        Assert.AreEqual(2m, table[0][1]);
        Assert.AreEqual(3m, table[0][2]);
    }

    [TestMethod]
    public void LeftOuterJoinPassMethodContextTest()
    {
        var query = @"
select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id)
from #A.entities() a
left outer join #B.entities() b on 1 = 1
left outer join #C.entities() c on 1 = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("xX") { Id = 2 }
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("xX") { Id = 3 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1m, table[0][0]);
        Assert.AreEqual(2m, table[0][1]);
        Assert.AreEqual(3m, table[0][2]);
    }

    [TestMethod]
    public void LeftOuterJoinWithFourOtherJoinsTest()
    {
        var query = @"
select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id), d.ToDecimal(d.Id)
from #A.entities() a
left outer join #B.entities() b on 1 = 1
left outer join #C.entities() c on 1 = 1
left outer join #D.entities() d on 1 = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("xX") { Id = 2 }
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("xX") { Id = 3 }
                ]
            },
            {
                "#D",
                [
                    new BasicEntity("xX") { Id = 4 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1m, table[0][0]);
        Assert.AreEqual(2m, table[0][1]);
        Assert.AreEqual(3m, table[0][2]);
        Assert.AreEqual(4m, table[0][3]);
    }

    [TestMethod]
    public void LeftOuterRightOuterJoinPassMethodContextTest()
    {
        var query =
            "select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id) from #A.entities() a left outer join #B.entities() b on 1 = 1 right outer join #C.entities() c on 1 = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("xX") { Id = 2 }
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("xX") { Id = 3 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1m, table[0][0]);
        Assert.AreEqual(2m, table[0][1]);
        Assert.AreEqual(3m, table[0][2]);
    }

    [TestMethod]
    public void RightOuterLeftOuterJoinPassMethodContextTest()
    {
        var query =
            "select a.ToDecimal(a.Id), b.ToDecimal(b.Id), c.ToDecimal(c.Id) from #A.entities() a right outer join #B.entities() b on 1 = 1 left outer join #C.entities() c on 1 = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("xX") { Id = 1 }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("xX") { Id = 2 }
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("xX") { Id = 3 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1m, table[0][0]);
        Assert.AreEqual(2m, table[0][1]);
        Assert.AreEqual(3m, table[0][2]);
    }

    [TestMethod]
    public void WhenMultipleAliasesAroundCteQuery_LeftOuterJoin_ShouldRetrieveValues()
    {
        var query =
            @"
with first as (
    select a.Country as Country from #A.entities() a
), second as (
    select a.Country as Country from #A.entities() a
), third as (
    select
        a.Country,
        b.Country
    from first a left outer join second b on a.Country = b.Country
)
select * from third
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Germany", "Berlin")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("a.Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual("b.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(entry =>
                (string)entry[0] == "Poland" &&
                (string)entry[1] == "Poland"),
            "First entry should be Poland");

        Assert.IsTrue(table.Any(entry =>
                (string)entry[0] == "Germany" &&
                (string)entry[1] == "Germany"),
            "Second entry should be Germany");
    }

    [TestMethod]
    public void WhenMultipleAliasesAroundCteQuery_RightOuterJoin_ShouldRetrieveValues()
    {
        var query =
            @"
with first as (
    select a.Country as Country from #A.entities() a
), second as (
    select a.Country as Country from #A.entities() a
), third as (
    select
        a.Country,
        b.Country
    from first a right outer join second b on a.Country = b.Country
)
select * from third
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Germany", "Berlin")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("a.Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual("b.Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row[0] == "Poland" &&
                (string)row[1] == "Poland"),
            "Expected row with Poland in both columns");

        Assert.IsTrue(table.Any(row =>
                (string)row[0] == "Germany" &&
                (string)row[1] == "Germany"),
            "Expected row with Germany in both columns");
    }
}
