using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticREC108InsertionDuplicationTests
{
    [TestMethod]
    public void InsertionAndDuplicationCandidates_ShouldHonorFrozenContract()
    {
        foreach (var candidate in CandidateCases())
        {
            var seed = ParseWithDiagnostics(candidate.SeedQuery);
            Assert.IsTrue(seed.Success, $"{candidate.CaseId}: valid seed produced diagnostics. {seed.FormatDiagnostics()}");
            Assert.IsEmpty(seed.Diagnostics, $"{candidate.CaseId}: valid seed produced diagnostics.");

            var result = ParseWithDiagnostics(candidate.ResultQuery);
            if (candidate.ExpectedCode is null)
            {
                Assert.IsTrue(result.Success, $"{candidate.CaseId}: quiet or legitimate repetition failed. {result.FormatDiagnostics()}");
                Assert.IsEmpty(result.Diagnostics, $"{candidate.CaseId}: quiet or legitimate repetition produced diagnostics.");
                continue;
            }

            Assert.IsFalse(result.Success, $"{candidate.CaseId}: accidental insertion unexpectedly parsed successfully.");
            Assert.HasCount(1, result.Diagnostics, $"{candidate.CaseId}: {result.FormatDiagnostics()}");

            var diagnostic = result.Diagnostics.Single();
            Assert.AreEqual(candidate.ExpectedCode.Value, diagnostic.Code, candidate.CaseId);
            Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity, candidate.CaseId);
            Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase, candidate.CaseId);
            Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind, candidate.CaseId);
            Assert.IsTrue(
                diagnostic.Span.Start >= 0 && diagnostic.Span.End <= candidate.ResultQuery.Length,
                $"{candidate.CaseId}: diagnostic span escaped the mutated query.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference), candidate.CaseId);
            Assert.IsNotEmpty(diagnostic.SuggestedFixes, candidate.CaseId);
            Assert.IsNotNull(diagnostic.ContextSnippet, candidate.CaseId);
        }
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainThirtyCasesAcrossFiveRecoveryFamilies()
    {
        var candidates = CandidateCases();

        Assert.HasCount(30, candidates);
        Assert.HasCount(14, candidates.Where(static candidate => candidate.ExpectedCode is not null));
        Assert.HasCount(5, candidates.Where(static candidate => candidate.ExpectedClassification == "valid_suspicious"));
        Assert.HasCount(11, candidates.Where(static candidate => candidate.ExpectedClassification == "valid_ordinary"));
        Assert.HasCount(16, candidates.Where(static candidate => candidate.ExpectedCode is null));
        CollectionAssert.AllItemsAreUnique(candidates.Select(static candidate => candidate.CaseId).ToArray());

        foreach (var family in Families)
            Assert.HasCount(6, candidates.Where(candidate => candidate.Family == family), $"{family} must contain six registered cases.");
    }

    [TestMethod]
    public void CandidateMetadata_ShouldDeclareMutationBoundaryAndClassificationForEveryCase()
    {
        foreach (var candidate in CandidateCases())
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.RootCause), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.InsertionPoint), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.ExpectedBoundaryToken), candidate.CaseId);
            Assert.IsTrue(candidate.SeedQuery.Contains($"/* {candidate.CaseId} */", StringComparison.Ordinal), candidate.CaseId);
            if (candidate.ExpectedCode is null)
                Assert.IsTrue(candidate.ExpectedClassification is "valid_ordinary" or "valid_suspicious", candidate.CaseId);
        }
    }

    private static readonly string[] Families =
    [
        "INSERTIONS",
        "DUPLICATIONS",
        "PASTED_FRAGMENTS",
        "QUIET_CONTROLS",
        "ENTRY_POINTS"
    ];

    private static IReadOnlyList<RecoveryCase> CandidateCases() =>
    [
        Invalid(
            "REC-108-I01", "INSERTIONS",
            "select 1 from #system.dual()",
            "from", ", from",
            DiagnosticCode.MQ2014_TrailingComma,
            "extra-select-list-comma", "SELECT list before FROM", "FROM"),
        Invalid(
            "REC-108-I02", "INSERTIONS",
            "select 1 from #system.dual() order by 1",
            "order by ", "order by , ",
            DiagnosticCode.MQ2015_LeadingComma,
            "extra-order-list-comma", "ORDER BY list start", "first order expression"),
        Invalid(
            "REC-108-I03", "INSERTIONS",
            "select (1) from #system.dual()",
            "from", ") from",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "extra-closing-delimiter", "after parenthesized projection", "FROM"),
        Invalid(
            "REC-108-I04", "INSERTIONS",
            "select * exclude (Name) from #system.dual()",
            "from", "exclude (Age) from",
            DiagnosticCode.MQ2041_InvalidStarModifierOrder,
            "duplicated-star-modifier", "star modifier chain", "next clause"),
        Invalid(
            "REC-108-I05", "INSERTIONS",
            "select 1 from #system.dual() where 1 = 1",
            "where 1 = 1", "where 1 = 1 where 2 = 2",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "duplicated-where-clause", "query WHERE clause", "end of predicate"),
        Invalid(
            "REC-108-I06-v2", "INSERTIONS",
            "select 1 from #system.dual() order by 1",
            "order by 1", "order by 1 order by 2",
            DiagnosticCode.MQ2009_InvalidOrderByExpression,
            "duplicated-order-clause", "query ORDER BY clause", "end of ordering"),

        Invalid(
            "REC-108-D01", "DUPLICATIONS",
            "select #system.dual(1) from #system.dual()",
            "#system.dual(1)", "#system.dual(1))",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "duplicated-function-closing-delimiter", "function call boundary", "FROM"),
        Invalid(
            "REC-108-D02", "DUPLICATIONS",
            "select 1 from #system.dual() group by 1",
            "group by 1", "group by 1 group by 2",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "duplicated-group-by-clause", "query GROUP BY clause", "end of grouping"),
        Invalid(
            "REC-108-D03", "DUPLICATIONS",
            "select 1 from #system.dual() take 1",
            "take 1", "take 1 take 2",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "duplicated-take-modifier", "query TAKE modifier", "end of statement"),
        ValidSuspicious(
            "REC-108-D04", "DUPLICATIONS",
            "select #system.dual(1) from #system.dual()",
            "#system.dual(1)", "#system.dual(1, 1)",
            "repeated-function-argument", "function argument list", "FROM"),
        ValidSuspicious(
            "REC-108-D05", "DUPLICATIONS",
            "select 1 from #system.dual() group by 1",
            "group by 1", "group by 1, 1",
            "repeated-grouping-expression", "GROUP BY expression list", "end of grouping"),
        ValidSuspicious(
            "REC-108-D06", "DUPLICATIONS",
            "select 1 from #system.dual() where 1 = 1",
            "where 1 = 1", "where 1 = 1 and 1 = 1",
            "repeated-predicate-expression", "WHERE expression", "end of predicate"),

        ValidSuspicious(
            "REC-108-P01-v2", "PASTED_FRAGMENTS",
            "select 1 from #system.dual()",
            "#system.dual()", "#system.dual(); select 2 from #system.dual()",
            "adjacent-pasted-select-fragment", "first statement boundary", "semicolon or end of input"),
        Invalid(
            "REC-108-P02", "PASTED_FRAGMENTS",
            "select 1 from #system.dual()",
            "#system.dual()", "#system.dual() from #system.dual()",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "adjacent-pasted-from-fragment", "FROM source boundary", "end of statement"),
        Invalid(
            "REC-108-P03", "PASTED_FRAGMENTS",
            "select 1 from #system.dual() group by 1 having 1 = 1",
            "having 1 = 1", "having 1 = 1 having 2 = 2",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "adjacent-pasted-having-fragment", "HAVING clause boundary", "end of grouping"),
        Invalid(
            "REC-108-P04", "PASTED_FRAGMENTS",
            "select 1 from #system.dual() order by 1 take 1",
            "take 1", "take 1 order by 2",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "adjacent-pasted-order-fragment", "slice modifier boundary", "end of statement"),
        ValidOrdinary(
            "REC-108-P05", "PASTED_FRAGMENTS",
            "select 1 from #system.dual()",
            "from", "from /* SELECT, ) WHERE JOIN */",
            "comment-shields-pasted-keywords", "FROM source boundary", "source expression"),
        ValidOrdinary(
            "REC-108-P06", "PASTED_FRAGMENTS",
            "select 'safe' from #system.dual()",
            "'safe'", "'select, ) where join'",
            "literal-shields-pasted-keywords", "string literal contents", "FROM"),

        ValidOrdinary(
            "REC-108-Q01", "QUIET_CONTROLS",
            "select 1 from #system.dual()",
            "select 1", "select 1 /* , ) SELECT WHERE JOIN */",
            "comment-shields-structural-punctuation", "projection comment", "FROM"),
        ValidOrdinary(
            "REC-108-Q02", "QUIET_CONTROLS",
            "select 'safe' from #system.dual()",
            "'safe'", "'/* select */ ; where )'",
            "literal-shields-comment-and-semicolon", "string literal contents", "FROM"),
        ValidOrdinary(
            "REC-108-Q03", "QUIET_CONTROLS",
            "select 'safe' from #system.dual()",
            "'safe'", "r'RENAME (x) REPLACE, SELECT'",
            "raw-literal-shields-modifiers", "raw string literal contents", "FROM"),
        ValidOrdinary(
            "REC-108-Q04", "QUIET_CONTROLS",
            "select 1 from #system.dual()",
            "select 1", "select 1, 1",
            "legitimate-repeated-projection", "SELECT projection list", "FROM"),
        ValidOrdinary(
            "REC-108-Q05", "QUIET_CONTROLS",
            "select * from #system.dual()",
            "select *", "select * replace (1 as First, 1 as Second)",
            "legitimate-repeated-replacement-expression", "REPLACE modifier list", "FROM"),
        ValidOrdinary(
            "REC-108-Q06", "QUIET_CONTROLS",
            "select 1 from #system.dual() order by 1",
            "order by 1", "order by 1, 1",
            "legitimate-repeated-order-expression", "ORDER BY expression list", "end of ordering"),

        ValidSuspicious(
            "REC-108-E01", "ENTRY_POINTS",
            "select 1 from #system.dual()",
            "#system.dual()", "#system.dual(); select 2 from #system.dual()",
            "semicolon-separated-executable-script", "script statement boundary", "next executable statement"),
        ValidOrdinary(
            "REC-108-E02", "ENTRY_POINTS",
            "select 1 from #system.dual()",
            "select 1", "table Jobs { Id: int }; select 1",
            "declaration-before-query", "declaration and executable boundary", "SELECT"),
        ValidOrdinary(
            "REC-108-E03", "ENTRY_POINTS",
            "select 1 from #system.dual()",
            "select 1", "enum State : int { Ready = 1 }; select 1",
            "enum-declaration-before-query", "declaration and executable boundary", "SELECT"),
        ValidOrdinary(
            "REC-108-E04", "ENTRY_POINTS",
            "select 1 from #system.dual()",
            "#system.dual()", "#system.dual();",
            "optional-trailing-statement-semicolon", "statement terminator", "end of input"),
        Invalid(
            "REC-108-E05", "ENTRY_POINTS",
            "select 1 from #system.dual()",
            "select 1 from #system.dual()", ";",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "semicolon-without-statement", "entry point start", "statement keyword"),
        Invalid(
            "REC-108-E06", "ENTRY_POINTS",
            "select 1 from #system.dual()",
            "select 1 from #system.dual()", ";;",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "duplicated-empty-statement", "entry point start", "statement keyword")
    ];

    private static RecoveryCase Invalid(
        string caseId,
        string family,
        string seed,
        string before,
        string after,
        DiagnosticCode expectedCode,
        string rootCause,
        string insertionPoint,
        string expectedBoundaryToken) =>
        Create(caseId, family, seed, before, after, "invalid", expectedCode, rootCause, insertionPoint, expectedBoundaryToken);

    private static RecoveryCase ValidSuspicious(
        string caseId,
        string family,
        string seed,
        string before,
        string after,
        string rootCause,
        string insertionPoint,
        string expectedBoundaryToken) =>
        Create(caseId, family, seed, before, after, "valid_suspicious", null, rootCause, insertionPoint, expectedBoundaryToken);

    private static RecoveryCase ValidOrdinary(
        string caseId,
        string family,
        string seed,
        string before,
        string after,
        string rootCause,
        string insertionPoint,
        string expectedBoundaryToken) =>
        Create(caseId, family, seed, before, after, "valid_ordinary", null, rootCause, insertionPoint, expectedBoundaryToken);

    private static RecoveryCase Create(
        string caseId,
        string family,
        string seed,
        string before,
        string after,
        string expectedClassification,
        DiagnosticCode? expectedCode,
        string rootCause,
        string insertionPoint,
        string expectedBoundaryToken)
    {
        var marker = $" /* {caseId} */";
        var seedQuery = seed + marker;
        var resultQuery = ReplaceOnce(seed, before, after) + marker;
        return new RecoveryCase(
            caseId,
            family,
            seedQuery,
            resultQuery,
            expectedClassification,
            expectedCode,
            rootCause,
            insertionPoint,
            expectedBoundaryToken);
    }

    private static string ReplaceOnce(string source, string before, string after)
    {
        var start = source.IndexOf(before, StringComparison.Ordinal);
        if (start < 0 || source.IndexOf(before, start + before.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException($"Expected exactly one '{before}' occurrence in '{source}'.");

        return source[..start] + after + source[(start + before.Length)..];
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }

    private sealed record RecoveryCase(
        string CaseId,
        string Family,
        string SeedQuery,
        string ResultQuery,
        string ExpectedClassification,
        DiagnosticCode? ExpectedCode,
        string RootCause,
        string InsertionPoint,
        string ExpectedBoundaryToken);
}
