using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class BracketedIdentifierBoundaryDiagnosticTests
{
    [TestMethod]
    public void MissingFirstClosingBracket_ShouldNotConsumeLaterIdentifierDelimiter()
    {
        const string validSeed =
            "select 1 as [case], 2 as [order], 3 as [Column With Spaces] from system.dual()";
        const string mutatedQuery =
            "select 1 as [case, 2 as [order], 3 as [Column With Spaces] from system.dual()";

        var seedResult = ParseWithDiagnostics(validSeed);

        Assert.IsTrue(seedResult.Success, seedResult.FormatDiagnostics());
        Assert.IsEmpty(seedResult.Diagnostics, seedResult.FormatDiagnostics());

        var result = ParseWithDiagnostics(mutatedQuery);

        Assert.IsFalse(result.Success, "The unterminated first bracketed identifier was accepted.");
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ2011_MissingClosingBracket, diagnostic.Code);
        Assert.AreEqual(new TextSpan(12, 12), diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, "Unterminated bracketed identifier");
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
