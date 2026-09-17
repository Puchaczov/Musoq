using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticREC091MissingTokenTargetingTests
{
    [TestMethod]
    [DynamicData(nameof(RecoveryCases))]
    public void MissingTokenRecovery_ShouldPreserveBoundariesAndApplyOnlyEmittedEdits(
        string caseId,
        string context,
        string query,
        DiagnosticCode expectedCode,
        string spanAnchor,
        int expectedSpanLength,
        string boundaryAnchor,
        string boundaryText,
        string expectedReplacement,
        string expectedRepairedQuery)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(caseId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(context));

        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, $"{caseId} unexpectedly parsed successfully. {result.FormatDiagnostics()}");
        Assert.HasCount(1, result.Diagnostics, $"{caseId}: {result.FormatDiagnostics()}");

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code, caseId);

        var expectedStart = ResolveSpanStart(query, spanAnchor);
        Assert.IsTrue(expectedStart >= 0, $"{caseId}: span anchor '{spanAnchor}' was not found.");
        var expectedLength = expectedSpanLength < 0
            ? query.Length - expectedStart
            : expectedSpanLength;
        Assert.AreEqual(new TextSpan(expectedStart, expectedLength), diagnostic.Span, caseId);

        if (!string.IsNullOrEmpty(boundaryAnchor))
        {
            var boundaryStart = query.IndexOf(boundaryAnchor, StringComparison.Ordinal);
            Assert.IsTrue(boundaryStart >= 0, $"{caseId}: boundary anchor '{boundaryAnchor}' was not found.");
            Assert.IsTrue(diagnostic.Span.End <= boundaryStart + boundaryText.Length,
                $"{caseId}: diagnostic consumed boundary '{boundaryText}'.");
            Assert.StartsWith(boundaryText, query[boundaryStart..], caseId);
        }

        var edits = diagnostic.SuggestedFixes
            .Where(action => action.TextEdit != null)
            .ToArray();

        if (string.IsNullOrEmpty(expectedReplacement))
        {
            Assert.IsEmpty(edits, $"{caseId}: a missing expression or identifier received an unsafe edit.");
            return;
        }

        Assert.HasCount(1, edits, caseId);
        var edit = edits[0].TextEdit!;
        Assert.AreEqual(DiagnosticActionKind.QuickFix, edits[0].Kind, caseId);
        Assert.AreEqual(expectedReplacement, edit.NewText, caseId);
        var repairedQuery = ApplyEdit(query, edit);
        Assert.AreEqual(expectedRepairedQuery, repairedQuery, caseId);

        var repaired = ParseWithDiagnostics(repairedQuery);
        Assert.IsTrue(repaired.Success, $"{caseId}: emitted edit did not produce valid grammar. {repaired.FormatDiagnostics()}");
        Assert.IsEmpty(repaired.Diagnostics, $"{caseId}: repaired query retained diagnostics.");
    }

    [TestMethod]
    [DynamicData(nameof(BracketedIdentifierControls))]
    public void BracketedIdentifierControls_ShouldReparseReservedAndSpaceContainingNames(
        string caseId,
        string query,
        string expectedIdentifier)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, $"{caseId}: {result.FormatDiagnostics()}");
        Assert.IsEmpty(result.Diagnostics, $"{caseId}: {result.FormatDiagnostics()}");
        Assert.Contains(expectedIdentifier, query, caseId);
    }

    public static IEnumerable<object[]> RecoveryCases()
    {
        yield return ["REC-091-A01", "alias.explicit-before-where", "select 1 from #system.dual() as where 1 = 1",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "<after:as>", 0, "where", "where", "", ""];
        yield return ["REC-091-A02", "alias.comment-before-where",
            "select 1 from #system.dual() as -- preserve boundary\r\nwhere 1 = 1",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "<after:as>", 0, "where", "where", "", ""];
        yield return ["REC-091-A03", "alias.invalid-after-as",
            "select 1 from #system.dual() as 123 where 1 = 1",
            DiagnosticCode.MQ2022_InvalidAlias, "123", 3, "where", "where", "", ""];
        yield return ["REC-091-A04", "alias.apply-before-multiline-where",
            "select 1 from source cross apply source.Column\r\nwhere 1 = 1",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "<after:source.Column>", 0, "where", "where", "", ""];
        yield return ["REC-091-A05", "alias.apply-before-block-comment-take",
            "select 1 from source cross apply source.Column /* preserve */ take 1",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "<after:source.Column>", 0, "take", "take", "", ""];
        yield return ["REC-091-A06", "alias.join-before-on",
            "select 1 from a.first() a inner join b.second() on 1 = 1",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "<after:second()>", 0, "on 1", "on", "", ""];
        yield return ["REC-091-A07", "alias.derived-before-where",
            "select * from (select 1 from #system.dual()) where 1 = 1",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "<after:#system.dual())>", 0, "where", "where", "", ""];
        yield return ["REC-091-A08", "alias.derived-at-eof",
            "select * from (select 1 from #system.dual())",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "<eof>", 0, "", "", "", ""];
        yield return ["REC-091-A09", "alias.values-before-order",
            "select * from values { ( Name: 'A' ) } order by 1",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "<after:) }>", 0, "order", "order", "", ""];
        yield return ["REC-091-A10", "alias.values-at-eof",
            "select * from values { ( Name: 'A' ) }",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "<eof>", 0, "", "", "", ""];
        yield return ["REC-091-A11", "alias.derived-explicit-as-before-take",
            "select * from (select 1 from #system.dual()) as take 1",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "<after:as>", 0, "take", "take", "", ""];
        yield return ["REC-091-A12", "alias.first-source-before-cross-join",
            "select 1 from a.first() cross join b.second()",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "<after:a.first()>", 0, "cross join", "cross join", "", ""];

        yield return ["REC-091-S01", "separator.between-before-where",
            "select 1 from #system.dual() where 1 between 2 where 1 = 1",
            DiagnosticCode.MQ2002_MissingToken, "where 1 = 1", 0, "where 1 = 1", "where", "", ""];
        yield return ["REC-091-S02", "separator.between-comment-before-take",
            "select 1 from #system.dual() where 1 between 2 /* keep */ take 1",
            DiagnosticCode.MQ2002_MissingToken, "take", 0, "take", "take", "", ""];
        yield return ["REC-091-S03", "separator.in-before-order",
            "select 1 from #system.dual() where 1 in order by 1",
            DiagnosticCode.MQ2002_MissingToken, "order", 0, "order", "order", "", ""];
        yield return ["REC-091-S04", "separator.in-at-eof",
            "select 1 from #system.dual() where 1 in",
            DiagnosticCode.MQ2002_MissingToken, "<eof>", 0, "", "", "", ""];
        yield return ["REC-091-S05", "separator.in-list-before-close",
            "select 1 from #system.dual() where 1 in (1 2) take 1",
            DiagnosticCode.MQ2018_MissingOperator, "2", 0, "take", "take", "", ""];
        yield return ["REC-091-S06", "separator.function-before-from",
            "select #system.dual(1 from #system.dual()",
            DiagnosticCode.MQ2021_UnclosedFunctionCall, "from #system", 0, "from #system", "from", "", ""];
        yield return ["REC-091-S07", "separator.function-before-close",
            "select #system.dual(1 2) from #system.dual()",
            DiagnosticCode.MQ2018_MissingOperator, "2", 0, "from #system", "from", "", ""];
        yield return ["REC-091-S08", "separator.values-rows",
            "select * from values { ( Name: 'A' ) { Name: 'B' } }",
            DiagnosticCode.MQ2001_UnexpectedToken, "{ Name: 'B' }", 1, "", "", "", ""];
        yield return ["REC-091-S09", "separator.values-fields",
            "select * from values { ( Name: 'A' Age: 1 ) }",
            DiagnosticCode.MQ2001_UnexpectedToken, "Age", 3, "", "", "", ""];
        yield return ["REC-091-S10", "separator.named-arguments",
            "select 1 from #system.dual(a: 1 /* keep */ b: 2)",
            DiagnosticCode.MQ2021_UnclosedFunctionCall, "b", 0, "b", "b", "", ""];

        yield return ["REC-091-D01", "delimiter.grouped-before-from",
            "select (1 + 2 from #system.dual()",
            DiagnosticCode.MQ2010_MissingClosingParenthesis, "from #system", 0, "from #system", "from", "", ""];
        yield return ["REC-091-D02", "delimiter.grouped-at-eof",
            "select (1 + 2",
            DiagnosticCode.MQ2010_MissingClosingParenthesis, "<eof>", 0, "", "", "", ""];
        yield return ["REC-091-D03", "delimiter.function-before-from",
            "select #system.dual(1 from #system.dual()",
            DiagnosticCode.MQ2021_UnclosedFunctionCall, "from #system", 0, "from #system", "from", "", ""];
        yield return ["REC-091-D04", "delimiter.function-at-eof",
            "select #system.dual(1",
            DiagnosticCode.MQ2021_UnclosedFunctionCall, "<eof>", 0, "", "", "", ""];
        yield return ["REC-091-D05", "delimiter.in-before-take",
            "select 1 from #system.dual() where 1 in (1, 2 take 1",
            DiagnosticCode.MQ2001_UnexpectedToken, "take", 4, "take", "take", "", ""];
        yield return ["REC-091-D06", "delimiter.scalar-subquery-at-eof",
            "select (select 1 from #system.dual() where 1 = 1",
            DiagnosticCode.MQ2001_UnexpectedToken, "<eof>", 0, "", "", "", ""];
        yield return ["REC-091-D07", "delimiter.derived-at-eof",
            "select * from (select 1 from #system.dual() where 1 = 1",
            DiagnosticCode.MQ2001_UnexpectedToken, "<eof>", 0, "", "", "", ""];
        yield return ["REC-091-D08", "delimiter.values-brace-at-eof",
            "select * from values { ( Name: 'A' )",
            DiagnosticCode.MQ2001_UnexpectedToken, "<eof>", 0, "", "", "", ""];
        yield return ["REC-091-D09", "delimiter.bracket-before-next-bracket",
            "select 1 as [case, 2 as [order], 3 as [Column With Spaces] from #system.dual()",
            DiagnosticCode.MQ2011_MissingClosingBracket, "[case", 12, "[order]", "[order]", "", ""];
        yield return ["REC-091-D10", "delimiter.bracket-at-eof",
            "select 1 as [Column With Spaces from #system.dual()",
            DiagnosticCode.MQ2011_MissingClosingBracket, "[Column", -1, "", "", "", ""];

        yield return ["REC-091-C01", "case.missing-when",
            "select case, 1 from #system.dual()",
            DiagnosticCode.MQ2027_MissingWhenClause, "case", 4, "case", "case", "", ""];
        yield return ["REC-091-C02", "case.missing-condition",
            "select case when then 1 else 2 end from #system.dual()",
            DiagnosticCode.MQ2001_UnexpectedToken, "then", 4, "then", "then", "", ""];
        yield return ["REC-091-C03", "case.missing-then-keyword",
            "select case when 1 = 1 else 2 end from #system.dual()",
            DiagnosticCode.MQ2001_UnexpectedToken, "else", 4, "else", "else", "", ""];
        yield return ["REC-091-C04", "case.missing-then-result",
            "select case when 1 = 1 then else 2 end from #system.dual()",
            DiagnosticCode.MQ2001_UnexpectedToken, "else", 4, "else", "else", "", ""];
        yield return ["REC-091-C05", "case.missing-else-keyword",
            "select case when 1 = 1 then 1 end from #system.dual()",
            DiagnosticCode.MQ2001_UnexpectedToken, "end", 3, "end", "end", "", ""];
        yield return ["REC-091-C06", "case.missing-else-result",
            "select case when 1 = 1 then 1 else end from #system.dual()",
            DiagnosticCode.MQ2001_UnexpectedToken, "end", 3, "end", "end", "", ""];
        yield return ["REC-091-C07", "case.missing-end-before-from",
            "select case when 1 = 1 then 1 else 2 from #system.dual()",
            DiagnosticCode.MQ2001_UnexpectedToken, "from #system", 4, "from #system", "from", "", ""];
        yield return ["REC-091-C08", "case.missing-end-at-eof",
            "select case when 1 = 1 then 1 else 2",
            DiagnosticCode.MQ2001_UnexpectedToken, "<eof>", 0, "", "", "", ""];
        yield return ["REC-091-C09", "case.multiline-missing-end",
            "select case\r\nwhen 1 = 1 then 1 else 2\r\nfrom #system.dual()",
            DiagnosticCode.MQ2001_UnexpectedToken, "from #system", 4, "from #system", "from", "", ""];

        yield return ["REC-091-K01", "keyword.select-typo",
            "SELCT 1 FROM #system.dual()",
            DiagnosticCode.MQ2001_UnexpectedToken, "SELCT", 5, "SELCT", "SELCT", "SELECT",
            "SELECT 1 FROM #system.dual()"];
        yield return ["REC-091-K02", "keyword.from-typo",
            "SELECT 1 FRMO #system.dual()",
            DiagnosticCode.MQ2004_MissingFromClause, "FRMO", 4, "FRMO", "FRMO", "FROM",
            "SELECT 1 FROM #system.dual()"];
        yield return ["REC-091-K03", "keyword.where-typo-with-bracketed-alias",
            "SELECT 1 FROM #system.dual() [where] WHRE 1 = 1",
            DiagnosticCode.MQ2001_UnexpectedToken, "WHRE", 4, "WHRE", "WHRE", "WHERE",
            "SELECT 1 FROM #system.dual() [where] WHERE 1 = 1"];
        yield return ["REC-091-K04", "keyword.group-typo-with-crlf",
            "SELECT 1 FROM #system.dual() d\r\nGROPU BY 1",
            DiagnosticCode.MQ2001_UnexpectedToken, "GROPU", 5, "GROPU", "GROPU", "GROUP",
            "SELECT 1 FROM #system.dual() d\r\nGROUP BY 1"];
        yield return ["REC-091-K05", "keyword.order-typo-with-literal",
            "SELECT 'ORDRE' AS Literal, 1 FROM #system.dual() d ORDRE BY 1",
            DiagnosticCode.MQ2001_UnexpectedToken, "ORDRE BY", 5, "ORDRE BY", "ORDRE", "ORDER",
            "SELECT 'ORDRE' AS Literal, 1 FROM #system.dual() d ORDER BY 1"];
        yield return ["REC-091-K06", "keyword.having-typo-with-comment",
            "SELECT 1 FROM #system.dual() d GROUP BY ALL HAVIGN /* preserve */ 1 = 1",
            DiagnosticCode.MQ2001_UnexpectedToken, "HAVIGN", 6, "HAVIGN", "HAVIGN", "HAVING",
            "SELECT 1 FROM #system.dual() d GROUP BY ALL HAVING /* preserve */ 1 = 1"];
        yield return ["REC-091-K07", "keyword.join-typo",
            "SELECT 1 FROM #system.dual() a JION #system.dual() b ON 1 = 1",
            DiagnosticCode.MQ2001_UnexpectedToken, "JION", 4, "JION", "JION", "JOIN",
            "SELECT 1 FROM #system.dual() a JOIN #system.dual() b ON 1 = 1"];
    }

    public static IEnumerable<object[]> BracketedIdentifierControls()
    {
        yield return ["REC-091-Q01", "select 1 as [case], 2 as [order], 3 as [Column With Spaces] from #system.dual()",
            "[Column With Spaces]"];
        yield return ["REC-091-Q02", "select 1 from source cross apply source.Column as [cross apply] take 1",
            "[cross apply]"];
        yield return ["REC-091-Q03", "select [where] from source", "[where]"];
        yield return ["REC-091-Q04", "select * from (select 1 from #system.dual()) [derived alias]",
            "[derived alias]"];
    }

    private static int ResolveSpanStart(string query, string spanAnchor)
    {
        if (spanAnchor == "<eof>")
            return query.Length;

        if (spanAnchor.StartsWith("<after:", StringComparison.Ordinal) &&
            spanAnchor.EndsWith('>'))
        {
            var precedingText = spanAnchor[7..^1];
            var precedingStart = query.IndexOf(precedingText, StringComparison.Ordinal);
            Assert.IsTrue(precedingStart >= 0, $"Span anchor '{spanAnchor}' was not found.");
            return precedingStart + precedingText.Length;
        }

        return query.IndexOf(spanAnchor, StringComparison.Ordinal);
    }

    private static string ApplyEdit(string query, TextEdit edit)
    {
        Assert.IsTrue(edit.Span.Start >= 0 && edit.Span.End <= query.Length);
        return query[..edit.Span.Start] + edit.NewText + query[edit.Span.End..];
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
