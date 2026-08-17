using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// A small, exact diagnostic corpus for the public analysis contract.  The
/// broad feature suites exercise individual emitters; these tests ensure that
/// the result still has one deterministic root, a useful query location, and
/// the phase/severity promised by the diagnostic catalog.
/// </summary>
[TestClass]
public sealed class DiagnosticContractOracleTests
{
    [TestMethod]
    public void UnknownCallable_HasOneStructuredBindRoot()
    {
        var result = Analyze("select Missing(Name) from #A.Entities()");

        var diagnostic = AssertSingle(result, DiagnosticCode.MQ3086_UnknownCallable,
            DiagnosticSeverity.Error, DiagnosticPhase.Bind, "Missing");

        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsTrue(diagnostic.Arguments.ContainsKey("callable"),
            "Callable diagnostics must identify the unresolved symbol.");
    }

    [TestMethod]
    public void NullSensitiveNotIn_IsOneWarningOnlyAndKeepsQuerySuccessful()
    {
        var result = Analyze("select Name from #A.Entities() where Name not in ('Alice', null)");

        Assert.IsTrue(result.IsParsed);
        Assert.IsFalse(result.HasErrors);
        Assert.IsTrue(result.IsSuccess);

        var diagnostic = AssertSingle(result, DiagnosticCode.MQ5024_NullSensitiveNotIn,
            DiagnosticSeverity.Warning, DiagnosticPhase.Bind, "NULL");
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
    }

    [TestMethod]
    public void IndependentUnknownColumns_AreRetainedInSourceOrder()
    {
        var result = Analyze("select Unknown1, Unknown2, Unknown3 from #A.Entities()");
        var diagnostics = result.Errors.ToList();

        Assert.HasCount(3, diagnostics,
            $"Expected three independent roots, got: {Format(diagnostics)}");
        Assert.IsTrue(diagnostics.All(static item => item.Code == DiagnosticCode.MQ3001_UnknownColumn),
            Format(diagnostics));
        Assert.IsTrue(diagnostics.Zip(diagnostics.Skip(1),
                static (left, right) => left.Location.Offset < right.Location.Offset)
            .All(static ordered => ordered), Format(diagnostics));
    }

    [TestMethod]
    public void InvalidQueries_DoNotLeakBroadClrExceptionsThroughAnalysis()
    {
        var queries = new[]
        {
            "select Missing(Name) from #A.Entities()",
            "select * from #A.Missing()",
            "select Name from #A.Entities() cross apply #A.Entities()",
            "select Name from #A.Entities() where Name + true",
            "select Name from #A.Entities() where Name not in ()"
        };

        foreach (var query in queries)
        {
            try
            {
                var result = Analyze(query);
                Assert.IsNotNull(result, query);
            }
            catch (Exception exception)
            {
                Assert.Fail($"Analysis leaked {exception.GetType().Name} for '{query}': {exception.Message}");
            }
        }
    }

    [TestMethod]
    public void WarningAndErrorCollectionsRemainDisjoint()
    {
        var warningResult = Analyze("select Name from #A.Entities() where Name not in ('Alice', null)");
        var errorResult = Analyze("select Missing(Name) from #A.Entities()");

        Assert.IsTrue(warningResult.Warnings.All(static item => item.Severity == DiagnosticSeverity.Warning));
        Assert.IsTrue(warningResult.Errors.Any() == false);
        Assert.IsTrue(errorResult.Errors.All(static item => item.Severity == DiagnosticSeverity.Error));
        Assert.IsTrue(errorResult.Warnings.Any() == false);
    }

    private static Diagnostic AssertSingle(
        QueryAnalysisResult result,
        DiagnosticCode expectedCode,
        DiagnosticSeverity expectedSeverity,
        DiagnosticPhase expectedPhase,
        string messageFragment)
    {
        var diagnostics = result.Diagnostics.ToList();
        Assert.HasCount(1, diagnostics, Format(diagnostics));

        var diagnostic = diagnostics[0];
        Assert.AreEqual(expectedCode, diagnostic.Code, Format(diagnostics));
        Assert.AreEqual(expectedSeverity, diagnostic.Severity);
        Assert.AreEqual(expectedPhase, diagnostic.Phase);
        StringAssert.Contains(diagnostic.Message, messageFragment);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsTrue(diagnostic.Location.IsValid, "Query diagnostics need a known start location.");
        Assert.IsTrue(diagnostic.EndLocation.IsValid, "Query diagnostics need a known end location.");
        Assert.IsTrue(diagnostic.Span.Length > 0, "Root diagnostics should span the offending token.");
        Assert.AreEqual(diagnostic.Location.Offset + diagnostic.Span.Length, diagnostic.EndLocation.Offset);

        return diagnostic;
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

    private static string Format(IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(static item =>
            $"{item.Code} {item.Severity}/{item.Phase} at {item.Span}: {item.Message}"));
    }
}
