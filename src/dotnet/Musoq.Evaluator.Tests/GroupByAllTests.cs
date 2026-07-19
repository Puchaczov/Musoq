using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class GroupByAllTests : BasicEntityTestBase
{
    [TestMethod]
    public void GroupByAll_WhenSelectHasSingleKey_ShouldGroupByThatKey()
    {
        const string query = "select City, Count(Name) as C from #A.Entities() group by all order by City";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", 2L],
            ["Paris", 2L],
            ["Warsaw", 2L]);
    }

    [TestMethod]
    public void GroupByAll_WhenSelectHasMultipleKeys_ShouldGroupByAllNonAggregates()
    {
        const string query = "select Country, City, Sum(Population) as Total from #A.Entities() group by all order by Country, City";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)),
            ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", "Berlin", 250m],
            ["FR", "Paris", 100m],
            ["PL", "Warsaw", 250m]);
    }

    [TestMethod]
    public void GroupByAll_WhenSelectHasComputedScalar_ShouldGroupByExpression()
    {
        const string query = "select ToLower(City) as CityKey, Count(Name) as C from #A.Entities() group by all order by CityKey";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("CityKey", typeof(string)),
            ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["berlin", 2L],
            ["paris", 2L],
            ["warsaw", 2L]);
    }

    [TestMethod]
    public void GroupByAll_WhenSelectHasConstant_ShouldKeepConstantAsGroupingKey()
    {
        const string query = "select Country, 'x' as Marker, Count(Name) as C from #A.Entities() group by all order by Country";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Marker", typeof(string)),
            ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", "x", 2L],
            ["FR", "x", 2L],
            ["PL", "x", 2L]);
    }

    [TestMethod]
    public void GroupByAll_WhenSelectRepeatsExpression_ShouldDeduplicateGroupingKeys()
    {
        const string query = "select City as A, City as B, Count(Name) as C from #A.Entities() group by all order by A";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("A", typeof(string)),
            ("B", typeof(string)),
            ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", "Berlin", 2L],
            ["Paris", "Paris", 2L],
            ["Warsaw", "Warsaw", 2L]);
    }

    [TestMethod]
    public void GroupByAll_WhenSelectHasOnlyAggregates_ShouldUseSingleGlobalGroup()
    {
        const string query = "select Count(Name), Sum(Population) from #A.Entities() group by all";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Count(Name)", typeof(long)),
            ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [6L, 600m]);
    }

    [TestMethod]
    public void GroupByAll_WhenHavingReferencesAggregate_ShouldFilterExpandedGroups()
    {
        const string query = "select City, Sum(Population) as Total from #A.Entities() group by all having Sum(Population) > 100 order by City";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", 250m],
            ["Warsaw", 250m]);
    }

    [TestMethod]
    public void GroupByAll_WhenSelectUsesLegacyPrefixDoubleColonNumber_ShouldReportParseError()
    {
        const string query = "select ::1, Count(Name) from #A.Entities() group by all";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSources()));

        AssertAnyEnvelopeHasCode(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertMessageContains(ex, "DoubleColon");
    }

    [TestMethod]
    public void GroupByAll_WhenNoAggregates_ShouldReturnDistinctExpandedGroups()
    {
        const string query = "select Country, City from #A.Entities() group by all order by Country, City";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", "Berlin"],
            ["FR", "Paris"],
            ["PL", "Warsaw"]);
    }

    [TestMethod]
    public void GroupByAll_WhenStarLikeExpandsColumns_ShouldUseExpandedColumnsAsKeys()
    {
        const string query = "select * like 'C%', Count(Name) as C from #A.Entities() group by all order by Country, City";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Country", typeof(string)),
            ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", "DE", 2L],
            ["Paris", "FR", 2L],
            ["Warsaw", "PL", 2L]);
    }

    [TestMethod]
    public void GroupByAll_WhenAliasedStarExpandsAfterJoin_ShouldUseExpandedColumnsAsKeys()
    {
        const string query = @"
            select a.* exclude (Name, Population, Money, Month, Time, Id, NullableValue), Count(b.Name) as Matches
            from #A.Entities() a
            inner join #B.Entities() b on a.Country = b.Country
            group by all
            order by a.Country, a.City";

        var table = CreateAndRunVirtualMachine(query, CreateJoinSources()).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("a.Country", typeof(string)),
            ("Matches", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", "DE", 4L],
            ["Paris", "FR", 1L]);
    }

    [TestMethod]
    public void GroupByAll_WhenStarReplaceCreatesExpression_ShouldUseReplacementExpressionAsKey()
    {
        const string query = "select * like 'P%' replace (Population + 1 as Population), Count(Name) as C from #A.Entities() group by all order by Population";

        var table = CreateAndRunVirtualMachine(query, CreatePopulationSources()).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Population", typeof(decimal)),
            ("C", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [11m, 2L],
            [21m, 1L]);
    }

    [TestMethod]
    public void GroupByAll_WhenUsedInsideCte_ShouldExposeOrdinaryGroupedResult()
    {
        const string query = @"
            with grouped as (
                select Country, City, Count(Name) as C
                from #A.Entities()
                group by all
            )
            select Country, Sum(C) as Total
            from grouped
            group by Country
            order by Country";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Total", typeof(long?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", 2L],
            ["FR", 2L],
            ["PL", 2L]);
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Alice") { City = "Warsaw", Country = "PL", Population = 100m },
                    new BasicEntity("Bob") { City = "Warsaw", Country = "PL", Population = 150m },
                    new BasicEntity("Carla") { City = "Berlin", Country = "DE", Population = 200m },
                    new BasicEntity("Dora") { City = "Paris", Country = "FR", Population = 75m },
                    new BasicEntity("Eve") { City = "Paris", Country = "FR", Population = 25m },
                    new BasicEntity("Frank") { City = "Berlin", Country = "DE", Population = 50m }
                ]
            }
        };
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateJoinSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A1") { City = "Berlin", Country = "DE" },
                    new BasicEntity("A2") { City = "Berlin", Country = "DE" },
                    new BasicEntity("A3") { City = "Paris", Country = "FR" }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("B1") { Country = "DE" },
                    new BasicEntity("B2") { Country = "DE" },
                    new BasicEntity("B3") { Country = "FR" }
                ]
            }
        };
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreatePopulationSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A") { Population = 10m },
                    new BasicEntity("B") { Population = 10m },
                    new BasicEntity("C") { Population = 20m }
                ]
            }
        };
    }
}
