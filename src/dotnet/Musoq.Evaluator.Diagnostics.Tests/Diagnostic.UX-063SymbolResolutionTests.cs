using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticUx063SymbolResolutionTests : Schema.NegativeTests.NegativeTestsBase
{
    [TestMethod]
    public void UnknownColumnTypo_ShouldExposeCanonicalCandidateAndSafeEdit()
    {
        const string query = "select Naame from #test.people()";
        var diagnostic = AssertSingleError(query, DiagnosticCode.MQ3001_UnknownColumn);
        var expectedSpan = SpanOf(query, "Naame");

        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        StringAssert.Contains(diagnostic.Message, "Did you mean 'Name'?");
        Assert.AreEqual("Naame", diagnostic.Arguments["column"]);
        Assert.AreEqual("Name", diagnostic.Arguments["suggestion"]);
        StringAssert.Contains(diagnostic.Arguments["candidateColumns"], "Name");
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.AreEqual("Core Spec - Column References", diagnostic.DocsReference);

        var action = diagnostic.SuggestedFixes.Single();
        Assert.AreEqual(DiagnosticActionKind.QuickFix, action.Kind);
        Assert.AreEqual(expectedSpan, action.TextEdit!.Span);
        Assert.AreEqual("Name", action.TextEdit.NewText);
    }

    [TestMethod]
    public void UnknownColumnCaseMismatch_ShouldSuggestSchemaCasingWithoutLowercaseEdit()
    {
        const string query = "select name from #test.people()";
        var diagnostic = AssertSingleError(query, DiagnosticCode.MQ3001_UnknownColumn);

        Assert.AreEqual("Name", diagnostic.Arguments["suggestion"]);
        Assert.AreEqual("Name", diagnostic.SuggestedFixes.Single().TextEdit!.NewText);
        Assert.AreEqual("name", query.Substring(
            diagnostic.SuggestedFixes.Single().TextEdit!.Span.Start,
            diagnostic.SuggestedFixes.Single().TextEdit!.Span.Length));
    }

    [TestMethod]
    public void AmbiguousColumn_ShouldExposeBothOwnersAndQualificationGuidance()
    {
        const string query =
            "select Id from #test.people() people inner join #test.nested() peers on people.Id = peers.Id";
        var diagnostic = AssertSingleError(query, DiagnosticCode.MQ3002_AmbiguousColumn);

        Assert.AreEqual(SpanOf(query, "Id"), diagnostic.Span);
        Assert.AreEqual("Id", diagnostic.Arguments["column"]);
        Assert.AreEqual("people, peers", diagnostic.Arguments["aliases"]);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.AreEqual("Core Spec - Column References", diagnostic.DocsReference);
        Assert.IsTrue(diagnostic.SuggestedFixes.Any(action =>
            action.Title.Contains("Qualify", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void UnknownPropertyTypo_ShouldKeepOwnerTypeAndCanonicalPropertyCasing()
    {
        const string query = "select Info.Labe from #test.nested()";
        var diagnostic = AssertSingleError(query, DiagnosticCode.MQ3028_UnknownProperty);

        Assert.AreEqual(SpanOf(query, "Labe"), diagnostic.Span);
        Assert.AreEqual("Labe", diagnostic.Arguments["property"]);
        Assert.AreEqual("ComplexInfo", diagnostic.Arguments["objectType"]);
        Assert.AreEqual("Label", diagnostic.Arguments["suggestion"]);
        Assert.AreEqual("Label", diagnostic.SuggestedFixes.Single().TextEdit!.NewText);
        Assert.AreEqual("Core Spec - Property Access", diagnostic.DocsReference);
    }

    [TestMethod]
    public void UnknownCallableTypo_ShouldExposeCandidateSignaturesAndSafeEdit()
    {
        const string query = "select Coutn(Name) from #test.people()";
        var diagnostic = AssertSingleError(query, DiagnosticCode.MQ3086_UnknownCallable);

        Assert.AreEqual(SpanOf(query, "Coutn"), diagnostic.Span);
        Assert.AreEqual("Coutn", diagnostic.Arguments["callable"]);
        Assert.AreEqual("Count", diagnostic.Arguments["suggestion"]);
        Assert.AreEqual("Count", diagnostic.Arguments["candidateCallables"]);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Arguments["availableCallables"]));
        Assert.AreEqual("Core Spec - Method Resolution", diagnostic.DocsReference);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));

        var action = diagnostic.SuggestedFixes.Single();
        Assert.AreEqual(DiagnosticActionKind.QuickFix, action.Kind);
        Assert.AreEqual(SpanOf(query, "Coutn"), action.TextEdit!.Span);
        Assert.AreEqual("Count", action.TextEdit.NewText);
    }

    [TestMethod]
    public void UnknownSymbolsWithoutCloseCandidate_ShouldNotInventDidYouMeanOrTextEdit()
    {
        const string columnQuery = "select NonExistentColumn from #test.people()";
        var columnDiagnostic = AssertSingleError(columnQuery, DiagnosticCode.MQ3001_UnknownColumn);
        Assert.IsFalse(columnDiagnostic.Message.Contains("Did you mean", StringComparison.Ordinal));
        Assert.IsFalse(columnDiagnostic.SuggestedFixes.Any(action => action.TextEdit != null));

        const string callableQuery = "select FakeFunc(Name) from #test.people()";
        var callableDiagnostic = AssertSingleError(callableQuery, DiagnosticCode.MQ3086_UnknownCallable);
        Assert.IsFalse(callableDiagnostic.Message.Contains("Did you mean", StringComparison.Ordinal));
        Assert.IsFalse(callableDiagnostic.SuggestedFixes.Any(action => action.TextEdit != null));
    }

    [TestMethod]
    public void SourceSchemaAndCteMistakes_ShouldRetainRootFactsAndGuidance()
    {
        const string sourceQuery = "select * from #test.poeple()";
        var sourceDiagnostic = AssertSingleError(sourceQuery, DiagnosticCode.MQ3085_UnknownSource);
        Assert.AreEqual("#test", sourceDiagnostic.Arguments["schema"]);
        Assert.AreEqual("poeple", sourceDiagnostic.Arguments["source"]);
        Assert.AreEqual("Core Spec - FROM Clause", sourceDiagnostic.DocsReference);
        Assert.IsNotEmpty(sourceDiagnostic.SuggestedFixes);

        const string schemaQuery = "select * from #tset.people()";
        var schemaDiagnostic = AssertSingleError(schemaQuery, DiagnosticCode.MQ3010_UnknownSchema);
        Assert.AreEqual("#tset", schemaDiagnostic.Arguments["schema"]);
        Assert.AreEqual("Core Spec - Schema References", schemaDiagnostic.DocsReference);
        Assert.IsNotEmpty(schemaDiagnostic.SuggestedFixes);

        const string cteQuery = "select * from MissingCte";
        var cteDiagnostic = AssertSingleError(cteQuery, DiagnosticCode.MQ3023_TableNotDefined);
        Assert.AreEqual("MissingCte", cteDiagnostic.Arguments["table"]);
        Assert.AreEqual("Core Spec - FROM Clause", cteDiagnostic.DocsReference);
        Assert.IsNotEmpty(cteDiagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void ValidCasingAndAliases_ShouldRemainExecutableAndDiagnosticFree()
    {
        const string query = "select people.Name from #test.people() people";
        var result = new QueryAnalyzer(CreateSchemaProvider()).Analyze(query);

        Assert.IsTrue(result.IsSuccess, string.Join(" | ", result.Diagnostics.Select(static item => item.Message)));
    }

    private Diagnostic AssertSingleError(string query, DiagnosticCode code)
    {
        var result = new QueryAnalyzer(CreateSchemaProvider()).Analyze(query);
        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, string.Join(" | ", result.Diagnostics.Select(static item => item.Message)));
        Assert.AreEqual(code, errors[0].Code);
        return errors[0];
    }

    private static TextSpan SpanOf(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start);
        return new TextSpan(start, text.Length);
    }
}
