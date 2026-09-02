using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class JoinInnerJoinTests : BasicEntityTestBase
{

    [TestMethod]
    public void SimpleJoinShorthandTest()
    {
        const string query = "select a.Id, b.Id from #A.entities() a join #B.entities() b on a.Id = b.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("x") { Id = 1 }, new BasicEntity("y") { Id = 2 }] },
            { "#B", [new BasicEntity("x") { Id = 2 }, new BasicEntity("z") { Id = 3 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Id", typeof(int)), ("b.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 2, 2 });
    }

    [TestMethod]
    public void SimpleJoinShorthandUppercaseTest()
    {
        const string query = "SELECT A.Id, B.Id FROM #A.entities() A JOIN #B.entities() B ON A.Id = B.Id";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("x") { Id = 1 }, new BasicEntity("y") { Id = 2 }] },
            { "#B", [new BasicEntity("x") { Id = 2 }, new BasicEntity("z") { Id = 3 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("A.Id", typeof(int)), ("B.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { 2, 2 });
    }

    [TestMethod]
    public void WhenSomeColumnsAreUsedAndNotEveryUsedTableHasUsedOwnColumns_MustNotThrow()
    {
        const string query = @"
select
    countries.Country
from #A.entities() countries
inner join #B.entities() cities on 1 = 1
inner join #C.entities() population on 1 = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [] },
            { "#B", [] },
            { "#C", [] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("countries.Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table);
    }

    [TestMethod]
    public void SimpleJoinTest()
    {
        var query =
            @"
select
    countries.Country,
    cities.City,
    population.Population
from #A.entities() countries
inner join #B.entities() cities on countries.Country = cities.Country
inner join #C.entities() population on cities.City = population.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Germany", "Berlin")
                ]
            },
            {
                "#B", [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin")
                ]
            },
            {
                "#C", [
                    new BasicEntity { City = "Krakow", Population = 400 },
                    new BasicEntity { City = "Wroclaw", Population = 500 },
                    new BasicEntity { City = "Warszawa", Population = 1000 },
                    new BasicEntity { City = "Gdansk", Population = 200 },
                    new BasicEntity { City = "Berlin", Population = 400 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("countries.Country", typeof(string)),
            ("cities.City", typeof(string)),
            ("population.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "Poland", "Krakow", 400m },
            new object?[] { "Poland", "Wroclaw", 500m },
            new object?[] { "Poland", "Warszawa", 1000m },
            new object?[] { "Poland", "Gdansk", 200m },
            new object?[] { "Germany", "Berlin", 400m });
    }

    [TestMethod]
    public void InnerJoinCteTablesTest()
    {
        var query = @"
with p as (
    select Country, City, Id from #A.entities()
), x as (
    select Country, City, Id from #B.entities()
)
select p.Id, x.Id from p inner join x on p.Country = x.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow") { Id = 0 },
                    new BasicEntity("Germany", "Berlin") { Id = 1 },
                    new BasicEntity("Russia", "Moscow") { Id = 2 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("Poland", "Krakow") { Id = 0 },
                    new BasicEntity("Poland", "Wroclaw") { Id = 1 },
                    new BasicEntity("Poland", "Warszawa") { Id = 2 },
                    new BasicEntity("Poland", "Gdansk") { Id = 3 },
                    new BasicEntity("Germany", "Berlin") { Id = 4 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("p.Id", typeof(int)), ("x.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { 0, 0 }, new object?[] { 0, 1 }, new object?[] { 0, 2 },
            new object?[] { 0, 3 }, new object?[] { 1, 4 });
    }

    [TestMethod]
    public void InnerJoinCteSelfJoinTest()
    {
        var query = @"
with p as (
    select Country, City, Id from #A.entities()
), x as (
    select Country, City, Id from p
)
select p.Id, x.Id from p p inner join x on p.Country = x.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow") { Id = 0 },
                    new BasicEntity("Germany", "Berlin") { Id = 1 },
                    new BasicEntity("Russia", "Moscow") { Id = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("p.Id", typeof(int)), ("x.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { 0, 0 }, new object?[] { 1, 1 }, new object?[] { 2, 2 });
    }

    [TestMethod]
    public void ComplexCteIssue1Test()
    {
        var query = @"
with p as (
	select
        Country
	from #A.entities()
), x as (
	select
		Country
	from p group by Country
)
select p.Country, x.Country from p inner join x on p.Country = x.Country where p.Country = 'Poland'
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow") { Id = 0 },
                    new BasicEntity("Germany", "Berlin") { Id = 1 },
                    new BasicEntity("Russia", "Moscow") { Id = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("p.Country", typeof(string)), ("x.Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Poland", "Poland" });
    }

    [TestMethod]
    public void ComplexCteIssue1WithGroupByTest()
    {
        var query = @"
with p as (
	select
        Country
	from #A.entities()
), x as (
	select
		Country
	from p group by Country
)
select p.Country, p.Count(p.Country) from p inner join x on p.Country = x.Country group by p.Country having p.Count(p.Country) > 1
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow") { Id = 0 },
                    new BasicEntity("Poland", "Krakow") { Id = 0 },
                    new BasicEntity("Germany", "Berlin") { Id = 1 },
                    new BasicEntity("Russia", "Moscow") { Id = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("p.Country", typeof(string)), ("p.Count(p.Country)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Poland", 2L });
    }

    [TestMethod]
    public void InnerJoinJoinPassMethodContextTest()
    {
        var query = @"
select
    a.ToDecimal(a.Id),
    b.ToDecimal(b.Id),
    c.ToDecimal(c.Id)
from #A.entities() a inner join #B.entities() b on 1 = 1 inner join #C.entities() c on 1 = 1";

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
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { 1m, 2m, 3m });
    }

    [TestMethod]
    public void WhenJoinedByMethodInvocations_ShouldRetrieveValues()
    {
        var query =
            @"
select
    countries.GetCountry(),
    cities.GetCity(),
    population.GetPopulation()
from #A.entities() countries
inner join #B.entities() cities on countries.GetCountry() = cities.GetCountry()
inner join #C.entities() population on cities.GetCity() = population.GetCity()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Germany", "Berlin")
                ]
            },
            {
                "#B", [
                    new BasicEntity("Poland", "Krakow"),
                    new BasicEntity("Poland", "Wroclaw"),
                    new BasicEntity("Poland", "Warszawa"),
                    new BasicEntity("Poland", "Gdansk"),
                    new BasicEntity("Germany", "Berlin")
                ]
            },
            {
                "#C", [
                    new BasicEntity { City = "Krakow", Population = 400 },
                    new BasicEntity { City = "Wroclaw", Population = 500 },
                    new BasicEntity { City = "Warszawa", Population = 1000 },
                    new BasicEntity { City = "Gdansk", Population = 200 },
                    new BasicEntity { City = "Berlin", Population = 400 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("countries.GetCountry()", typeof(string)),
            ("cities.GetCity()", typeof(string)),
            ("population.GetPopulation()", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "Poland", "Krakow", 400m },
            new object?[] { "Poland", "Wroclaw", 500m },
            new object?[] { "Poland", "Warszawa", 1000m },
            new object?[] { "Poland", "Gdansk", 200m },
            new object?[] { "Germany", "Berlin", 400m });
    }

    [TestMethod]
    public void WhenSelfJoined_ShouldRetrieveValues()
    {
        var query =
            @"
select
    countries.GetCountry(),
    cities.GetCity()
from #A.entities() countries
inner join #A.entities() cities on countries.Country = cities.Country
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
            ("countries.GetCountry()", typeof(string)),
            ("cities.GetCity()", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "Poland", "Krakow" }, new object?[] { "Germany", "Berlin" });
    }

    [TestMethod]
    public void WhenSelfJoined_WithMethodUsedModifyJoinedValues_ShouldPass()
    {
        var query =
            @"
select
    t.Country,
    t2.City
from #A.entities() t
inner join #A.entities() t2 on t.Trim(t.Country) = t2.Trim(t2.Country)
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity(" Poland ", " Krakow"),
                    new BasicEntity("Germany ", " Berlin")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("t.Country", typeof(string)), ("t2.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { " Poland ", " Krakow" },
            new object?[] { "Germany ", " Berlin" });
    }
}
