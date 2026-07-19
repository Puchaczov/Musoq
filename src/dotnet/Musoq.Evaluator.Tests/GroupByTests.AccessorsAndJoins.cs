using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class GroupByTests
{

    [TestMethod]
    public void GroupBySubstrTest()
    {
        var query =
            @"select Substring(Name, 0, 2), Count(Substring(Name, 0, 2)) from #A.Entities() group by Substring(Name, 0, 2)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("AA:1"),
                    new BasicEntity("AA:2"),
                    new BasicEntity("AA:3"),
                    new BasicEntity("BB:1"),
                    new BasicEntity("BB:2"),
                    new BasicEntity("CC:1")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("Substring(Name, 0, 2)", typeof(string)),
            ("Count(Substring(Name, 0, 2))", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "AA", 3L }, new object?[] { "BB", 2L }, new object?[] { "CC", 1L });
    }

    [TestMethod]
    public void GroupByWithSelectedConstantModifiedByFunctionTest()
    {
        var query =
            @"select Name, Count(Name), Inc(10d), 1 from #A.Entities() group by Name having Count(Name) >= 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("ABBA"),
                    new BasicEntity("BABBA"),
                    new BasicEntity("CECCA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("Name", typeof(string)), ("Count(Name)", typeof(long)),
            ("Inc(10)", typeof(decimal)), ("1", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "ABBA", 3L, 11m, 1 },
            new object?[] { "BABBA", 2L, 11m, 1 });
    }

    [TestMethod]
    public void GroupByColumnSubstringTest()
    {
        var query =
            """
            select
                Country,
                Substring(City, IndexOf(City, ':')) as 'City',
                Count(City) as 'Count',
                Sum(Population) as 'Sum'
            from #A.Entities()
            group by Substring(City, IndexOf(City, ':')), Country
            """;

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW:TARGOWEK", "POLAND", 500),
                    new BasicEntity("WARSAW:URSYNOW", "POLAND", 500),
                    new BasicEntity("KATOWICE:ZAWODZIE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("Country", typeof(string)), ("City", typeof(string)),
            ("Count", typeof(long)), ("Sum", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "POLAND", "WARSAW", 2L, 1000m },
            new object?[] { "POLAND", "KATOWICE", 1L, 250m });
    }

    [TestMethod]
    public void GroupBySimpleAccessTest()
    {
        var query = @"select Month from #A.Entities() group by Month";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("cracow", "jan", -200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { "jan" });
    }

    [TestMethod]
    public void GroupByComplexObjectAccessTest()
    {
        var query = @"select Self.Month from #A.Entities() group by Self.Month";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("cracow", "jan", -200m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Self.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { "jan" });
    }

    [TestMethod]
    public void GroupByComplexObjectAccessWithSumTest()
    {
        var query = @"select Self.Month, Sum(Self.Money) from #A.Entities() group by Self.Month";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("cracow", "jan", -200m),
                    new BasicEntity("cracow", "feb", 100m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("Self.Month", typeof(string)), ("Sum(Self.Money)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "jan", 500m }, new object?[] { "feb", 100m });
    }

    [TestMethod]
    public void GroupByWithCaseWhenInSelectTest()
    {
        var query =
            @"select (case when Self.Month = 'jan' then 'JANUARY' when Self.Month = 'feb' then 'FEBRUARY' else 'NONE' end) as MonthName from #A.Entities() group by Self.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("cracow", "jan", -200m),
                    new BasicEntity("cracow", "feb", 100m),
                    new BasicEntity("cracow", "march", 100m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("MonthName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "JANUARY" }, new object?[] { "FEBRUARY" }, new object?[] { "NONE" });
    }

    [TestMethod]
    public void GroupByWithCaseWhenAsGroupingResultFunctionTest()
    {
        var query =
            @"select (case when e.Month = e.Month then e.Month else '' end), Count(case when e.Month = e.Month then e.Month else '' end) from #A.Entities() e group by (case when e.Month = e.Month then e.Month else '' end)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("cracow", "jan", -200m),
                    new BasicEntity("cracow", "feb", 100m),
                    new BasicEntity("cracow", "march", 100m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("case when e.Month = e.Month then e.Month else  end", typeof(string)),
            ("Count(case when e.Month = e.Month then e.Month else  end)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "jan", 3L }, new object?[] { "feb", 1L }, new object?[] { "march", 1L });
    }

    [TestMethod]
    public void GroupByWithExplicitGroupedExpressionTest()
    {
        var query =
            @"select (case when e.Month = e.Month then e.Month else '' end) as monthKey, Count((case when e.Month = e.Month then e.Month else '' end)) as monthCount, 'fake' as constantKey from #A.Entities() e group by (case when e.Month = e.Month then e.Month else '' end), 'fake'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("cracow", "jan", -200m),
                    new BasicEntity("cracow", "feb", 100m),
                    new BasicEntity("cracow", "march", 100m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("monthKey", typeof(string)), ("monthCount", typeof(long)), ("constantKey", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "jan", 3L, "fake" }, new object?[] { "feb", 1L, "fake" },
            new object?[] { "march", 1L, "fake" });
    }

    [TestMethod]
    public void GroupByWithExplicitGroupedExpressionAndCustomColumnNamingTest()
    {
        var query =
            @"select (case when e.Month = e.Month then e.Month else '' end) as firstColumn, Count((case when e.Month = e.Month then e.Month else '' end)) as secondColumn, 'fake' as thirdColumn from #A.Entities() e group by (case when e.Month = e.Month then e.Month else '' end), 'fake'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("czestochowa", "jan", 400m),
                    new BasicEntity("katowice", "jan", 300m),
                    new BasicEntity("cracow", "jan", -200m),
                    new BasicEntity("cracow", "feb", 100m),
                    new BasicEntity("cracow", "march", 100m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("firstColumn", typeof(string)), ("secondColumn", typeof(long)), ("thirdColumn", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "jan", 3L, "fake" }, new object?[] { "feb", 1L, "fake" },
            new object?[] { "march", 1L, "fake" });
    }

    [TestMethod]
    public void WhenGroupByUsedWithJoinsByMethodInvocation_ShouldRetrieveValues()
    {
        var query =
            @"
select
    countries.GetCountry() as Country,
    population.Sum(population.GetPopulation()) as Population
from #A.entities() countries
inner join #B.entities() cities on countries.GetCountry() = cities.GetCountry()
inner join #C.entities() population on cities.GetCity() = population.GetCity()
group by countries.GetCountry()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Germany" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Country = "Poland", City = "Krakow" },
                    new BasicEntity { Country = "Poland", City = "Wroclaw" },
                    new BasicEntity { Country = "Poland", City = "Warszawa" },
                    new BasicEntity { Country = "Poland", City = "Gdansk" },
                    new BasicEntity { Country = "Germany", City = "Berlin" }
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
            ("Country", typeof(string)), ("Population", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "Poland", 2100m }, new object?[] { "Germany", 400m });
    }

    [TestMethod]
    public void WhenGroupByWithWhereUsed_WhereUsesFieldThatWillBeUsedInResultingTable_ShouldSuccess()
    {
        var query = @"
select
    a.Country,
    b.AggregateValues(b.City) as Cities
from #A.entities() a
inner join #B.entities() b on a.Country = b.Country
where a.Country = 'Poland'
group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { City = "Warsaw", Country = "Poland" },
                    new BasicEntity { City = "Gdansk", Country = "Poland" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Country", typeof(string)), ("Cities", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Poland", "Warsaw,Gdansk" });
    }

    [TestMethod]
    public void WhenGroupByWithWhereUsed_WhereUsesFieldThatWontBeUsedInResultingTable_ShouldSuccess()
    {
        var query = @"
select
    a.Country,
    b.AggregateValues(b.City) as Cities
from #A.entities() a
inner join #B.entities() b on a.Country = b.Country
where b.Population > 200
group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { City = "Warsaw", Country = "Poland", Population = 200 },
                    new BasicEntity { City = "Gdansk", Country = "Poland", Population = 300 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Country", typeof(string)), ("Cities", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            new object?[] { "Poland", "Gdansk" });
    }

    [TestMethod]
    public void WhenAccessingTheFirstLetterWithMethodCallInsideAggregation_ShouldSucceed()
    {
        var query = @"
select
    a.Country,
    AggregateValues(GetElementAt(a.Country, 0)) as FirstLetter
from #A.entities() a
group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Brazil" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Country", typeof(string)), ("FirstLetter", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "Poland", "P" }, new object?[] { "Brazil", "B" });
    }

    [TestMethod]
    public void WhenAccessingTheFirstLetterWithIndexerInsideAggregation_ShouldSucceed()
    {
        var query = @"
select
    a.Country,
    AggregateValues(a.Country[0]) as FirstLetter
from #A.entities() a
group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Brazil" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table,
            ("a.Country", typeof(string)), ("FirstLetter", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "Poland", "P" }, new object?[] { "Brazil", "B" });
    }

}
