using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core022GroupByAggregationTests : BasicEntityTestBase
{
    [TestMethod]
    public void AggregateFamilies_GroupedRows_ShouldReturnTypedValues()
    {
        const string query = """
            select
                Country,
                Count(Name) as NamedRows,
                Count(*) as Rows,
                Sum(Population) as Total,
                Avg(Population) as Average,
                Min(Population) as Minimum,
                Max(Population) as Maximum,
                AggregateValues(Name, '|') as Names
            from #A.Entities()
            group by Country
            order by Country
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Name = "Alice", Country = "DE", Population = 5m },
            new BasicEntity { Name = "Bob", Country = "DE", Population = 15m },
            new BasicEntity { Name = "Carla", Country = "PL", Population = 20m })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("NamedRows", typeof(long)),
            ("Rows", typeof(long)),
            ("Total", typeof(decimal?)),
            ("Average", typeof(decimal?)),
            ("Minimum", typeof(decimal?)),
            ("Maximum", typeof(decimal?)),
            ("Names", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", 2L, 2L, 20m, 10m, 5m, 15m, "Alice|Bob"],
            ["PL", 1L, 1L, 20m, 20m, 20m, 20m, "Carla"]);
    }

    [TestMethod]
    public void ParentLevelAggregates_GroupedByMonthAndCity_ShouldRepeatMonthTotals()
    {
        const string query = """
            select
                Month,
                City,
                Count(City) as CityRows,
                Count(City, 1) as MonthRows,
                Sum(Money) as CityTotal,
                Sum(Money, 1) as MonthTotal,
                Avg(Money, 1) as MonthAverage
            from #A.Entities()
            group by Month, City
            order by Month, City
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Month = "Feb", City = "Warsaw", Money = 40m },
            new BasicEntity { Month = "Jan", City = "Krakow", Money = 30m },
            new BasicEntity { Month = "Jan", City = "Warsaw", Money = 10m },
            new BasicEntity { Month = "Jan", City = "Warsaw", Money = 20m })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Month", typeof(string)),
            ("City", typeof(string)),
            ("CityRows", typeof(long)),
            ("MonthRows", typeof(long)),
            ("CityTotal", typeof(decimal?)),
            ("MonthTotal", typeof(decimal?)),
            ("MonthAverage", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Feb", "Warsaw", 1L, 1L, 40m, 40m, 40m],
            ["Jan", "Krakow", 1L, 3L, 30m, 60m, 20m],
            ["Jan", "Warsaw", 2L, 3L, 30m, 60m, 20m]);
    }

    [TestMethod]
    public void AggregateWithoutGroupBy_ShouldProduceOneGlobalRow()
    {
        const string query = """
            select
                Count(*) as Rows,
                Count(Name) as NamedRows,
                Sum(Population) as Total,
                Avg(Population) as Average
            from #A.Entities()
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Name = "A", Population = 10m },
            new BasicEntity { Name = null, Population = 20m },
            new BasicEntity { Name = "C", Population = 30m })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Rows", typeof(long)),
            ("NamedRows", typeof(long)),
            ("Total", typeof(decimal?)),
            ("Average", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [3L, 2L, 60m, 20m]);
    }

    [TestMethod]
    public void NullGrouping_MultiColumnKeys_ShouldKeepNullTuplesDistinct()
    {
        const string query = """
            select Country, City, Count(City) as NamedCities, Count(*) as Rows
            from #A.Entities()
            group by Country, City
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Name = "A", Country = null, City = null },
            new BasicEntity { Name = "B", Country = null, City = "Warsaw" },
            new BasicEntity { Name = "C", Country = "PL", City = null },
            new BasicEntity { Name = "D", Country = "PL", City = null },
            new BasicEntity { Name = "E", Country = "PL", City = "Warsaw" })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)),
            ("NamedCities", typeof(long)),
            ("Rows", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [null, null, 0L, 1L],
            [null, "Warsaw", 1L, 1L],
            ["PL", null, 0L, 2L],
            ["PL", "Warsaw", 1L, 1L]);
    }

    [TestMethod]
    [DataRow("group by 'constant'")]
    [DataRow("group by '1'")]
    [DataRow("group by 1 + 0")]
    public void NonOrdinalConstants_ShouldProduceOneGroup(string groupByClause)
    {
        var query = $"select Count(*) as Rows, Sum(Population) as Total from #A.Entities() {groupByClause}";

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Population = 10m },
            new BasicEntity { Population = 20m })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Rows", typeof(long)),
            ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2L, 30m]);
    }

    [TestMethod]
    public void GroupByAll_WindowProjection_ShouldExcludeWindowFromGroupingKeys()
    {
        const string query = """
            select City, Count(*) as Rows, RowNumber() over (order by City) as Position
            from #A.Entities()
            group by all
            order by City
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { City = "Berlin" },
            new BasicEntity { City = "Berlin" },
            new BasicEntity { City = "Paris" })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Rows", typeof(long)),
            ("Position", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", 2L, 1L],
            ["Paris", 1L, 2L]);
    }

    [TestMethod]
    public void GroupByAll_RenameProjection_ShouldGroupFinalOutputExpressions()
    {
        const string query = """
            select * like 'C%' rename (City as CityKey), Count(*) as Rows
            from #A.Entities()
            group by all
            order by Country, City
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Country = "DE", City = "Berlin" },
            new BasicEntity { Country = "DE", City = "Berlin" },
            new BasicEntity { Country = "PL", City = "Warsaw" })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("CityKey", typeof(string)),
            ("Country", typeof(string)),
            ("Rows", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", "DE", 2L],
            ["Warsaw", "PL", 1L]);
    }

    [TestMethod]
    public void GroupByAll_OnlyWindowExpression_ShouldUseOneConstantGroup()
    {
        const string query = """
            select RowNumber() over (order by 1) as Position
            from #A.Entities()
            group by all
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { City = "Berlin" },
            new BasicEntity { City = "Paris" })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1L]);
    }

    [TestMethod]
    public void NonAggregateRestriction_ShouldReportExactDiagnosticForMissingColumn()
    {
        const string query = "select Name, City, Count(1) from #A.Entities() group by City";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3012_NonAggregateInSelect,
            "Column 'Name' must appear in the GROUP BY clause or be used in an aggregate function. Current GROUP BY columns: City.",
            "Name",
            "Every selected column must be either aggregated or included in the GROUP BY clause.",
            [
                "Add the column to the GROUP BY clause.",
                "Wrap the column in an aggregate function such as Count, Sum, Min, or Max."
            ]);
    }

    [TestMethod]
    public void AggregateInGroupBy_ShouldReportExactDiagnostic()
    {
        const string query = "select Count(*) from #A.Entities() group by Sum(Population)";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3092_AggregateInGroupBy,
            "GROUP BY expressions cannot contain aggregate functions or aggregate SELECT aliases.",
            "Sum",
            "An aggregate expression is not valid inside GROUP BY.",
            [
                "Group by the input expression instead.",
                "Move the aggregate expression to SELECT or HAVING."
            ]);
    }

    [TestMethod]
    public void GroupByOrdinalOutOfRange_ShouldReportExactDiagnostic()
    {
        const string query = "select City, Count(*) from #A.Entities() group by 3";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3024_GroupByIndexOutOfRange,
            "GROUP BY position 3 is out of range. SELECT projection contains 2 field(s).",
            "3",
            "A positional GROUP BY reference points outside the SELECT projection list.",
            [
                "Use a GROUP BY index between 1 and the number of selected columns.",
                "Prefer grouping by the expression or alias directly for clarity."
            ],
            "Core Spec - GROUP BY Clause");
    }

    [TestMethod]
    public void QualifiedNonAggregateColumnFromAnotherAlias_ShouldNotSatisfyGroupingByName()
    {
        const string query = """
            select a.City, b.City, Count(*) as Rows
            from #A.Entities() a
            inner join #B.Entities() b on a.Id = b.Id
            group by a.City
            """;

        var result = Analyze(query);
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var diagnostic = result.Errors.Single();

        Assert.AreEqual(DiagnosticCode.MQ3012_NonAggregateInSelect, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(
            "Column 'City' must appear in the GROUP BY clause or be used in an aggregate function. Current GROUP BY columns: City.",
            diagnostic.Message);
        var cityStart = query.IndexOf("b.City", StringComparison.Ordinal) + "b.".Length;
        Assert.AreEqual(new TextSpan(cityStart, "City".Length), diagnostic.Span);
    }

    private static void AssertSemanticDiagnosticContract(
        string query,
        DiagnosticCode expectedCode,
        string expectedMessage,
        string expectedExpression,
        string expectedExplanation,
        string[] expectedFixes,
        string expectedDocsReference = "Core Spec - GROUP BY and Aggregation")
    {
        var result = Analyze(query);
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, FormatDiagnostics(result));

        var diagnostic = errors[0];
        var expectedStart = query.IndexOf(expectedExpression, StringComparison.Ordinal);
        var expectedSpan = new TextSpan(expectedStart, expectedExpression.Length);

        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedMessage, diagnostic.Message);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsEmpty(diagnostic.Arguments);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedSpan.End, envelope.EndOffset);
        Assert.AreEqual(expectedSpan.Length, envelope.Length);
        Assert.AreEqual(expectedExplanation, envelope.Explanation);
        Assert.AreEqual(expectedDocsReference, envelope.DocsReference);
        CollectionAssert.AreEqual(expectedFixes, envelope.SuggestedFixes.ToArray());
        Assert.HasCount(expectedFixes.Length, envelope.Actions);
        CollectionAssert.AreEqual(expectedFixes, envelope.Actions.Select(static action => action.Title).ToArray());
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [],
                ["#B"] = []
            });

        return new QueryAnalyzer(provider).Analyze(query);
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"{diagnostic.Code}: {diagnostic.Message}"));
    }
}
