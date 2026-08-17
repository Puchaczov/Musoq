using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

internal static class DiagnosticContractTestAssertions
{
    internal static Diagnostic AssertSingleError(
        QueryAnalysisResult result,
        DiagnosticCode expectedCode,
        string context)
    {
        var diagnostics = result.Errors.ToList();
        Assert.HasCount(1, diagnostics,
            $"Expected one error {expectedCode} ({context}) but got: {Format(diagnostics)}");

        var diagnostic = diagnostics[0];
        Assert.AreEqual(expectedCode, diagnostic.Code, Format(diagnostics));
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhaseMapping.FromCode(expectedCode), diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        if (diagnostic.Location.IsValid || diagnostic.EndLocation.IsValid)
        {
            Assert.IsTrue(diagnostic.Location.IsValid, $"{expectedCode} ({context}) has an invalid source location.");
            Assert.IsTrue(diagnostic.EndLocation.IsValid, $"{expectedCode} ({context}) has an invalid end location.");
            Assert.IsTrue(diagnostic.Span.Length >= 0, $"{expectedCode} ({context}) has a negative span.");
            Assert.AreEqual(
                diagnostic.Location.Offset + diagnostic.Span.Length,
                diagnostic.EndLocation.Offset,
                $"{expectedCode} ({context}) has inconsistent source endpoints.");
        }

        return diagnostic;
    }

    internal static void AssertErrorsHaveCode(
        QueryAnalysisResult result,
        DiagnosticCode expectedCode,
        string context)
    {
        var diagnostics = result.Errors.ToList();
        Assert.IsNotEmpty(diagnostics,
            $"Expected {expectedCode} ({context}) but got no errors.");
        Assert.IsTrue(diagnostics.All(item => item.Code == expectedCode),
            $"Expected only {expectedCode} ({context}) but got: {Format(diagnostics)}");
        Assert.IsTrue(diagnostics.All(item => item.Severity == DiagnosticSeverity.Error));
        Assert.IsTrue(diagnostics.All(item => item.Phase == DiagnosticPhaseMapping.FromCode(expectedCode)));
    }

    internal static void AssertNoErrors(QueryAnalysisResult result, string context)
    {
        Assert.IsFalse(result.HasErrors,
            $"Expected no errors ({context}) but got: {Format(result.Diagnostics)}");
    }

    private static string Format(System.Collections.Generic.IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(static item =>
            $"{item.Code} {item.Severity}/{item.Phase} at {item.Span}: {item.Message}"));
    }
}
