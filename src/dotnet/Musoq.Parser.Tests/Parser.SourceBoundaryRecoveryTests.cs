using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ParserSourceBoundaryRecoveryTests
{
    [TestMethod]
    public void MissingApplyAlias_ShouldStopBeforeEverySourceBoundary()
    {
        var boundaries = new[]
        {
            (Suffix: "on 1 = 1", Boundary: "ON"),
            (Suffix: "with ordinality", Boundary: "WITH ORDINALITY"),
            (Suffix: "where 1 = 1", Boundary: "WHERE"),
            (Suffix: "group by 1", Boundary: "GROUP BY"),
            (Suffix: "having 1 = 1", Boundary: "HAVING"),
            (Suffix: "window w as ()", Boundary: "WINDOW"),
            (Suffix: "qualify 1 = 1", Boundary: "QUALIFY"),
            (Suffix: "order by 1", Boundary: "ORDER BY"),
            (Suffix: "skip 1", Boundary: "SKIP"),
            (Suffix: "take 1", Boundary: "TAKE"),
            (Suffix: "union select 1 from source", Boundary: "UNION"),
            (Suffix: "union all select 1 from source", Boundary: "UNION ALL"),
            (Suffix: "except select 1 from source", Boundary: "EXCEPT"),
            (Suffix: "intersect select 1 from source", Boundary: "INTERSECT"),
            (Suffix: "cross apply source.Column item", Boundary: "CROSS APPLY"),
            (Suffix: "cross join source", Boundary: "CROSS JOIN"),
            (Suffix: "; select 1 from source", Boundary: ";"),
            (Suffix: string.Empty, Boundary: "the end of the statement")
        };

        foreach (var (suffix, boundary) in boundaries)
        {
            var query = $"select 1 from a.first() a cross apply a.Column {suffix}";
            var diagnostic = GetSingleDiagnostic(query);

            Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, diagnostic.Code, boundary);
            Assert.AreEqual($"The CROSS APPLY source requires an alias before {boundary}.", diagnostic.Message,
                boundary);
            Assert.AreEqual(query.IndexOf("a.Column", StringComparison.Ordinal) + "a.Column".Length,
                diagnostic.Span.Start, boundary);
            Assert.AreEqual(0, diagnostic.Span.Length, boundary);
        }
    }

    [TestMethod]
    public void MissingApplyAliasInsideParentheses_ShouldStopBeforeRightParenthesis()
    {
        const string query = "select 1 from (select 1 from a.first() a cross apply a.Column) outerSource";
        var diagnostic = GetSingleDiagnostic(query);

        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, diagnostic.Code);
        Assert.AreEqual("The CROSS APPLY source requires an alias before ).", diagnostic.Message);
        Assert.AreEqual(query.IndexOf("a.Column", StringComparison.Ordinal) + "a.Column".Length,
            diagnostic.Span.Start);
        Assert.AreEqual(0, diagnostic.Span.Length);
    }

    [TestMethod]
    public void MissingJoinAlias_ShouldUseTheSameBoundaryContract()
    {
        var boundaries = new[]
        {
            (Suffix: "where 1 = 1", Boundary: "WHERE"),
            (Suffix: "order by 1", Boundary: "ORDER BY"),
            (Suffix: "take 1", Boundary: "TAKE"),
            (Suffix: string.Empty, Boundary: "the end of the statement")
        };

        foreach (var (suffix, boundary) in boundaries)
        {
            var query = $"select 1 from a.first() a inner join b.second() {suffix}";
            var diagnostic = GetSingleDiagnostic(query);

            Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, diagnostic.Code, boundary);
            Assert.AreEqual($"The INNER JOIN source requires an alias before {boundary}.", diagnostic.Message,
                boundary);
            Assert.AreEqual(query.IndexOf("second()", StringComparison.Ordinal) + "second()".Length,
                diagnostic.Span.Start, boundary);
            Assert.AreEqual(0, diagnostic.Span.Length, boundary);
        }
    }

    [TestMethod]
    public void ExplicitAsBeforeEveryBoundary_ShouldUseInsertionPointAfterAs()
    {
        var boundaries = new[]
        {
            "on 1 = 1", "with ordinality", "where 1 = 1", "group by 1", "having 1 = 1",
            "window w as ()", "qualify 1 = 1", "order by 1", "skip 1", "take 1", "union select 1 from source",
            "; select 1 from source", string.Empty
        };

        foreach (var suffix in boundaries)
        {
            var query = $"select 1 from a.first() a cross apply a.Column as {suffix}";
            var result = ParseWithDiagnostics(query);
            var diagnostic = result.Diagnostics.Single();
            var asEnd = query.IndexOf(" as ", query.IndexOf("Column", StringComparison.Ordinal) + "Column".Length,
                StringComparison.OrdinalIgnoreCase) + 3;

            Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, diagnostic.Code, suffix);
            Assert.AreEqual("The source requires an alias identifier after AS.", diagnostic.Message, suffix);
            Assert.AreEqual(asEnd, diagnostic.Span.Start, suffix);
            Assert.AreEqual(0, diagnostic.Span.Length, suffix);
        }
    }

    [TestMethod]
    public void CorrectlyAliasedJoinWithoutOn_ShouldUseTypedJoinConditionDiagnostic()
    {
        var queries = new[]
        {
            "select 1 from a.first() a inner join b.second() b where 1 = 1",
            "select 1 from a.first() a inner join b.second() b take 1",
            "select 1 from a.first() a inner join b.second() b",
            "select 1 from a.first() a inner join b.second() b on",
            "select 1 from a.first() a inner join b.second() b on where 1 = 1"
        };

        foreach (var query in queries)
        {
            var result = ParseWithDiagnostics(query);

            Assert.HasCount(1, result.Diagnostics, query);
            Assert.AreEqual(DiagnosticCode.MQ2007_InvalidJoinCondition, result.Diagnostics[0].Code, query);
            Assert.IsFalse(result.Diagnostics.Any(d => d.Code is DiagnosticCode.MQ2001_UnexpectedToken
                or DiagnosticCode.MQ2030_UnsupportedSyntax), query);
        }
    }

    [TestMethod]
    public void MissingJoinAlias_ShouldTakePrecedenceOverMissingOn()
    {
        const string query = "select 1 from a.first() a inner join b.second() where 1 = 1";
        var result = ParseWithDiagnostics(query);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, result.Diagnostics[0].Code);
        Assert.IsFalse(result.Diagnostics.Any(d => d.Code == DiagnosticCode.MQ2007_InvalidJoinCondition));
    }

    [TestMethod]
    public void ValidApplyOrdinality_ShouldRetainExistingTypedOrdinalityHandling()
    {
        var valid = ParseWithDiagnostics("select item.Ordinal from a.first() a cross apply a.Numbers item with ordinality");
        var invalid = ParseWithDiagnostics("select item.Ordinal from a.first() a cross apply a.Numbers item with invalid");

        Assert.IsTrue(valid.Success, valid.FormatDiagnostics());
        Assert.IsEmpty(valid.Diagnostics, valid.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2002_MissingToken, invalid.Diagnostics.Single().Code);
    }

    [TestMethod]
    public void MissingAliasRecovery_ShouldContinueAfterSemicolonCommentsAndCrLf()
    {
        const string query = "select 1 from a.first() a\r\ncross apply a.Column -- boundary comment\r\ntake 1;\r\nselect 1 from source";
        var result = ParseWithDiagnostics(query);

        Assert.IsNotNull(result.Root);
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, result.Diagnostics[0].Code);

        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(result.Root!.Expression);
        Assert.HasCount(1, statements.Statements);
    }

    [TestMethod]
    public void StrictAndRecoveryParsing_ShouldMatchForBoundaryErrors()
    {
        var queries = new[]
        {
            "select 1 from a.first() a cross apply a.Column where 1 = 1",
            "select 1 from a.first() a cross apply a.Column; select 1 from source",
            "select 1 from a.first() a inner join b.second() where 1 = 1"
        };

        foreach (var query in queries)
        {
            var recovery = GetSingleDiagnostic(query);
            var strict = Assert.Throws<SyntaxException>(() => new Parser(new Lexer(query, true)).ComposeAll());

            Assert.AreEqual(recovery.Code, strict.Code, query);
            Assert.AreEqual(recovery.Message, strict.Message, query);
            Assert.AreEqual(recovery.Span, strict.Span, query);
        }
    }

    [TestMethod]
    public void SharedAndSeparateLexerParserBags_ShouldNotDuplicateBoundaryDiagnostic()
    {
        const string query = "select 1 from a.first() a cross apply a.Column take 1";
        var separateLexer = new Lexer(query, true);
        var separateParserBag = new DiagnosticBag { SourceText = separateLexer.SourceText };
        var separateResult = new Parser(separateLexer, separateParserBag).ParseWithDiagnostics();

        var sharedLexer = new Lexer(query, true);
        var sharedResult = new Parser(sharedLexer, sharedLexer.Diagnostics).ParseWithDiagnostics();

        Assert.HasCount(1, separateResult.Diagnostics);
        Assert.HasCount(1, separateParserBag);
        Assert.IsEmpty(separateLexer.Diagnostics);
        Assert.HasCount(1, sharedResult.Diagnostics);
        Assert.HasCount(1, sharedLexer.Diagnostics);
        Assert.AreEqual(separateResult.Diagnostics[0].Code, sharedResult.Diagnostics[0].Code);
        Assert.AreEqual(separateResult.Diagnostics[0].Span, sharedResult.Diagnostics[0].Span);
    }

    [TestMethod]
    public void BracketedAndContextualAliases_ShouldRemainValidAcrossBoundaries()
    {
        var bracketed = ParseWithDiagnostics(
            "select 1 from source cross apply source.Column [where] take 1");
        var contextual = ParseWithDiagnostics(
            "select 1 from source cross apply source.Column as wheres take 1");
        var bracketedMultiword = ParseWithDiagnostics(
            "select 1 from source cross apply source.Column as [cross apply] take 1");

        Assert.IsTrue(bracketed.Success, bracketed.FormatDiagnostics());
        Assert.IsEmpty(bracketed.Diagnostics, bracketed.FormatDiagnostics());
        Assert.IsTrue(contextual.Success, contextual.FormatDiagnostics());
        Assert.IsEmpty(contextual.Diagnostics, contextual.FormatDiagnostics());
        Assert.IsTrue(bracketedMultiword.Success, bracketedMultiword.FormatDiagnostics());
        Assert.IsEmpty(bracketedMultiword.Diagnostics, bracketedMultiword.FormatDiagnostics());
    }

    [TestMethod]
    public void ChainedApplyWithAliases_ShouldRemainValidAfterBoundaryChanges()
    {
        var result = ParseWithDiagnostics(
            "select item2.Value from source cross apply source.Column item cross apply item.Next item2 take 1");

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
    }

    private static Diagnostic GetSingleDiagnostic(string query)
    {
        var result = ParseWithDiagnostics(query);
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        return result.Diagnostics[0];
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var diagnostics = new DiagnosticBag();
        return new Parser(new Lexer(query, true), diagnostics).ParseWithDiagnostics();
    }
}
