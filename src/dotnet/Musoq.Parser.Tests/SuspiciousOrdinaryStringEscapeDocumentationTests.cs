using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SuspiciousOrdinaryStringEscapeDocumentationTests
{
    [TestMethod]
    [DataRow(@"select FullPath from os.files('C:\new\test', true)", true)]
    [DataRow(@"select FullPath from os.files('C:\new\test', true) take 5", true)]
    [DataRow(@"select FullPath from os.files(r'C:\new\test', true)", false)]
    [DataRow(@"select FullPath from os.files(r'C:\new\test', true) take 5", false)]
    [DataRow(@"select FullPath from os.files('C:\\new\\test', true)", false)]
    [DataRow(@"select FullPath from os.files('C:\\new\\test', true) take 5", false)]
    [DataRow(@"select 'C:\q' from system.dual()", false)]
    [DataRow(@"select r'C:\Some\Path\To\Directory' from system.dual()", false)]
    [DataRow(@"select r'\\server\share' from system.dual()", false)]
    [DataRow(@"select r'C:\Temp\' from system.dual()", false)]
    [DataRow(@"select r'a''b' from system.dual()", false)]
    public void DocumentedPathExamples_ShouldParseWithDocumentedWarningBehavior(
        string query,
        bool expectsWarning)
    {
        var result = Parse(query);

        Assert.IsNotNull(result.Root, result.FormatDiagnostics());
        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        var warnings = result.Warnings
            .Where(diagnostic => diagnostic.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape)
            .ToArray();

        if (expectsWarning)
        {
            Assert.HasCount(1, warnings, result.FormatDiagnostics());
            Assert.AreEqual(DiagnosticPhase.Parse, warnings[0].Phase);
            StringAssert.Contains(warnings[0].Message, "raw literal");
            return;
        }

        Assert.IsEmpty(warnings, result.FormatDiagnostics());
    }

    [TestMethod]
    public void DocumentedOrdinaryAndRawNewlineExamples_ShouldRemainWarningFree()
    {
        var ordinary = Parse(@"select '\n' from system.dual()");
        var raw = Parse(@"select r'\n' from system.dual()");

        Assert.IsTrue(ordinary.Success, ordinary.FormatDiagnostics());
        Assert.IsTrue(raw.Success, raw.FormatDiagnostics());
        Assert.IsEmpty(ordinary.Warnings, ordinary.FormatDiagnostics());
        Assert.IsEmpty(raw.Warnings, raw.FormatDiagnostics());
    }

    [TestMethod]
    [DataRow(@"select 'C:\u123' from system.dual()")]
    [DataRow(@"select 'C:\x1' from system.dual()")]
    public void DocumentedMalformedEscapeGuidance_ShouldRemainMQ1004Only(string query)
    {
        var result = Parse(query);

        Assert.IsFalse(result.Success);
        Assert.HasCount(1, result.Errors, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ1004_InvalidEscapeSequence, result.Errors.First().Code);
        Assert.IsEmpty(result.Warnings, result.FormatDiagnostics());
    }

    private static ParseResult Parse(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
