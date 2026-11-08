using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AggregateIdentityTests : BasicEntityTestBase
{
    [TestMethod]
    public void FilteredAggregates_WithDifferentPredicates_ShouldNotCollapse()
    {
        const string query = """
                             select
                                 Count(City) filter (where Population > 100) as Over100,
                                 Count(City) filter (where Population > 200) as Over200
                             from #A.Entities()
                             """;

        var table = CreateAndRunVirtualMachine(query, CreatePopulationSources()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Over100", typeof(long)),
            ("Over200", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2L, 1L]);
    }

    [TestMethod]
    public void FilteredAggregates_WithDottedStringLiterals_ShouldNotStripLiteralSegments()
    {
        const string query = """
                             select
                                 Count(City) filter (where Country = 'a.b') as Dotted,
                                 Count(City) filter (where Country = 'b') as Plain
                             from #A.Entities()
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateDottedLiteralSources()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Dotted", typeof(long)),
            ("Plain", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2L, 1L]);
    }

    [TestMethod]
    public void AggregateIdentity_ShouldKeepDistinctAndFilterSeparate()
    {
        const string query = """
                             select
                                 Count(Country) as AllCountries,
                                 Count(distinct Country) filter (where Population > 0) as DistinctPositiveCountries,
                                 Count(Country) filter (where Population > 0) as PositiveCountries
                             from #A.Entities()
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateDistinctSources()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("AllCountries", typeof(long)),
            ("DistinctPositiveCountries", typeof(long)),
            ("PositiveCountries", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [4L, 2L, 3L]);
    }

    [TestMethod]
    public void OrderByAggregate_WithQualifiedReference_ShouldBindToEquivalentProjectedAggregate()
    {
        const string query = """
                             select Country, Sum(Population) as Total
                             from #A.Entities() a
                             group by Country
                             order by Sum(a.Population) desc
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateGroupedSources()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Germany", 400m],
            ["Poland", 300m]);
    }

    [TestMethod]
    public void OrderByAggregateReference_ShouldResolveProjectedAggregateWhenUnambiguous()
    {
        const string query = """
                             select Country, Sum(Population) as Total
                             from #A.Entities()
                             group by Country
                             order by Sum(Population) desc
                             """;

        var table = CreateAndRunVirtualMachine(query, CreateGroupedSources()).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Germany", 400m],
            ["Poland", 300m]);
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreatePopulationSources()
    {
        return CreateSingleSource(
            new BasicEntity("A", "PL", 50),
            new BasicEntity("B", "PL", 150),
            new BasicEntity("C", "DE", 250));
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateDottedLiteralSources()
    {
        return CreateSingleSource(
            new BasicEntity("A", "a.b", 1),
            new BasicEntity("B", "b", 1),
            new BasicEntity("C", "a.b", 1));
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateDistinctSources()
    {
        return CreateSingleSource(
            new BasicEntity("A", "Poland", 100),
            new BasicEntity("B", "Poland", 200),
            new BasicEntity("C", "Germany", 300),
            new BasicEntity("D", "France", -10));
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateGroupedSources()
    {
        return CreateSingleSource(
            new BasicEntity("A", "Poland", 100),
            new BasicEntity("B", "Poland", 200),
            new BasicEntity("C", "Germany", 400));
    }
}
