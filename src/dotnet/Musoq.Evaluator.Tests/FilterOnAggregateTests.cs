using System.Collections.Generic;
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
        var query = "select Count(City) filter (where Population > 200) as C from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [4L]);
    }

    [TestMethod]
    public void WhenFilterOnCountWildcard_ShouldCountOnlyMatchingRows()
    {
        var query = "select Count(*) filter (where Population > 200) as C from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [4L]);
    }

    [TestMethod]
    public void WhenFilterOnCountWithoutArguments_ShouldCountOnlyMatchingRows()
    {
        var query = "select Count() filter (where Population > 200) as C from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [4L]);
    }

    [TestMethod]
    public void WhenFilterOnCustomArgumentlessAggregate_ShouldFilterWithoutParserCountRewrite()
    {
        var query = "select CustomRowCount() filter (where Country = 'Germany') as C from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2L]);
    }

    [TestMethod]
    public void WhenCustomArgumentlessAggregateUsesWildcard_ShouldResolveAsArgumentlessAggregate()
    {
        var query = "select CustomRowCount(*) as C from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [7L]);
    }

    [TestMethod]
    public void WhenCustomArgumentlessAggregateUsesWildcardWithFilter_ShouldFilterRows()
    {
        var query = "select CustomRowCount(*) filter (where Country = 'France') as C from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2L]);
    }

    [TestMethod]
    public void WhenValueAggregateUsesWildcardWithoutArgumentlessOverload_ShouldRemainInvalid()
    {
        var query = "select Sum(*) as Total from #A.Entities()";
        var sources = CreateCitySources();

        Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));
    }

    [TestMethod]
    public void WhenFilterOnCountWildcardWithGroupBy_ShouldFilterWithinEachGroup()
    {
        var query = "select Country, Count(*) filter (where Population > 200) as C from #A.Entities() group by Country";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", 2L],
            ["Germany", 1L],
            ["France", 1L]);
    }

    [TestMethod]
    public void WhenFilterOnCountWildcardMatchesNoRows_ShouldReturnZero()
    {
        var query = "select Count(*) filter (where Population > 9999) as C from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [0L]);
    }

    [TestMethod]
    public void WhenEquivalentCountFilterForms_ShouldReturnSameCount()
    {
        var query = @"select
            Count() filter (where Population > 200) as CountNoArgs,
            Count(*) filter (where Population > 200) as CountStar,
            Count(City) filter (where Population > 200) as CountCity
            from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("CountNoArgs", typeof(long)),
            ("CountStar", typeof(long)),
            ("CountCity", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [4L, 4L, 4L]);
    }

    [TestMethod]
    public void WhenFilterOnCountDistinct_ShouldCountDistinctMatchingValues()
    {
        var query = "select Count(distinct Country) filter (where Population > 200) as C from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [3L]);
    }

    [TestMethod]
    public void WhenFilterOnSum_ShouldSumOnlyMatchingRows()
    {
        var query = "select Sum(Population) filter (where Country = 'Poland') as Total from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [900m]);
    }

    [TestMethod]
    public void WhenFilterOnMin_ShouldMinOnlyMatchingRows()
    {
        var query = "select Min(Population) filter (where Country = 'Germany') as Minimum from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Minimum", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [200m]);
    }

    [TestMethod]
    public void WhenFilterOnMax_ShouldMaxOnlyMatchingRows()
    {
        var query = "select Max(Population) filter (where Country = 'France') as Maximum from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Maximum", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [600m]);
    }

    [TestMethod]
    public void WhenFilterOnAvg_ShouldAvgOnlyMatchingRows()
    {
        var query = "select Avg(Population) filter (where Country = 'Poland') as Average from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Average", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [300m]);
    }

    [TestMethod]
    public void WhenFilterWithGroupBy_ShouldFilterWithinEachGroup()
    {
        var query = "select Country, Count(City) filter (where Population > 200) as C from #A.Entities() group by Country";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", 2L],
            ["Germany", 1L],
            ["France", 1L]);
    }

    [TestMethod]
    public void WhenFilterWithGroupByAndSum_ShouldSumFilteredWithinEachGroup()
    {
        var query = "select Country, Sum(Population) filter (where Population > 200) as S from #A.Entities() group by Country";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("S", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", 800m],
            ["Germany", 400m],
            ["France", 600m]);
    }

    [TestMethod]
    public void WhenMultipleFiltersInSameSelect_ShouldApplyEachIndependently()
    {
        var query = @"select
            Count(City) filter (where Country = 'Poland') as PolandCount,
            Count(City) filter (where Country = 'Germany') as GermanyCount
            from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("PolandCount", typeof(long)),
            ("GermanyCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [3L, 2L]);
    }

    [TestMethod]
    public void WhenMixedFilteredAndUnfilteredAggregates_ShouldWorkCorrectly()
    {
        var query = @"select
            Count(City) as TotalCount,
            Count(City) filter (where Population > 300) as LargeCount
            from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("TotalCount", typeof(long)),
            ("LargeCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [7L, 3L]);
    }

    [TestMethod]
    public void WhenFilterMatchesNoRows_ShouldReturnZeroOrNull()
    {
        var query = "select Count(City) filter (where Population > 9999) as C from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [0L]);
    }

    [TestMethod]
    public void WhenFilterWithAndCondition_ShouldApplyBothConditions()
    {
        var query = "select Count(City) filter (where Population > 100 and Country = 'Poland') as C from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2L]);
    }

    [TestMethod]
    public void WhenFilterWithOrCondition_ShouldApplyEitherCondition()
    {
        var query = "select Count(City) filter (where Country = 'Poland' or Country = 'France') as C from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [5L]);
    }

    [TestMethod]
    public void WhenFilterWithHaving_ShouldFilterThenHaving()
    {
        var query = @"select Country, Count(City) filter (where Population > 200) as C
                      from #A.Entities()
                      group by Country
                      having Count(City) filter (where Population > 200) >= 2";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Poland", 2L]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("TotalCities", typeof(long)),
            ("LargeCities", typeof(long)),
            ("TotalPop", typeof(decimal?)),
            ("LargePop", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", 3L, 2L, 900m, 800m],
            ["Germany", 2L, 1L, 600m, 400m],
            ["France", 2L, 1L, 750m, 600m]);
    }

    [TestMethod]
    public void WhenFilterCaseInsensitive_ShouldWork()
    {
        var query = "select Count(City) FILTER (WHERE Population > 200) as C from #A.Entities()";
        var sources = CreateCitySources();

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [4L]);
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
