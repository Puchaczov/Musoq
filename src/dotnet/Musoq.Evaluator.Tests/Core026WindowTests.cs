using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core026WindowTests : BasicEntityTestBase
{
    [TestMethod]
    public void WindowFrames_ImplicitRangeAndExplicitRows_ShouldDistinguishPeers()
    {
        const string query = """
            select Name,
                   Sum(Population) over (order by Population) as ImplicitRange,
                   Sum(Population) over (order by Population rows between unbounded preceding and current row) as PhysicalRows
            from #A.Entities()
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 100 },
            new BasicEntity("Charlie") { Population = 200 })).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 200m, 100m],
            ["Bob", 200m, 200m],
            ["Charlie", 400m, 400m]);
    }

    [TestMethod]
    public void NamedWindow_ReferenceShouldResolveCaseInsensitively()
    {
        const string query = """
            select Name, Sum(Population) over running as RunningPopulation
            from #A.Entities()
            window Running as (order by Name rows between unbounded preceding and current row)
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 })).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m],
            ["Bob", 300m],
            ["Charlie", 600m]);
    }

    [TestMethod]
    public void Qualify_SelectAliasShouldFilterBeforeOrderingAndPaging()
    {
        const string query = """
            select Name, RowNumber() over (order by Name) as rn
            from #A.Entities()
            qualify rn <= 3
            order by Name desc
            skip 1
            take 1
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity("Diana"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"))).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Bob", 2L]);
    }

    [TestMethod]
    public void BoundedRange_WithOneNumericOrderKeyShouldUseValueDistance()
    {
        const string query = """
            select Name, Sum(Population) over (
                order by Population range between 50 preceding and current row
            ) as NearbyPopulation
            from #A.Entities()
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity("A") { Population = 100 },
            new BasicEntity("B") { Population = 120 },
            new BasicEntity("C") { Population = 180 })).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", 100m],
            ["B", 220m],
            ["C", 180m]);
    }

    [TestMethod]
    public void QualifyWithoutWindow_ShouldReportExactDiagnostic()
    {
        const string query = "select Name from #A.Entities() qualify Name = 'Alice'";

        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3050_QualifyRequiresWindowFunction,
            "QUALIFY clause requires at least one window function in its expression.",
            SpanOf(query, "Name = 'Alice'"),
            "Core Spec - QUALIFY");
    }

    [TestMethod]
    public void RangeFrameWithoutOrderBy_ShouldReportExactDiagnostic()
    {
        const string query = "select Sum(Population) over (range between unbounded preceding and current row) from #A.Entities()";

        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3052_RangeFrameRequiresOrderBy,
            "A RANGE window frame requires an ORDER BY clause in the window specification.",
            SpanOf(query, "(range between unbounded preceding and current row)"),
            "Core Spec - Window Frames");
    }

    [TestMethod]
    public void InvalidWindowFrameBounds_ShouldReportExactDiagnostic()
    {
        const string query = "select Sum(Population) over (order by Name rows between unbounded following and current row) from #A.Entities()";

        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3053_InvalidWindowFrameBounds,
            "Invalid window frame: start bound 'UNBOUNDED FOLLOWING' is logically after end bound 'CURRENT ROW'.",
            SpanOf(query, "(order by Name rows between unbounded following and current row)"),
            "Core Spec - Window Frames");
    }

    [TestMethod]
    public void BoundedRangeWithNonNumericOrderKey_ShouldReportExactDiagnostic()
    {
        const string query = "select Sum(Population) over (order by Name range between 1 preceding and current row) from #A.Entities()";

        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3098_InvalidRangeFrameOrderKey,
            "A RANGE frame with a PRECEDING or FOLLOWING offset requires exactly one numeric ORDER BY key.",
            SpanOf(query, "(order by Name range between 1 preceding and current row)"),
            "Core Spec - Window Frames");
    }

    [TestMethod]
    public void UnknownNamedWindow_ShouldReportExactDiagnostic()
    {
        const string query = "select Name, RowNumber() over missing from #A.Entities()";

        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3104_UnknownNamedWindow,
            "Named window 'missing' is not defined in the current query.",
            SpanOf(query, "RowNumber() over missing"),
            "Core Spec - Window Functions");
    }

    [TestMethod]
    public void DuplicateNamedWindow_ShouldReportExactDiagnostic()
    {
        const string query = "select Name from #A.entities() window ranked as (order by Name), ranked as (order by City)";

        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3105_DuplicateNamedWindow,
            "Window definition 'ranked' is declared more than once in this query.",
            SpanOf(query, "ranked as (order by City)"),
            "Core Spec - Window Functions");
    }

    [TestMethod]
    public void WindowFunctionInHaving_ShouldReportExactDiagnostic()
    {
        const string query = "select City, Count() from #A.entities() group by City having RowNumber() over (order by City) = 1";

        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3101_WindowFunctionInFilter,
            "Window functions are not allowed in HAVING; use QUALIFY to filter window results.",
            SpanOf(query, "RowNumber() over (order by City)"),
            "Core Spec - Window Functions");
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [] })).Analyze(query);
    }

    private static void AssertWindowDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        string expectedMessage,
        TextSpan expectedSpan,
        string expectedDocsReference)
    {
        var result = Analyze(query);
        var diagnostics = result.Errors.ToArray();
        Assert.HasCount(1, diagnostics, FormatDiagnostics(result));

        var diagnostic = diagnostics[0];
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedMessage, diagnostic.Message);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(expectedDocsReference, envelope.DocsReference);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.HasCount(envelope.SuggestedFixes.Count, envelope.Actions);
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
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
