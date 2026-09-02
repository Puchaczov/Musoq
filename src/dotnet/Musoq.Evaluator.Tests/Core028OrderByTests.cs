using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core028OrderByTests : BasicEntityTestBase
{
    [TestMethod]
    public void OrderBy_DefaultDirections_ShouldUseOrdinalStringsAndDefaultNullPlacement()
    {
        var sources = CreateSingleSource(
            new BasicEntity("null") { City = null },
            new BasicEntity("A") { City = "A" },
            new BasicEntity("B") { City = "B" },
            new BasicEntity("a") { City = "a" },
            new BasicEntity("b") { City = "b" });

        var ascending = Run("select Name, City from #A.Entities() order by City", sources);
        var descending = Run("select Name, City from #A.Entities() order by City desc", sources);

        TableMaterializationTestHelper.AssertRowsInOrder(
            ascending,
            new object?[] { "null", null },
            ["A", "A"],
            ["B", "B"],
            ["a", "a"],
            ["b", "b"]);
        TableMaterializationTestHelper.AssertRowsInOrder(
            descending,
            ["b", "b"],
            ["a", "a"],
            ["B", "B"],
            ["A", "A"],
            new object?[] { "null", null });
    }

    [TestMethod]
    public void OrderBy_MixedKeysAndExplicitNullPolicies_ShouldApplyEachKey()
    {
        var table = Run(
            "select Name, Country, NullableValue from #A.Entities() order by Country desc nulls first, NullableValue asc nulls last, Name desc",
            CreateSingleSource(
                new BasicEntity("A") { Country = null, NullableValue = 2 },
                new BasicEntity("B") { Country = null, NullableValue = null },
                new BasicEntity("C") { Country = "US", NullableValue = 2 },
                new BasicEntity("D") { Country = "US", NullableValue = null },
                new BasicEntity("E") { Country = "PL", NullableValue = 1 }));

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            new object?[] { "A", null, 2 },
            new object?[] { "B", null, null },
            new object?[] { "C", "US", 2 },
            new object?[] { "D", "US", null },
            new object?[] { "E", "PL", 1 });
    }

    [TestMethod]
    public void OrderBy_ProjectionAlias_ShouldResolveCaseInsensitivelyBeforePaging()
    {
        var table = Run(
            "select Name, Money + Population as Total from #A.Entities() order by total desc skip 1 take 2",
            CreateSingleSource(
                new BasicEntity("low") { Money = 1m, Population = 10m },
                new BasicEntity("high") { Money = 10m, Population = 100m },
                new BasicEntity("middle") { Money = 5m, Population = 50m },
                new BasicEntity("top") { Money = 20m, Population = 200m }));

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["high", 110m],
            ["middle", 55m]);
    }

    [TestMethod]
    public void Pagination_ShouldRespectZeroAndOversizedBoundariesAfterOrdering()
    {
        var sources = CreateSingleSource(
            new BasicEntity("one") { Id = 1 },
            new BasicEntity("two") { Id = 2 },
            new BasicEntity("three") { Id = 3 });

        var page = Run("select Id from #A.Entities() order by Id desc skip 1 take 1", sources);
        var skipPastEnd = Run("select Id from #A.Entities() order by Id skip 10", sources);
        var takePastEnd = Run("select Id from #A.Entities() order by Id take 10", sources);
        var takeZero = Run("select Id from #A.Entities() order by Id take 0", sources);
        var skipZero = Run("select Id from #A.Entities() order by Id desc skip 0 take 2", sources);

        TableMaterializationTestHelper.AssertRowsInOrder(page, [2]);
        Assert.AreEqual(0, skipPastEnd.Count);
        TableMaterializationTestHelper.AssertRowsInOrder(takePastEnd, [1], [2], [3]);
        Assert.AreEqual(0, takeZero.Count);
        TableMaterializationTestHelper.AssertRowsInOrder(skipZero, [3], [2]);
    }

    [TestMethod]
    public void GroupedAndWindowedResults_ShouldOrderAndPageAtTheirFinalBoundaries()
    {
        var grouped = Run(
            "select Country, Count(Name) as Amount from #A.Entities() group by Country order by Amount desc take 2",
            CreateSingleSource(
                new BasicEntity("a") { Country = "PL" },
                new BasicEntity("b") { Country = "PL" },
                new BasicEntity("c") { Country = "US" },
                new BasicEntity("d") { Country = "DE" },
                new BasicEntity("e") { Country = "DE" },
                new BasicEntity("f") { Country = "DE" }));

        var windowed = Run(
            "select Name, RowNumber() over (order by Id desc) as RowNo from #A.Entities() order by RowNo skip 1 take 2",
            CreateSingleSource(
                new BasicEntity("one") { Id = 1 },
                new BasicEntity("two") { Id = 2 },
                new BasicEntity("three") { Id = 3 },
                new BasicEntity("four") { Id = 4 }));

        TableMaterializationTestHelper.AssertRowsInOrder(
            grouped,
            ["DE", 3L],
            ["PL", 2L]);
        TableMaterializationTestHelper.AssertRowsInOrder(
            windowed,
            ["three", 2L],
            ["two", 3L]);
    }

    [TestMethod]
    public void PositiveUnorderedSkip_ShouldReportExactAdvisoryWhileZeroSkipRemainsQuiet()
    {
        const string query = "select Name from #A.Entities() skip 2";
        var result = Analyze(query);
        var warning = result.Warnings.Single(static item => item.Code == DiagnosticCode.MQ5021_UnorderedSkip);

        Assert.IsFalse(result.HasErrors, FormatDiagnostics(result));
        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, warning.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, warning.SourceKind);
        Assert.AreEqual(
            "SKIP 2 is used without ORDER BY, so the skipped rows are not deterministic",
            warning.Message);
        Assert.AreEqual(SpanOf(query, "2"), warning.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(warning.ContextSnippet));

        var zero = Analyze("select Name from #A.Entities() skip 0");
        Assert.IsFalse(zero.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5021_UnorderedSkip));
    }

    [TestMethod]
    public void OrderBy_OrdinalExpression_ShouldReportExactDedicatedDiagnostic()
    {
        const string query = "select Name, City from #A.Entities() order by 1";
        var diagnostic = AssertSingleError(Analyze(query), DiagnosticCode.MQ3093_OrderByOrdinalUnsupported);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(
            "ORDER BY column position is not supported. Use a column name or alias instead of a numeric position.",
            diagnostic.Message);
        Assert.AreEqual(SpanOf(query, "1"), diagnostic.Span);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual("Core Spec - ORDER BY", envelope.DocsReference);
        Assert.IsNotEmpty(envelope.SuggestedFixes);
    }

    private Tables.Table Run(string query, IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var vm = CreateAndRunVirtualMachine(query, sources);
        return TableMaterializationTestHelper.Materialize(vm.Run());
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [] })).Analyze(query);
    }

    private static Diagnostic AssertSingleError(QueryAnalysisResult result, DiagnosticCode expectedCode)
    {
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, FormatDiagnostics(result));
        Assert.AreEqual(expectedCode, errors[0].Code);
        return errors[0];
    }

    private static TextSpan SpanOf(string query, string text)
    {
        return new TextSpan(query.IndexOf(text, StringComparison.Ordinal), text.Length);
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"{diagnostic.Code}: {diagnostic.Message} at {diagnostic.Span}"));
    }
}
