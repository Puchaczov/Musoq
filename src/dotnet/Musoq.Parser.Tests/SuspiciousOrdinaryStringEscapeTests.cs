using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SuspiciousOrdinaryStringEscapeTests
{
    [TestMethod]
    [DataRow(@"'C:\new\test'", @"\n")]
    [DataRow(@"'C:\temp'", @"\t")]
    [DataRow(@"'C:\bin'", @"\b")]
    [DataRow(@"'C:\files'", @"\f")]
    [DataRow(@"'C:\root'", @"\r")]
    [DataRow(@"'C:\escape'", @"\e")]
    [DataRow(@"'C:\0data'", @"\0")]
    [DataRow(@"'C:\u0041'", @"\u0041")]
    [DataRow(@"'C:\x41'", @"\x41")]
    [DataRow(@"'C:/new\test'", @"\t")]
    [DataRow(@"'\new\file'", @"\n")]
    [DataRow(@"'.\new\file'", @"\n")]
    [DataRow(@"'..\temp\file'", @"\t")]
    [DataRow(@"'\\server\share'", @"\\")]
    [DataRow(@"'\\?\C:\Directory'", @"\\")]
    [DataRow(@"'\\.\pipe\name'", @"\\")]
    public void RootedOrExplicitRelativePath_WithValueChangingEscape_ShouldWarnAtFirstHazard(
        string source,
        string hazardousEscape)
    {
        var lexer = new Lexer(source, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        var warning = lexer.Diagnostics.ToSortedList().Single();
        Assert.AreEqual(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, warning.Code);
        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, warning.Phase);
        Assert.AreEqual(new TextSpan(source.IndexOf(hazardousEscape, System.StringComparison.Ordinal), hazardousEscape.Length),
            warning.Span);
        Assert.Contains(hazardousEscape, warning.Message);
    }

    [TestMethod]
    public void MultipleHazardsInOneLiteral_ShouldReportOnlyTheFirstHazard()
    {
        var source = @"'C:\new\temp'";
        var lexer = new Lexer(source, true);

        lexer.Next();

        var warnings = lexer.Diagnostics.ToSortedList()
            .Where(diagnostic => diagnostic.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape)
            .ToArray();
        Assert.HasCount(1, warnings);
        Assert.AreEqual(new TextSpan(source.IndexOf(@"\n", System.StringComparison.Ordinal), 2), warnings[0].Span);
    }

    [TestMethod]
    public void MultipleSuspiciousLiterals_ShouldReportOneWarningPerLiteral()
    {
        var query = @"select 'C:\new' from #system.dual(); select 'C:\temp' from #system.dual()";
        var lexer = new Lexer(query, true);

        while (lexer.Next().TokenType != TokenType.EndOfFile)
        {
        }

        var warnings = lexer.Diagnostics.ToSortedList()
            .Where(diagnostic => diagnostic.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape)
            .ToArray();
        Assert.HasCount(2, warnings);
        Assert.IsTrue(warnings[0].Span.Start < warnings[1].Span.Start);
    }

    [TestMethod]
    public void CorrectlyDoubledBackslashes_ShouldRemainQuietAndPreservePath()
    {
        var lexer = new Lexer(@"'C:\\new\\test'", true);

        var token = lexer.Next();

        Assert.AreEqual(@"C:\new\test", token.Value);
        Assert.IsFalse(lexer.Diagnostics.Any(diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape));
    }

    [TestMethod]
    public void UnknownEscapes_ShouldRemainQuiet()
    {
        var lexer = new Lexer(@"'C:\Some\Path'", true);

        lexer.Next();

        Assert.IsFalse(lexer.Diagnostics.Any(diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape));
    }

    [TestMethod]
    public void StandaloneEscape_ShouldRemainQuiet()
    {
        var lexer = new Lexer(@"'\n'", true);

        lexer.Next();

        Assert.IsFalse(lexer.Diagnostics.Any(diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape));
    }

    [TestMethod]
    public void MalformedEscape_ShouldReportMQ1004WithoutMQ5014()
    {
        var lexer = new Lexer(@"'C:\u123'", true, true);

        lexer.Next();

        var diagnostics = lexer.Diagnostics.ToSortedList();
        Assert.HasCount(1, diagnostics);
        Assert.AreEqual(DiagnosticCode.MQ1004_InvalidEscapeSequence, diagnostics[0].Code);
    }

    [TestMethod]
    public void StrictAndRecoveryModes_ShouldReportEquivalentWarningForValidLiteral()
    {
        var source = @"'C:\new\test'";
        var strictLexer = new Lexer(source, true);
        var recoveryLexer = new Lexer(source, true, true);

        strictLexer.Next();
        recoveryLexer.Next();

        var strictWarning = strictLexer.Diagnostics.ToSortedList().Single();
        var recoveryWarning = recoveryLexer.Diagnostics.ToSortedList().Single();
        Assert.AreEqual(strictWarning.Code, recoveryWarning.Code);
        Assert.AreEqual(strictWarning.Message, recoveryWarning.Message);
        Assert.AreEqual(strictWarning.Span, recoveryWarning.Span);
    }

    [TestMethod]
    public void ParseWithDiagnostics_ShouldReturnSuccessfulWarningOnlyResult()
    {
        var source = @"select 'C:\new\test' from #system.dual()";
        var result = Parse(source, useLexerDiagnostics: false);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsTrue(result.HasWarnings);
        Assert.HasCount(1, result.Warnings);
        Assert.AreEqual(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, result.Warnings.Single().Code);
        result.ThrowIfErrors();
    }

    [TestMethod]
    public void ParserAndLexerSharingDiagnosticBag_ShouldNotDuplicateWarnings()
    {
        var result = Parse(@"select 'C:\new\test' from #system.dual()", useLexerDiagnostics: true);

        Assert.HasCount(1, result.Warnings);
    }

    [TestMethod]
    public void WarningDiagnostic_ShouldExposeMetadataAndFormattedGuidance()
    {
        var source = @"select 'C:\new\test' from #system.dual()";
        var warning = Parse(source, useLexerDiagnostics: false).Warnings.Single();

        var envelope = MusoqErrorEnvelope.FromDiagnostic(warning, source);
        var formatted = MusoqErrorEnvelopeFormatter.FormatText(envelope);
        var json = MusoqErrorEnvelopeFormatter.FormatJson(envelope);

        Assert.AreEqual(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Warning, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.Contains("raw literal", formatted, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("double", formatted, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"code\":\"MQ5014\"", json);
        Assert.Contains("\"severity\":\"warning\"", json);
        Assert.Contains("\"phase\":\"parse\"", json);
    }

    private static ParseResult Parse(string query, bool useLexerDiagnostics)
    {
        var lexer = new Lexer(query, true);
        var diagnostics = useLexerDiagnostics
            ? lexer.Diagnostics
            : new DiagnosticBag { SourceText = lexer.SourceText };
        return new Parser(lexer, diagnostics).ParseWithDiagnostics();
    }
}
