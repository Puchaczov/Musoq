using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class AggregateIdentityTests : BasicEntityTestBase
{
    [TestMethod]
    public void FilteredCountsKeepDifferentNumericDivisions()
    {
        const string sql =
            "select Count(*) filter (where s.K/2i=1) as L,Count(*) filter (where s.K/2d=1) as R"
            + " from values { (K:3) } s";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1L, 0L]);
    }

    [TestMethod]
    [DataRow(
        "select Count(*) filter (where s.C='a') as L,Count(*) filter (where s.C='A') as U from values { (C:'a') } s"
    )]
    [DataRow("pivot values { (C:'a') } s on C in ('a' as L,'A' as U) using Count(*) as N")]
    public void AggregateFilters_PreserveTheCaseOfTextLiterals(string sql)
    {
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1L, 0L]);
    }

    [TestMethod]
    public void AggregateArguments_PreserveTheCaseOfTextLiterals()
    {
        const string sql = "select Max('a') as L,Max('A') as U from values { (C:'a') } s";
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, ["a", "A"]);
    }

    [TestMethod]
    public void FilteredAggregates_WithDifferentPredicates_ShouldNotCollapse()
    {
        const string query = (
            "select\n"
            + "    Count(City) filter (where Population > 100) as Over100,\n"
            + "    Count(City) filter (where Population > 200) as Over200\n"
            + "from #A.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreatePopulationSources()).Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Over100", typeof(long)), ("Over200", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2L, 1L]);
    }

    [TestMethod]
    public void FilteredAggregates_WithDottedStringLiterals_ShouldNotStripLiteralSegments()
    {
        const string query = (
            "select\n"
            + "    Count(City) filter (where Country = 'a.b') as Dotted,\n"
            + "    Count(City) filter (where Country = 'b') as Plain\n"
            + "from #A.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreateDottedLiteralSources()).Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Dotted", typeof(long)), ("Plain", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2L, 1L]);
    }

    [TestMethod]
    public void AggregateIdentity_ShouldKeepDistinctAndFilterSeparate()
    {
        const string query = (
            "select\n"
            + "    Count(Country) as AllCountries,\n"
            + "    Count(distinct Country) filter (where Population > 0) as DistinctPositiveCountries,\n"
            + "    Count(Country) filter (where Population > 0) as PositiveCountries\n"
            + "from #A.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreateDistinctSources()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("AllCountries", typeof(long)),
            ("DistinctPositiveCountries", typeof(long)),
            ("PositiveCountries", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, [4L, 2L, 3L]);
    }

    [TestMethod]
    [DataRow("1 in (Count(*))", true)]
    [DataRow("Sum(K) is null", false)]
    [DataRow("Sum(K) filter (where K=0) is null", true)]
    public void AggregateInsidePredicate_ShouldReadTheResultForItsGroup(string expression, bool expected)
    {
        var sql = $"select {expression} as B from values {{ (K: 1) }} v group by K";
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [expected]);
    }

    [TestMethod]
    [DataRow("Count(*)")]
    [DataRow("1+Count(*) filter (where K=2)")]
    public void OrderByAggregateOutsideSelect_ShouldOrderGroupsWithoutAddingAnOutput(string order)
    {
        var sql = $"select K from values {{ (K:1),(K:2),(K:2) }} v group by K order by {order} desc";
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("K", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2], [1]);
    }

    [TestMethod]
    public void OrderByAggregate_WithQualifiedReference_ShouldBindToEquivalentProjectedAggregate()
    {
        const string query = (
            "select Country, Sum(Population) as Total\n"
            + "from #A.Entities() a\n"
            + "group by Country\n"
            + "order by Sum(a.Population) desc"
        );

        var table = CreateAndRunVirtualMachine(query, CreateGroupedSources()).Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Germany", 400m], ["Poland", 300m]);
    }

    [TestMethod]
    public void OrderByAggregateReference_ShouldResolveProjectedAggregateWhenUnambiguous()
    {
        const string query = (
            "select Country, Sum(Population) as Total\n"
            + "from #A.Entities()\n"
            + "group by Country\n"
            + "order by Sum(Population) desc"
        );

        var table = CreateAndRunVirtualMachine(query, CreateGroupedSources()).Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Germany", 400m], ["Poland", 300m]);
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreatePopulationSources()
    {
        return CreateSingleSource(
            new BasicEntity("A", "PL", 50),
            new BasicEntity("B", "PL", 150),
            new BasicEntity("C", "DE", 250)
        );
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateDottedLiteralSources()
    {
        return CreateSingleSource(
            new BasicEntity("A", "a.b", 1),
            new BasicEntity("B", "b", 1),
            new BasicEntity("C", "a.b", 1)
        );
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateDistinctSources()
    {
        return CreateSingleSource(
            new BasicEntity("A", "Poland", 100),
            new BasicEntity("B", "Poland", 200),
            new BasicEntity("C", "Germany", 300),
            new BasicEntity("D", "France", -10)
        );
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateGroupedSources()
    {
        return CreateSingleSource(
            new BasicEntity("A", "Poland", 100),
            new BasicEntity("B", "Poland", 200),
            new BasicEntity("C", "Germany", 400)
        );
    }
}
