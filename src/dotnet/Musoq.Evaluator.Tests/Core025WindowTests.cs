using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core025WindowTests : BasicEntityTestBase
{
    [TestMethod]
    public void WindowFamilies_WithPeers_ShouldReturnDocumentedRankingValues()
    {
        const string query = """
            select Name,
                   RowNumber() over (order by Population) as RowNo,
                   Rank() over (order by Population) as RankNo,
                   DenseRank() over (order by Population) as DenseRankNo,
                   PercentRank() over (order by Population) as PercentRankNo,
                   CumeDist() over (order by Population) as CumeDistNo,
                   Ntile(3) over (order by Population) as TileNo
            from #A.Entities()
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 200 },
            new BasicEntity("Diana") { Population = 300 })).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNo", typeof(long)),
            ("RankNo", typeof(long)),
            ("DenseRankNo", typeof(long)),
            ("PercentRankNo", typeof(double)),
            ("CumeDistNo", typeof(double)),
            ("TileNo", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 1L, 1L, 1L, 0d, 0.25d, 1L],
            ["Bob", 2L, 2L, 2L, 1d / 3d, 0.75d, 1L],
            ["Charlie", 3L, 2L, 2L, 1d / 3d, 0.75d, 2L],
            ["Diana", 4L, 4L, 3L, 1d, 1d, 3L]);
    }

    [TestMethod]
    public void WindowOrderBy_ExplicitNullPlacementAndCompositePartition_ShouldBeHonored()
    {
        const string query = """
            select Name, Country, City,
                   ROW_NUMBER() over (
                       partition by Country, City
                       order by NullableValue desc nulls last, Name asc nulls first) as RowNo
            from #A.Entities()
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity("A") { Country = "PL", City = "GDA", NullableValue = null },
            new BasicEntity("B") { Country = "PL", City = "GDA", NullableValue = 2 },
            new BasicEntity("C") { Country = "PL", City = "GDA", NullableValue = 1 },
            new BasicEntity("D") { Country = "PL", City = "WAW", NullableValue = null })).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", "PL", "GDA", 3L],
            ["B", "PL", "GDA", 1L],
            ["C", "PL", "GDA", 2L],
            ["D", "PL", "WAW", 1L]);
    }

    [TestMethod]
    public void OffsetWindows_WithExplicitOffsetAndDefault_ShouldPreserveNullableValueType()
    {
        const string query = """
            select Name,
                   Lag(Population, 2, 999) over (order by Name) as Previous,
                   Lead(Population, 2, 999) over (order by Name) as Next
            from #A.Entities()
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 })).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Previous", typeof(decimal?)),
            ("Next", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 999m, 300m],
            ["Bob", 999m, 999m],
            ["Charlie", 100m, 999m]);
    }

    [TestMethod]
    public void WindowArgumentArity_ShouldReportExactCallableDiagnostic()
    {
        const string query = "select RowNumber(1) over (order by Name) from #A.Entities()";
        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3087_InvalidCallableArity,
            "Callable 'RowNumber' does not accept 1 argument(s); expected 0.",
            SpanOf(query, "RowNumber"));
    }

    [TestMethod]
    public void Ntile_NonIntegerBucket_ShouldReportExactCallableDiagnostic()
    {
        const string query = "select Ntile(1.5) over (order by Name) from #A.Entities()";
        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            "No overload of callable 'Ntile' accepts argument types (Decimal).",
            SpanOf(query, "1.5"));
    }

    [TestMethod]
    public void Ntile_NonPositiveBucket_ShouldReportExactWindowArgumentDiagnostic()
    {
        const string query = "select Ntile(0) over (order by Name) from #A.Entities()";
        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3103_InvalidWindowFunctionArgument,
            "Window function 'Ntile' has an invalid argument: bucket count must be a positive integer.",
            SpanOf(query, "0"));
    }

    [TestMethod]
    public void Ntile_TooManyArguments_ShouldReportExactCallableDiagnostic()
    {
        const string query = "select Ntile(1, 2) over (order by Name) from #A.Entities()";
        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3087_InvalidCallableArity,
            "Callable 'Ntile' does not accept 2 argument(s); expected 1.",
            SpanOf(query, "Ntile"));
    }

    [TestMethod]
    public void OffsetWindow_InvalidOffsetType_ShouldReportExactCallableDiagnostic()
    {
        const string query = "select Lag(Name, 'one') over (order by Name) from #A.Entities()";
        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            "No overload of callable 'Lag' accepts argument types (String, String).",
            SpanOf(query, "'one'"));
    }

    [TestMethod]
    public void OffsetWindow_MissingValue_ShouldReportExactCallableDiagnostic()
    {
        const string query = "select Lag() over (order by Name) from #A.Entities()";
        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3087_InvalidCallableArity,
            "Callable 'Lag' does not accept 0 argument(s); expected 1..3.",
            SpanOf(query, "Lag"));
    }

    [TestMethod]
    public void NthValue_InvalidPositionType_ShouldReportExactCallableDiagnostic()
    {
        const string query = "select NthValue(Name, 'two') over (order by Name) from #A.Entities()";
        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            "No overload of callable 'NthValue' accepts argument types (String, String).",
            SpanOf(query, "'two'"));
    }

    [TestMethod]
    public void NthValue_ZeroPosition_ShouldReportExactWindowArgumentDiagnostic()
    {
        const string query = "select NthValue(Name, 0) over (order by Name) from #A.Entities()";
        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3103_InvalidWindowFunctionArgument,
            "Window function 'NthValue' has an invalid argument: position must be a positive 1-based integer.",
            SpanOf(query, "0"));
    }

    [TestMethod]
    public void NthValue_MissingPosition_ShouldReportExactCallableDiagnostic()
    {
        const string query = "select NthValue(Name) over (order by Name) from #A.Entities()";
        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3087_InvalidCallableArity,
            "Callable 'NthValue' does not accept 1 argument(s); expected 2.",
            SpanOf(query, "NthValue"));
    }

    [TestMethod]
    public void RankingWindow_WithoutOrderBy_ShouldReportExactWindowDiagnostic()
    {
        const string query = "select RowNumber() over () from #A.Entities()";
        var orderSpanStart = query.IndexOf("over ()", StringComparison.Ordinal) + "over ".Length;
        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3099_WindowOrderByRequired,
            "Window function 'RowNumber' requires ORDER BY inside its OVER specification.",
            new TextSpan(orderSpanStart, 2));
    }

    [TestMethod]
    public void NestedWindowFunction_ShouldReportExactWindowDiagnostic()
    {
        const string query = "select Sum(RowNumber() over (order by Name)) over (order by Name) from #A.Entities()";
        var start = query.IndexOf("Sum", StringComparison.Ordinal);
        var end = query.IndexOf(" from", StringComparison.Ordinal);

        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3100_NestedWindowFunction,
            "Window functions cannot be nested inside another window function. Move the inner expression into a CTE or derived query.",
            new TextSpan(start, end - start));
    }

    [TestMethod]
    public void WindowFunctionInWhere_ShouldReportExactWindowDiagnostic()
    {
        const string query = "select Name from #A.Entities() where RowNumber() over (order by Name) = 1";
        var start = query.LastIndexOf("RowNumber", StringComparison.Ordinal);
        var end = query.IndexOf(" = 1", start, StringComparison.Ordinal);

        AssertWindowDiagnostic(
            query,
            DiagnosticCode.MQ3101_WindowFunctionInFilter,
            "Window functions are not allowed in WHERE; use QUALIFY to filter window results.",
            new TextSpan(start, end - start));
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
        TextSpan expectedSpan)
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
        Assert.AreEqual("Core Spec - " + (expectedCode is DiagnosticCode.MQ3087_InvalidCallableArity or DiagnosticCode.MQ3088_NoMatchingCallableOverload
            ? "Method Resolution"
            : "Window Functions"), envelope.DocsReference);
        Assert.IsNotEmpty(envelope.SuggestedFixes);
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
