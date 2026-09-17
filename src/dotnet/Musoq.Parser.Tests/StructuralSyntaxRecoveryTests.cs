using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class StructuralSyntaxRecoveryTests
{
    [TestMethod]
    [DynamicData(nameof(MalformedQueries))]
    public void MalformedStructuredSyntax_ShouldProduceBoundedQueryDiagnostics(string caseId, string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, $"{caseId}: malformed query parsed successfully.");
        Assert.IsNotEmpty(result.Diagnostics, $"{caseId}: no diagnostic was produced.");
        Assert.IsTrue(result.Diagnostics.Count <= 4, $"{caseId}: recovery cascaded unexpectedly.");

        foreach (var diagnostic in result.Diagnostics)
        {
            Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity, caseId);
            Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase, caseId);
            Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind, caseId);
            Assert.IsTrue(diagnostic.Span.Start >= 0, $"{caseId}: negative diagnostic start.");
            Assert.IsTrue(diagnostic.Span.End <= query.Length, $"{caseId}: diagnostic escaped query bounds.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Message), caseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation), caseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference), caseId);
        }
    }

    [TestMethod]
    public void ContextualWordsAndGrouping_ShouldRecoverToLaterValidStatements()
    {
        const string query = "select (1 + 2) * 3 from values /* before brace */ { ( Value: 9, ), } v";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
    }

    public static IEnumerable<object[]> MalformedQueries()
    {
        yield return ["array.missing-comma", "select * from #inputs.numbers(values: array { 1 2 }) n"];
        yield return ["array.doubled-comma", "select * from #inputs.numbers(values: array { 1,, 2 }) n"];
        yield return ["array.missing-close", "select * from #inputs.numbers(values: array { 1, 2 ) n"];
        yield return ["array.missing-keyword", "select * from #inputs.numbers(values: { 1, 2 }) n"];
        yield return ["record.missing-colon", "select * from #inputs.configure(options: (Enabled true)) c"];
        yield return ["record.missing-comma", "select * from #inputs.configure(options: (Enabled: true Codes: array { 1 })) c"];
        yield return ["record.empty", "select * from #inputs.configure(options: ()) c"];
        yield return ["values.legacy-row", "select * from values { { Name: 'A' } } v"];
        yield return ["values.mixed-row", "select * from values { ( Name: 'A' ), { Name: 'B' } } v"];
        yield return ["values.missing-row-comma", "select * from values { ( Name: 'A' ) ( Name: 'B' ) } v"];
        yield return ["type.repeated-suffix", "params(items: int[]??) select 1 from #system.dual()"];
        yield return ["declaration.incomplete", "params(items: (Id: string, Pattern:)) select 1 from #system.dual()"];
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}