using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticCross069MultipleErrorRecoveryTests
{
    [TestMethod]
    public void SemanticRecovery_ShouldRetainIndependentRootsAndSuppressDependentCascades()
    {
        const string query =
            "select Unknown1 + Unknown2, Name.Missing.Deep, Unknown3 from #A.Entities() " +
            "where Unknown4 = 1 order by Unknown5";

        var result = Analyze(query);
        var diagnostics = result.Errors.ToList();

        Assert.IsTrue(result.IsParsed);
        Assert.HasCount(6, diagnostics, Format(diagnostics));
        CollectionAssert.AreEqual(
            new[]
            {
                DiagnosticCode.MQ3001_UnknownColumn,
                DiagnosticCode.MQ3001_UnknownColumn,
                DiagnosticCode.MQ3028_UnknownProperty,
                DiagnosticCode.MQ3001_UnknownColumn,
                DiagnosticCode.MQ3001_UnknownColumn,
                DiagnosticCode.MQ3001_UnknownColumn
            },
            diagnostics.Select(static diagnostic => diagnostic.Code).ToArray(),
            Format(diagnostics));

        var expectedOffsets = new[]
        {
            query.IndexOf("Unknown1", StringComparison.Ordinal),
            query.IndexOf("Unknown2", StringComparison.Ordinal),
            query.IndexOf("Missing", StringComparison.Ordinal),
            query.IndexOf("Unknown3", StringComparison.Ordinal),
            query.IndexOf("Unknown4", StringComparison.Ordinal),
            query.IndexOf("Unknown5", StringComparison.Ordinal)
        };
        CollectionAssert.AreEqual(
            expectedOffsets,
            diagnostics.Select(static diagnostic => diagnostic.Location.Offset).ToArray(),
            Format(diagnostics));

        var propertyDiagnostic = diagnostics.Single(static diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ3028_UnknownProperty);
        StringAssert.Contains(propertyDiagnostic.Message, "Missing");
        Assert.IsFalse(propertyDiagnostic.Message.Contains("Deep", StringComparison.Ordinal));
        AssertNoInternalDiagnostics(diagnostics);
    }

    [TestMethod]
    public void QualifiedUnknownColumns_ShouldKeepLaterClauseRootsReachable()
    {
        const string query =
            "select a.Unknown1, a.Name from #A.Entities() a " +
            "where a.Unknown2 = 1 order by a.Unknown3";

        var result = Analyze(query);
        var diagnostics = result.Errors.ToList();

        Assert.HasCount(3, diagnostics, Format(diagnostics));
        Assert.IsTrue(diagnostics.All(static diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ3001_UnknownColumn), Format(diagnostics));
        CollectionAssert.AreEqual(
            new[]
            {
                query.IndexOf("Unknown1", StringComparison.Ordinal),
                query.IndexOf("Unknown2", StringComparison.Ordinal),
                query.IndexOf("Unknown3", StringComparison.Ordinal)
            },
            diagnostics.Select(static diagnostic => diagnostic.Location.Offset).ToArray(),
            Format(diagnostics));
        AssertNoInternalDiagnostics(diagnostics);
    }

    [TestMethod]
    public void UnknownAliasChain_ShouldReportTheAliasRootOnly()
    {
        const string query = "select missing.Name.Deep from #A.Entities()";

        var result = Analyze(query);
        var diagnostics = result.Errors.ToList();

        Assert.HasCount(1, diagnostics, Format(diagnostics));
        Assert.AreEqual(DiagnosticCode.MQ3015_UnknownAlias, diagnostics[0].Code);
        StringAssert.Contains(diagnostics[0].Message, "missing");
        Assert.IsFalse(diagnostics.Any(static diagnostic =>
            diagnostic.Code is DiagnosticCode.MQ3001_UnknownColumn or DiagnosticCode.MQ3028_UnknownProperty));
        AssertNoInternalDiagnostics(diagnostics);
    }

    [TestMethod]
    public void RepeatedAnalysis_ShouldPreserveDiagnosticRootAndOrdering()
    {
        const string query =
            "select UnknownLeft, Name.Missing.Deep, UnknownRight from #A.Entities() " +
            "where UnknownFilter = 1 order by UnknownOrder";

        var first = Analyze(query).Diagnostics.Select(CreateSignature).ToArray();
        var second = Analyze(query).Diagnostics.Select(CreateSignature).ToArray();

        CollectionAssert.AreEqual(first, second);
        CollectionAssert.AreEqual(
            first.OrderBy(static signature => signature.Offset).ToArray(),
            first,
            "Diagnostics must be returned in deterministic source order.");
        Assert.IsFalse(first.Any(static signature =>
            signature.Code is DiagnosticCode.MQ9001_InternalCompilerError or
            DiagnosticCode.MQ9002_InternalExecutionError));
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = []
            });
        return new QueryAnalyzer(provider).Analyze(query);
    }

    private static DiagnosticSignature CreateSignature(Diagnostic diagnostic)
    {
        return new DiagnosticSignature(
            diagnostic.Code,
            diagnostic.Severity,
            diagnostic.Phase,
            diagnostic.SourceKind,
            diagnostic.Location.Offset,
            diagnostic.EndLocation.Offset,
            diagnostic.Message);
    }

    private static void AssertNoInternalDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        Assert.IsFalse(diagnostics.Any(static diagnostic =>
            diagnostic.Code is DiagnosticCode.MQ9001_InternalCompilerError or
            DiagnosticCode.MQ9002_InternalExecutionError), Format(diagnostics));
    }

    private static string Format(IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(static diagnostic =>
            $"{diagnostic.Code} {diagnostic.Span}: {diagnostic.Message}"));
    }

    private sealed record DiagnosticSignature(
        DiagnosticCode Code,
        DiagnosticSeverity Severity,
        DiagnosticPhase Phase,
        DiagnosticSourceKind SourceKind,
        int Offset,
        int EndOffset,
        string Message);
}
