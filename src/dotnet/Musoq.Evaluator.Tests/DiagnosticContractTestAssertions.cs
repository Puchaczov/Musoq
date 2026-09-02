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
        Assert.AreEqual(ExpectedSourceKind(expectedCode), diagnostic.SourceKind);
        AssertLocationConsistency(diagnostic, expectedCode, context);

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
        Assert.IsTrue(diagnostics.All(item => item.SourceKind == ExpectedSourceKind(expectedCode)));
        foreach (var diagnostic in diagnostics)
            AssertLocationConsistency(diagnostic, expectedCode, context);
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

    private static DiagnosticSourceKind ExpectedSourceKind(DiagnosticCode code)
    {
        var value = (int)code;

        return value switch
        {
            >= 4001 and <= 4016 => DiagnosticSourceKind.Schema,
            >= 7003 and <= 7009 => DiagnosticSourceKind.Runtime,
            >= 7010 and <= 7012 => DiagnosticSourceKind.DataSource,
            >= 8001 and <= 8002 => DiagnosticSourceKind.GeneratedSource,
            >= 9001 and <= 9002 => DiagnosticSourceKind.Internal,
            _ => DiagnosticSourceKind.Query
        };
    }

    private static void AssertLocationConsistency(
        Diagnostic diagnostic,
        DiagnosticCode expectedCode,
        string context)
    {
        if (!diagnostic.Location.IsValid && !diagnostic.EndLocation.IsValid)
            return;

        Assert.IsTrue(diagnostic.Location.IsValid, $"{expectedCode} ({context}) has an invalid source location.");
        Assert.IsTrue(diagnostic.EndLocation.IsValid, $"{expectedCode} ({context}) has an invalid end location.");
        Assert.IsTrue(diagnostic.Span.Length >= 0, $"{expectedCode} ({context}) has a negative span.");
        Assert.AreEqual(
            diagnostic.Location.Offset + diagnostic.Span.Length,
            diagnostic.EndLocation.Offset,
            $"{expectedCode} ({context}) has inconsistent source endpoints.");
    }
}
