using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class JoinOuterJoinTests : BasicEntityTestBase
{

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

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Id", typeof(int)), ("b.Id", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 1, null });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Id", typeof(int)), ("b.Id", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { 1, null }, new object?[] { 2, 2 });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("A.Id", typeof(int)), ("B.Id", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { 1, null }, new object?[] { 2, 2 });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Id", typeof(int)), ("b.Id", typeof(int?)), ("c.Id", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 1, null, null });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Id", typeof(int)), ("b.Id", typeof(int?)), ("c.Id", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 1, null, null });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Id", typeof(int)), ("b.Id", typeof(int?)), ("c.Id", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { 1, 1, 1 }, new object?[] { 2, null, null });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Id", typeof(int?)), ("b.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null, 1 });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Id", typeof(int?)), ("b.Id", typeof(int?)), ("c.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { 1, 1, 1 }, new object?[] { null, null, 2 });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.ToDecimal(a.Id)", typeof(decimal?)),
            ("b.ToDecimal(b.Id)", typeof(decimal?)),
            ("c.ToDecimal(c.Id)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 1m, 2m, 3m });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.ToDecimal(a.Id)", typeof(decimal?)),
            ("b.ToDecimal(b.Id)", typeof(decimal?)),
            ("c.ToDecimal(c.Id)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 1m, 2m, 3m });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.ToDecimal(a.Id)", typeof(decimal?)),
            ("b.ToDecimal(b.Id)", typeof(decimal?)),
            ("c.ToDecimal(c.Id)", typeof(decimal?)),
            ("d.ToDecimal(d.Id)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 1m, 2m, 3m, 4m });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.ToDecimal(a.Id)", typeof(decimal?)),
            ("b.ToDecimal(b.Id)", typeof(decimal?)),
            ("c.ToDecimal(c.Id)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 1m, 2m, 3m });
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.ToDecimal(a.Id)", typeof(decimal?)),
            ("b.ToDecimal(b.Id)", typeof(decimal?)),
            ("c.ToDecimal(c.Id)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 1m, 2m, 3m });
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
        a.Country as LeftCountry,
        b.Country as RightCountry
    from first a left outer join second b on a.Country = b.Country
)
select LeftCountry, RightCountry from third
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("LeftCountry", typeof(string)), ("RightCountry", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "Poland", "Poland" }, new object?[] { "Germany", "Germany" });
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
        a.Country as LeftCountry,
        b.Country as RightCountry
    from first a right outer join second b on a.Country = b.Country
)
select LeftCountry, RightCountry from third
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

        TableMaterializationTestHelper.AssertColumns(table,
            ("LeftCountry", typeof(string)), ("RightCountry", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "Poland", "Poland" }, new object?[] { "Germany", "Germany" });
    }
}
