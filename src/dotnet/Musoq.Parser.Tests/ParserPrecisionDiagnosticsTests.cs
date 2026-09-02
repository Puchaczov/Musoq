using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ParserPrecisionDiagnosticsTests
{
    [TestMethod]
    public void BareIntegerInclusiveBoundaries_ShouldHaveIntType()
    {
        const string query = "select 2147483647, -2147483648 from system.dual()";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var statements = (StatementsArrayNode)result.Root!.Expression;
        var queryNode = ((SingleSetNode)statements.Statements.Single().Node).Query;
        var returnTypes = queryNode.Select.Fields.Select(field => field.Expression.ReturnType).ToArray();
        CollectionAssert.AreEqual(new[] { typeof(int), typeof(int) }, returnTypes, result.FormatDiagnostics());
    }

    [TestMethod]
    public void BareIntegerOneAboveInt32Maximum_ShouldReportWholeLiteralOutOfRange()
    {
        const string validSeed = "select 2147483647 from system.dual()";
        var seedResult = ParseWithDiagnostics(validSeed);

        Assert.IsTrue(seedResult.Success, seedResult.FormatDiagnostics());

        const string mutatedLiteral = "2147483648";
        var mutatedQuery = $"select {mutatedLiteral} from system.dual()";
        var result = ParseWithDiagnostics(mutatedQuery);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ1009_NumericLiteralOutOfRange, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(mutatedQuery.IndexOf(mutatedLiteral, StringComparison.Ordinal), diagnostic.Span.Start);
        Assert.AreEqual(mutatedLiteral.Length, diagnostic.Span.Length);
    }

    [TestMethod]
    public void AlternativeBaseLiteralSeed_ShouldParseWithoutDiagnostics()
    {
        const string query =
            "select 0xFF, 0XFF, 0b1010, 0B1010, 0o77, 0O77, 0x0 from system.dual()";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
    }

    [TestMethod]
    public void HexLiteralWithNonHexDigit_ShouldReportWholeLiteralAsInvalid()
    {
        const string validSeed = "select 0xFF from system.dual()";
        var seedResult = ParseWithDiagnostics(validSeed);

        Assert.IsTrue(seedResult.Success, seedResult.FormatDiagnostics());
        Assert.IsEmpty(seedResult.Diagnostics, seedResult.FormatDiagnostics());

        const string mutatedLiteral = "0xFG";
        var mutatedQuery = $"select {mutatedLiteral} from system.dual()";
        var result = ParseWithDiagnostics(mutatedQuery);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ1006_InvalidHexNumber, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(mutatedQuery.IndexOf(mutatedLiteral, StringComparison.Ordinal), diagnostic.Span.Start);
        Assert.AreEqual(mutatedLiteral.Length, diagnostic.Span.Length);
    }

    [TestMethod]
    public void BinaryLiteralWithTrailingInvalidDigit_ShouldReportWholeInvalidLiteral()
    {
        const string invalidLiteral = "0b1012";
        const string query =
            "select 0xFF, 0XFF, 0b1012, 0B1010, 0o77, 0O77, 0x0 from system.dual()";

        var result = ParseWithDiagnostics(query);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ1007_InvalidBinaryNumber, diagnostic.Code, result.FormatDiagnostics());
        Assert.AreEqual(
            new TextSpan(query.IndexOf(invalidLiteral, StringComparison.Ordinal), invalidLiteral.Length),
            diagnostic.Span);
    }

    [TestMethod]
    public void ReservedCaseWithoutBrackets_ShouldReportMissingWhenAtCaseKeyword()
    {
        const string query = "select case, [order], [Column With Spaces] from schema.method()";
        var result = ParseWithDiagnostics(query);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ2027_MissingWhenClause, diagnostic.Code, result.FormatDiagnostics());
        Assert.AreEqual(query.IndexOf("case", StringComparison.Ordinal), diagnostic.Span.Start);
        Assert.AreEqual("case".Length, diagnostic.Span.Length);
        StringAssert.Contains(diagnostic.Message, "[case]");
    }

    [TestMethod]
    [DataRow("999999999999999999999999999999999999999")]
    [DataRow("0xFFFFFFFFFFFFFFFF1")]
    [DataRow("0b11111111111111111111111111111111111111111111111111111111111111111")]
    [DataRow("0o7777777777777777777777")]
    public void NumericLiteralOverflow_ShouldIdentifyTheWholeLiteral(string literal)
    {
        var query = $"select {literal} from #test.rows()";
        var result = ParseWithDiagnostics(query);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ1009_NumericLiteralOutOfRange, diagnostic.Code);
        Assert.AreEqual(query.IndexOf(literal, StringComparison.Ordinal), diagnostic.Span.Start);
        Assert.AreEqual(literal.Length, diagnostic.Span.Length);
    }

    [TestMethod]
    public void NumericLiteralOverflow_StrictParsing_ShouldThrowTypedDiagnostic()
    {
        const string literal = "0xFFFFFFFFFFFFFFFF1";
        var query = $"select {literal} from #test.rows()";

        var exception = Assert.Throws<SyntaxException>(() =>
            new Parser(new Lexer(query, true)).ComposeAll());

        Assert.AreEqual(DiagnosticCode.MQ1009_NumericLiteralOutOfRange, exception.Code);
        Assert.AreEqual(query.IndexOf(literal, StringComparison.Ordinal), exception.Span!.Value.Start);
        Assert.AreEqual(literal.Length, exception.Span.Value.Length);
    }

    [TestMethod]
    public void NumericLiteralOverflow_Recovery_ShouldKeepTheFollowingStatementRecoverable()
    {
        const string literal = "0xFFFFFFFFFFFFFFFF1";
        var query = $"select {literal} from #test.rows(); select 1 from #test.rows()";

        var result = ParseWithDiagnostics(query);

        Assert.IsNotNull(result.Root);
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ1009_NumericLiteralOutOfRange, result.Diagnostics[0].Code);
    }

    [TestMethod]
    public void ContainsEmptyList_ShouldUseTheEmptyPredicateDiagnostic()
    {
        var result = ParseWithDiagnostics("select 1 from #test.rows() where Name contains ()");

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ2037_EmptyPredicateListNotAllowed, diagnostic.Code);
    }

    [TestMethod]
    public void NotInEmptyList_ShouldUseTheEmptyPredicateDiagnostic()
    {
        var result = ParseWithDiagnostics("select 1 from #test.rows() where Name not in ()");

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ2037_EmptyPredicateListNotAllowed, diagnostic.Code);
    }

    [TestMethod]
    public void InEmptyList_ShouldUseTheEmptyPredicateDiagnostic()
    {
        var result = ParseWithDiagnostics("select 1 from #test.rows() where Name in ()");

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ2037_EmptyPredicateListNotAllowed, diagnostic.Code);
    }

    [TestMethod]
    [DataRow("take -1", "TAKE")]
    [DataRow("skip -1", "SKIP")]
    [DataRow("take 3.5", "TAKE")]
    [DataRow("skip 'two'", "SKIP")]
    public void InvalidSliceCount_ShouldUseTheSliceDiagnostic(string clause, string clauseName)
    {
        var result = ParseWithDiagnostics($"select 1 from #test.rows() {clause}");

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ2038_InvalidSliceCount, diagnostic.Code);
        StringAssert.Contains(diagnostic.Message, clauseName);
    }

    [TestMethod]
    public void TieBreakOutsideAsOfJoin_ShouldUseTheTieBreakDiagnostic()
    {
        var result = ParseWithDiagnostics(
            "select 1 from a.first() a inner join b.second() b on a.Id = b.Id tie break by b.Score");

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ2039_TieBreakRequiresAsOfJoin, diagnostic.Code);
    }

    [TestMethod]
    [DataRow("analyze select 1 from #test.rows()")]
    [DataRow("explain select 1 from #test.rows()")]
    [DataRow("profile")]
    [DataRow("profile table temp {Name: string}")]
    public void InvalidDiagnosticCommand_ShouldUseTheCommandDiagnostic(string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ2040_InvalidDiagnosticCommand, diagnostic.Code);
    }

    [TestMethod]
    public void DuplicateStarModifier_ShouldUseTheStarOrderDiagnostic()
    {
        var result = ParseWithDiagnostics("select * exclude (Name) exclude (City) from #test.rows()");

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ2041_InvalidStarModifierOrder, diagnostic.Code);
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var diagnostics = new DiagnosticBag();
        return new Parser(new Lexer(query, true, recoverOnError: true), diagnostics).ParseWithDiagnostics();
    }
}
