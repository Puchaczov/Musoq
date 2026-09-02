using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core023GroupByFilterTests : BasicEntityTestBase
{
    [TestMethod]
    public void GroupByAll_CaseInsensitiveDuplicateExpressions_ShouldDeduplicateExpansion()
    {
        const string query = "select ToLower(City), tolower(City) as Repeated, Count(*) as Rows from #A.Entities() group by all";

        var result = Analyze(query);

        Assert.IsTrue(result.IsSuccess, FormatDiagnostics(result));
        var groupBy = GetQuery(result.Root!).GroupBy;
        Assert.IsNotNull(groupBy);
        Assert.HasCount(1, groupBy.Fields);
        StringAssert.Contains(groupBy.Fields[0].Expression.ToString(), "ToLower");
    }

    [TestMethod]
    public void GroupByNegativeInteger_ShouldReportExactNonPositiveOrdinalDiagnostic()
    {
        const string query = "select Count(*) as Rows from #A.Entities() group by -1";

        var result = Analyze(query);
        var diagnostic = AssertSingleError(result, DiagnosticCode.MQ3024_GroupByIndexOutOfRange);

        Assert.AreEqual(
            "GROUP BY position -1 is out of range. SELECT projection contains 1 field(s).",
            diagnostic.Message);
        Assert.AreEqual(new TextSpan(query.IndexOf("-1", StringComparison.Ordinal), 2), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
    }

    [TestMethod]
    public void GroupByZero_ShouldReportExactOrdinalDiagnostic()
    {
        const string query = "select City, Count(*) from #A.Entities() group by 0";
        var result = Analyze(query);
        var diagnostic = AssertSingleError(result, DiagnosticCode.MQ3024_GroupByIndexOutOfRange);

        Assert.AreEqual(
            "GROUP BY position 0 is out of range. SELECT projection contains 2 field(s).",
            diagnostic.Message);
        Assert.AreEqual(new TextSpan(query.IndexOf('0'), 1), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
    }

    [TestMethod]
    public void GroupByOrdinal_AggregateProjection_ShouldReportExactAggregateDiagnostic()
    {
        const string query = "select City, Count(*) as Rows from #A.Entities() group by 2";
        var result = Analyze(query);
        var diagnostic = AssertSingleError(result, DiagnosticCode.MQ3092_AggregateInGroupBy);

        Assert.AreEqual(
            "GROUP BY expressions cannot contain aggregate functions or aggregate SELECT aliases.",
            diagnostic.Message);
        Assert.AreEqual(new TextSpan(query.IndexOf('2'), 1), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
    }

    [TestMethod]
    public void GroupByAll_WithFilteredAggregateAndHaving_ShouldExpandKeysBeforeFilteringGroups()
    {
        const string query = """
            select Country, Count(Name) filter (where Population > 100) as Large
            from #A.Entities()
            group by all
            having Count(Name) filter (where Population > 100) > 0
            order by Country
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Country = "DE", Name = "A", Population = 150m },
            new BasicEntity { Country = "DE", Name = "B", Population = 50m },
            new BasicEntity { Country = "FR", Name = "C", Population = 200m },
            new BasicEntity { Country = "FR", Name = "D", Population = 75m },
            new BasicEntity { Country = "PL", Name = "E", Population = 50m })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Large", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", 1L],
            ["FR", 1L]);
    }

    [TestMethod]
    public void AggregateFilter_AggregateValues_ShouldFilterBeforeAggregation()
    {
        const string query = "select AggregateValues(Name, '|') filter (where NullableValue > 0) as Names from #A.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { Name = "ignored", NullableValue = 0 },
            new BasicEntity { Name = "first", NullableValue = 1 },
            new BasicEntity { Name = "null", NullableValue = null },
            new BasicEntity { Name = "second", NullableValue = 2 })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Names", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["first|second"]);
    }

    [TestMethod]
    public void AggregateFilter_Distinct_ShouldFilterBeforeDeduplication()
    {
        const string query = "select Count(distinct City) filter (where Population > 100) as UniqueCities from #A.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { City = "Warsaw", Population = 50m },
            new BasicEntity { City = "Warsaw", Population = 150m },
            new BasicEntity { City = "Berlin", Population = 200m },
            new BasicEntity { City = "Berlin", Population = 75m },
            new BasicEntity { City = null, Population = 300m })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("UniqueCities", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2L]);
    }

    [TestMethod]
    public void AggregateFilter_NullPredicateResult_ShouldExcludeTheRow()
    {
        const string query = "select Count(*) filter (where NullableValue > 0) as Rows from #A.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity { NullableValue = null },
            new BasicEntity { NullableValue = 0 },
            new BasicEntity { NullableValue = 1 })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Rows", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1L]);
    }

    [TestMethod]
    public void FilterOnNonAggregate_ShouldReportExactDiagnosticEnvelope()
    {
        const string query = "select ToUpper(Name) filter (where Name = 'Alice') from #A.Entities()";
        var result = Analyze(query);
        var diagnostic = AssertSingleError(result, DiagnosticCode.MQ3051_FilterOnNonAggregate);
        var functionStart = query.IndexOf("ToUpper", StringComparison.Ordinal);

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(
            "FILTER clause can only be applied to aggregate functions, but 'ToUpper' is not an aggregate function.",
            diagnostic.Message);
        Assert.AreEqual(new TextSpan(functionStart, "ToUpper".Length), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsEmpty(diagnostic.Arguments);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(DiagnosticCode.MQ3051_FilterOnNonAggregate, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(functionStart, envelope.Offset);
        Assert.AreEqual(functionStart + "ToUpper".Length, envelope.EndOffset);
        Assert.AreEqual("The FILTER clause can only be applied to aggregate functions.", envelope.Explanation);
        Assert.AreEqual("Core Spec - FILTER", envelope.DocsReference);
        CollectionAssert.AreEqual(
            new[]
            {
                "Remove FILTER from the non-aggregate function call.",
                "Use a CASE expression inside an aggregate argument for conditional aggregation."
            },
            envelope.SuggestedFixes.ToArray());
        Assert.HasCount(2, envelope.Actions);
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [] })).Analyze(query);
    }

    private static QueryNode GetQuery(RootNode root)
    {
        var statements = (StatementsArrayNode)root.Expression;
        return statements.Statements.Single().Node switch
        {
            QueryNode query => query,
            SingleSetNode singleSet => singleSet.Query,
            _ => throw new AssertFailedException("Expected a single query statement.")
        };
    }

    private static Diagnostic AssertSingleError(QueryAnalysisResult result, DiagnosticCode expectedCode)
    {
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, FormatDiagnostics(result));
        Assert.AreEqual(expectedCode, errors[0].Code);
        return errors[0];
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"{diagnostic.Code}: {diagnostic.Message}"));
    }
}
