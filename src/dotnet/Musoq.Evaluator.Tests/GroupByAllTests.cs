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

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Berlin", table[0][0]);
        Assert.AreEqual(2L, table[0][1]);
        Assert.AreEqual("Paris", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
        Assert.AreEqual("Warsaw", table[2][0]);
        Assert.AreEqual(2L, table[2][1]);
    }

    [TestMethod]
    public void GroupByAll_WhenSelectHasMultipleKeys_ShouldGroupByAllNonAggregates()
    {
        const string query = "select Country, City, Sum(Population) as Total from #A.Entities() group by all order by Country, City";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("DE", table[0][0]);
        Assert.AreEqual("Berlin", table[0][1]);
        Assert.AreEqual(250m, table[0][2]);
        Assert.AreEqual("FR", table[1][0]);
        Assert.AreEqual("Paris", table[1][1]);
        Assert.AreEqual(100m, table[1][2]);
        Assert.AreEqual("PL", table[2][0]);
        Assert.AreEqual("Warsaw", table[2][1]);
        Assert.AreEqual(250m, table[2][2]);
    }

    [TestMethod]
    public void GroupByAll_WhenSelectHasComputedScalar_ShouldGroupByExpression()
    {
        const string query = "select ToLower(City) as CityKey, Count(Name) as C from #A.Entities() group by all order by CityKey";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("berlin", table[0][0]);
        Assert.AreEqual(2L, table[0][1]);
        Assert.AreEqual("paris", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
        Assert.AreEqual("warsaw", table[2][0]);
        Assert.AreEqual(2L, table[2][1]);
    }

    [TestMethod]
    public void GroupByAll_WhenSelectHasConstant_ShouldKeepConstantAsGroupingKey()
    {
        const string query = "select Country, 'x' as Marker, Count(Name) as C from #A.Entities() group by all order by Country";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("DE", table[0][0]);
        Assert.AreEqual("x", table[0][1]);
        Assert.AreEqual(2L, table[0][2]);
        Assert.AreEqual("FR", table[1][0]);
        Assert.AreEqual("x", table[1][1]);
        Assert.AreEqual(2L, table[1][2]);
        Assert.AreEqual("PL", table[2][0]);
        Assert.AreEqual("x", table[2][1]);
        Assert.AreEqual(2L, table[2][2]);
    }

    [TestMethod]
    public void GroupByAll_WhenSelectRepeatsExpression_ShouldDeduplicateGroupingKeys()
    {
        const string query = "select City as A, City as B, Count(Name) as C from #A.Entities() group by all order by A";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Berlin", table[0][0]);
        Assert.AreEqual("Berlin", table[0][1]);
        Assert.AreEqual(2L, table[0][2]);
        Assert.AreEqual("Paris", table[1][0]);
        Assert.AreEqual("Paris", table[1][1]);
        Assert.AreEqual(2L, table[1][2]);
        Assert.AreEqual("Warsaw", table[2][0]);
        Assert.AreEqual("Warsaw", table[2][1]);
        Assert.AreEqual(2L, table[2][2]);
    }

    [TestMethod]
    public void GroupByAll_WhenSelectHasOnlyAggregates_ShouldUseSingleGlobalGroup()
    {
        const string query = "select Count(Name), Sum(Population) from #A.Entities() group by all";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(6L, table[0][0]);
        Assert.AreEqual(600m, table[0][1]);
    }

    [TestMethod]
    public void GroupByAll_WhenHavingReferencesAggregate_ShouldFilterExpandedGroups()
    {
        const string query = "select City, Sum(Population) as Total from #A.Entities() group by all having Sum(Population) > 100 order by City";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Berlin", table[0][0]);
        Assert.AreEqual(250m, table[0][1]);
        Assert.AreEqual("Warsaw", table[1][0]);
        Assert.AreEqual(250m, table[1][1]);
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

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("DE", table[0][0]);
        Assert.AreEqual("Berlin", table[0][1]);
        Assert.AreEqual("FR", table[1][0]);
        Assert.AreEqual("Paris", table[1][1]);
        Assert.AreEqual("PL", table[2][0]);
        Assert.AreEqual("Warsaw", table[2][1]);
    }

    [TestMethod]
    public void GroupByAll_WhenStarLikeExpandsColumns_ShouldUseExpandedColumnsAsKeys()
    {
        const string query = "select * like 'C%', Count(Name) as C from #A.Entities() group by all order by Country, City";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Berlin", table[0][0]);
        Assert.AreEqual("DE", table[0][1]);
        Assert.AreEqual(2L, table[0][2]);
        Assert.AreEqual("Paris", table[1][0]);
        Assert.AreEqual("FR", table[1][1]);
        Assert.AreEqual(2L, table[1][2]);
        Assert.AreEqual("Warsaw", table[2][0]);
        Assert.AreEqual("PL", table[2][1]);
        Assert.AreEqual(2L, table[2][2]);
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

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Berlin", table[0][0]);
        Assert.AreEqual("DE", table[0][1]);
        Assert.AreEqual(4L, table[0][2]);
        Assert.AreEqual("Paris", table[1][0]);
        Assert.AreEqual("FR", table[1][1]);
        Assert.AreEqual(1L, table[1][2]);
    }

    [TestMethod]
    public void GroupByAll_WhenStarReplaceCreatesExpression_ShouldUseReplacementExpressionAsKey()
    {
        const string query = "select * like 'P%' replace (Population + 1 as Population), Count(Name) as C from #A.Entities() group by all order by Population";

        var table = CreateAndRunVirtualMachine(query, CreatePopulationSources()).Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(11m, table[0][0]);
        Assert.AreEqual(2L, table[0][1]);
        Assert.AreEqual(21m, table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
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

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("DE", table[0][0]);
        Assert.AreEqual(2L, Convert.ToInt64(table[0][1]));
        Assert.AreEqual("FR", table[1][0]);
        Assert.AreEqual(2L, Convert.ToInt64(table[1][1]));
        Assert.AreEqual("PL", table[2][0]);
        Assert.AreEqual(2L, Convert.ToInt64(table[2][1]));
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
