using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class FilterOnAggregateTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateCitySources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500),
                    new BasicEntity("Cracow", "Poland", 300),
                    new BasicEntity("Gdansk", "Poland", 100),
                    new BasicEntity("Berlin", "Germany", 400),
                    new BasicEntity("Munich", "Germany", 200),
                    new BasicEntity("Paris", "France", 600),
                    new BasicEntity("Lyon", "France", 150)
                ]
            }
        };
    }

    [TestMethod]
    public void WhenFilterOnCount_ShouldCountOnlyMatchingRows()
    {
        var query = "select Count(City) filter (where Population > 200) from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(4L, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFilterOnSum_ShouldSumOnlyMatchingRows()
    {
        var query = "select Sum(Population) filter (where Country = 'Poland') from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(900m, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFilterOnMin_ShouldMinOnlyMatchingRows()
    {
        var query = "select Min(Population) filter (where Country = 'Germany') from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(200m, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFilterOnMax_ShouldMaxOnlyMatchingRows()
    {
        var query = "select Max(Population) filter (where Country = 'France') from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(600m, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFilterOnAvg_ShouldAvgOnlyMatchingRows()
    {
        var query = "select Avg(Population) filter (where Country = 'Poland') from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(300m, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFilterWithGroupBy_ShouldFilterWithinEachGroup()
    {
        var query = "select Country, Count(City) filter (where Population > 200) from #A.Entities() group by Country";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "Poland" &&
            (long)row[1] == 2L
        ));

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "Germany" &&
            (long)row[1] == 1L
        ));

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "France" &&
            (long)row[1] == 1L
        ));
    }

    [TestMethod]
    public void WhenFilterWithGroupByAndSum_ShouldSumFilteredWithinEachGroup()
    {
        var query = "select Country, Sum(Population) filter (where Population > 200) from #A.Entities() group by Country";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "Poland" &&
            (decimal)row[1] == 800m
        ));

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "Germany" &&
            (decimal)row[1] == 400m
        ));

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "France" &&
            (decimal)row[1] == 600m
        ));
    }

    [TestMethod]
    public void WhenMultipleFiltersInSameSelect_ShouldApplyEachIndependently()
    {
        var query = @"select
            Count(City) filter (where Country = 'Poland'),
            Count(City) filter (where Country = 'Germany')
            from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3L, table[0].Values[0]);
        Assert.AreEqual(2L, table[0].Values[1]);
    }

    [TestMethod]
    public void WhenMixedFilteredAndUnfilteredAggregates_ShouldWorkCorrectly()
    {
        var query = @"select
            Count(City),
            Count(City) filter (where Population > 300)
            from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(7L, table[0].Values[0]);
        Assert.AreEqual(3L, table[0].Values[1]);
    }

    [TestMethod]
    public void WhenFilterMatchesNoRows_ShouldReturnZeroOrNull()
    {
        var query = "select Count(City) filter (where Population > 9999) from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0L, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFilterWithAndCondition_ShouldApplyBothConditions()
    {
        var query = "select Count(City) filter (where Population > 100 and Country = 'Poland') from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2L, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFilterWithOrCondition_ShouldApplyEitherCondition()
    {
        var query = "select Count(City) filter (where Country = 'Poland' or Country = 'France') from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5L, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFilterWithHaving_ShouldFilterThenHaving()
    {
        var query = @"select Country, Count(City) filter (where Population > 200)
                      from #A.Entities()
                      group by Country
                      having Count(City) filter (where Population > 200) >= 2";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Poland", table[0].Values[0]);
        Assert.AreEqual(2L, table[0].Values[1]);
    }

    [TestMethod]
    public void WhenFilterWithGroupByAndMixedAggregates_ShouldWorkCorrectly()
    {
        var query = @"select
            Country,
            Count(City) as TotalCities,
            Count(City) filter (where Population > 200) as LargeCities,
            Sum(Population) as TotalPop,
            Sum(Population) filter (where Population > 200) as LargePop
            from #A.Entities()
            group by Country";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "Poland" &&
            (long)row[1] == 3L &&
            (long)row[2] == 2L &&
            (decimal)row[3] == 900m &&
            (decimal)row[4] == 800m
        ));

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "Germany" &&
            (long)row[1] == 2L &&
            (long)row[2] == 1L &&
            (decimal)row[3] == 600m &&
            (decimal)row[4] == 400m
        ));

        Assert.IsTrue(table.Any(row =>
            (string)row[0] == "France" &&
            (long)row[1] == 2L &&
            (long)row[2] == 1L &&
            (decimal)row[3] == 750m &&
            (decimal)row[4] == 600m
        ));
    }

    [TestMethod]
    public void WhenFilterCaseInsensitive_ShouldWork()
    {
        var query = "select Count(City) FILTER (WHERE Population > 200) from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(4L, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFilterOnNonAggregateFunction_ShouldThrowMQ3051()
    {
        const string query = "select ToUpper(Name) filter (where Name = 'Alice') from #A.Entities()";

        var sources = CreateCitySources();

        var ex = Assert.Throws<MusoqQueryException>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });

        Assert.AreEqual(DiagnosticCode.MQ3051_FilterOnNonAggregate, ex.PrimaryEnvelope.Code);
    }
}
