using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GroupByReferenceTests : BasicEntityTestBase
{
    [TestMethod]
    public void GroupByOrdinal_FirstProjection_ShouldGroupByFirstSelectExpression()
    {
        const string query = "select City, Count(Name) from #A.Entities() group by 1 order by City";

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
    public void LegacyPrefixDoubleColonNumber_SelectGroupByPattern_ShouldFailParsing()
    {
        const string query = "select ::1, Count(Name) from #A.Entities() group by City";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "DoubleColon");
    }

    [TestMethod]
    public void GroupByOrdinal_MultipleOrdinals_ShouldGroupByEachReferencedProjection()
    {
        const string query = "select Country, City, Count(Name) from #A.Entities() group by 1, 2 order by Country, City";

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
    public void GroupByOrdinal_AfterStarProjectionExpansion_ShouldUseExpandedProjectionPositions()
    {
        const string query = "select * like 'C%', Count(Name) as C from #A.Entities() group by 1, 2 order by Country, City";

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
    public void GroupByOrdinal_Zero_ShouldReportGroupByIndexOutOfRange()
    {
        const string query = "select City, Count(Name) from #A.Entities() group by 0";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3024_GroupByIndexOutOfRange, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void GroupByOrdinal_OutOfRange_ShouldReportGroupByIndexOutOfRange()
    {
        const string query = "select City, Count(Name) from #A.Entities() group by 3";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3024_GroupByIndexOutOfRange, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void GroupByAlias_NonAggregateSelectAlias_ShouldGroupByAliasedExpression()
    {
        const string query = "select City as c, Count(Name) as cnt from #A.Entities() group by c order by c";

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
    public void WhereAlias_NonAggregateSelectAlias_ShouldFilterByAliasedExpression()
    {
        const string query = "select Population + 1 as p, Name from #A.Entities() where p > 101 order by Name";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(151m, table[0][0]);
        Assert.AreEqual("Bob", table[0][1]);
        Assert.AreEqual(201m, table[1][0]);
        Assert.AreEqual("Carla", table[1][1]);
        Assert.AreEqual(251m, table[2][0]);
        Assert.AreEqual("Grace", table[2][1]);
    }

    [TestMethod]
    public void HavingAlias_AggregateSelectAlias_ShouldFilterByAggregateAlias()
    {
        const string query = "select City, Count(Name) as cnt from #A.Entities() group by City having cnt > 1 order by City";

        var table = CreateAndRunVirtualMachine(query, CreateSourcesWithSingleCity()).Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Berlin", table[0][0]);
        Assert.AreEqual(2L, table[0][1]);
        Assert.AreEqual("Warsaw", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
    }

    [TestMethod]
    public void HavingAlias_GroupedNonAggregateSelectAlias_ShouldFilterByGroupedAlias()
    {
        const string query = "select City as c, Count(Name) as cnt from #A.Entities() group by c having c = 'Warsaw'";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Warsaw", table[0][0]);
        Assert.AreEqual(2L, table[0][1]);
    }

    [TestMethod]
    public void AliasNameConflict_SourceColumnShouldWinOverSelectAlias()
    {
        const string query = "select Country as City, Name from #A.Entities() where City = 'Warsaw' order by Name";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("PL", table[0][0]);
        Assert.AreEqual("Alice", table[0][1]);
        Assert.AreEqual("PL", table[1][0]);
        Assert.AreEqual("Bob", table[1][1]);
    }

    [TestMethod]
    public void GroupByAlias_AggregateSelectAlias_ShouldBeRejected()
    {
        const string query = "select Count(Name) as cnt from #A.Entities() group by cnt";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2030_UnsupportedSyntax, DiagnosticPhase.Parse, "aggregate SELECT aliases");
    }

    [TestMethod]
    public void GroupByOrdinal_AggregateProjectionOrdinal_ShouldBeRejected()
    {
        const string query = "select City, Count(Name) as cnt from #A.Entities() group by 2";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2030_UnsupportedSyntax, DiagnosticPhase.Parse, "aggregate SELECT aliases");
    }

    [TestMethod]
    public void GroupByAll_WithAliasesAndCasts_ShouldExpandNonAggregateCastExpressions()
    {
        const string query = @"
            select City::String as c, Population::Int32 as p, Count(Name) as cnt
            from #A.Entities()
            group by all
            order by c, p";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TokenSource.Token);

        Assert.AreEqual(6, table.Count);
        Assert.AreEqual("Berlin", table[0][0]);
        Assert.AreEqual(200, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual("Berlin", table[1][0]);
        Assert.AreEqual(250, table[1][1]);
        Assert.AreEqual(1L, table[1][2]);
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
                    new BasicEntity("Grace") { City = "Berlin", Country = "DE", Population = 250m }
                ]
            }
        };
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateSourcesWithSingleCity()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Alice") { City = "Warsaw" },
                    new BasicEntity("Bob") { City = "Warsaw" },
                    new BasicEntity("Carla") { City = "Berlin" },
                    new BasicEntity("Dora") { City = "Berlin" },
                    new BasicEntity("Eve") { City = "Paris" }
                ]
            }
        };
    }
}
