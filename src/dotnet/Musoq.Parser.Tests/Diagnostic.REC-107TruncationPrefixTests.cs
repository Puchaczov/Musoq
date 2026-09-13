using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticREC107TruncationPrefixTests
{
    [TestMethod]
    public void TruncationCandidatesV2_ShouldHonorFrozenContract()
    {
        foreach (var candidate in CandidateCases())
        {
            var seed = ParseWithDiagnostics(candidate.SeedQuery);
            Assert.IsTrue(seed.Success, $"{candidate.CaseId}: valid seed produced diagnostics. {seed.FormatDiagnostics()}");
            Assert.IsEmpty(seed.Diagnostics, $"{candidate.CaseId}: valid seed produced diagnostics.");

            var result = ParseWithDiagnostics(candidate.ResultQuery);
            if (candidate.ExpectedCode is null)
            {
                Assert.IsTrue(result.Success, $"{candidate.CaseId}: complete prefix control failed. {result.FormatDiagnostics()}");
                Assert.IsEmpty(result.Diagnostics, $"{candidate.CaseId}: complete prefix control produced diagnostics.");
                continue;
            }

            Assert.IsFalse(result.Success, $"{candidate.CaseId}: incomplete prefix unexpectedly parsed successfully.");
            Assert.HasCount(1, result.Diagnostics, $"{candidate.CaseId}: {result.FormatDiagnostics()}");
            var diagnostic = result.Diagnostics.Single();
            Assert.AreEqual(candidate.ExpectedCode.Value, diagnostic.Code, candidate.CaseId);
            Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity, candidate.CaseId);
            Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase, candidate.CaseId);
            Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind, candidate.CaseId);
            Assert.IsTrue(diagnostic.Span.Start >= 0 && diagnostic.Span.End <= candidate.ResultQuery.Length, $"{candidate.CaseId}: diagnostic span escaped the query.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference), candidate.CaseId);
            Assert.IsNotEmpty(diagnostic.SuggestedFixes, candidate.CaseId);
            Assert.IsNotNull(diagnostic.ContextSnippet, candidate.CaseId);
            Assert.IsFalse(diagnostic.Phase != DiagnosticPhase.Parse, $"{candidate.CaseId}: dependent diagnostic replaced parse root.");
        }
    }

    [TestMethod]
    public void PrefixMatrix_ShouldContainFortyEightCasesAcrossEightFamilies()
    {
        var candidates = CandidateCases();
        Assert.HasCount(48, candidates);
        Assert.HasCount(40, candidates.Where(static candidate => candidate.ExpectedCode is not null));
        Assert.HasCount(8, candidates.Where(static candidate => candidate.ExpectedCode is null));
        CollectionAssert.AllItemsAreUnique(candidates.Select(static candidate => candidate.CaseId).ToArray());
        foreach (var family in Families)
            Assert.HasCount(6, candidates.Where(candidate => candidate.Family == family), $"{family} must contain five incomplete prefixes and one complete control.");
    }

    [TestMethod]
    public void PrefixInventory_ShouldDeclareInsertionAndBoundaryForEveryCase()
    {
        foreach (var candidate in CandidateCases())
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.InsertionPoint), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.ExpectedBoundaryToken), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.RootCause), candidate.CaseId);
            Assert.IsTrue(candidate.SeedQuery.Contains($"/* {candidate.CaseId} */", StringComparison.Ordinal), candidate.CaseId);
        }
    }

    private static readonly string[] Families =
    [
        "SELECT",
        "FROM",
        "PREDICATES",
        "JOIN_APPLY",
        "GROUPING",
        "ORDERING",
        "CTE",
        "DECLARATIONS",
    ];

    private static IReadOnlyList<PrefixCase> CandidateCases() =>
    [
        Invalid(
            "REC-107-S01", "SELECT",
            "select 1 from #system.dual()", " from #system.dual()", "/* omitted */",
            DiagnosticCode.MQ2004_MissingFromClause, "select-prefix-before-from", "after SELECT projection before required FROM", "FROM clause"),

        Invalid(
            "REC-107-S02", "SELECT",
            "select 1 from #system.dual()", "#system.dual()", "/* omitted */",
            DiagnosticCode.MQ2001_UnexpectedToken, "from-source-prefix-at-eof", "after FROM keyword", "source expression"),

        Invalid(
            "REC-107-S03", "SELECT",
            "select 1 from #system.dual()", ")", "/* omitted */",
            DiagnosticCode.MQ2021_UnclosedFunctionCall, "source-function-prefix-open-delimiter", "source method call", "closing parenthesis"),

        Invalid(
            "REC-107-S04", "SELECT",
            "select 1 as item from #system.dual()", "item", "/* omitted */",
            DiagnosticCode.MQ2022_InvalidAlias, "projection-alias-prefix-at-eof", "after AS in projection", "alias identifier"),

        Invalid(
            "REC-107-S05", "SELECT",
            "select 1 from #system.dual() order by 1", "order by 1", "order by /* omitted */",
            DiagnosticCode.MQ2001_UnexpectedToken, "order-prefix-at-eof", "after ORDER BY keyword", "ordering expression"),

        Control(
            "REC-107-S06", "SELECT",
            "select distinct 1 from #system.dual()", "distinct", "/* omitted */",
            "optional-select-modifier", "optional DISTINCT modifier", "first projection expression"),

        Invalid(
            "REC-107-F01", "FROM",
            "select 1 from #system.dual()", " from #system.dual()", "/* omitted */",
            DiagnosticCode.MQ2004_MissingFromClause, "from-clause-prefix-before-source", "after SELECT projection", "FROM clause"),

        Invalid(
            "REC-107-F02", "FROM",
            "select 1 from #system.dual()", "#system.dual()", "/* omitted */",
            DiagnosticCode.MQ2001_UnexpectedToken, "from-source-prefix-at-eof", "after FROM keyword", "source expression"),

        Invalid(
            "REC-107-F03", "FROM",
            "select 1 from #system.dual()", "dual", "/* omitted */",
            DiagnosticCode.MQ2001_UnexpectedToken, "qualified-source-prefix-before-method", "after schema separator", "method name"),

        Invalid(
            "REC-107-F04", "FROM",
            "select 1 from #system.dual()", ")", "/* omitted */",
            DiagnosticCode.MQ2021_UnclosedFunctionCall, "source-function-prefix-open-delimiter", "source method call", "closing parenthesis"),

        Invalid(
            "REC-107-F05", "FROM",
            "select 1 from #system.dual() as source", "source", "/* omitted */",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "explicit-source-alias-at-eof", "after source AS", "alias identifier"),

        Control(
            "REC-107-F06", "FROM",
            "select 1 from #system.dual() as source", "as ", "/* omitted */",
            "optional-source-alias-introducer", "optional AS introducer", "source identifier"),

        Invalid(
            "REC-107-P01", "PREDICATES",
            "select 1 from #system.dual() where 1 = 1", " 1 = 1", "/* omitted */",
            DiagnosticCode.MQ2017_UnexpectedEndOfFile, "where-prefix-at-eof", "after WHERE keyword", "predicate expression"),

        Invalid(
            "REC-107-P02-v2", "PREDICATES",
            "select 1 from #system.dual() where 1 = 1", "= 1", "= ",
            DiagnosticCode.MQ2020_MissingOperand, "comparison-right-prefix-at-eof", "after comparison operator", "right operand"),

        Invalid(
            "REC-107-P03", "PREDICATES",
            "select 1 from #system.dual() where 1 = 1 and 2 = 2", " 2 = 2", "/* omitted */",
            DiagnosticCode.MQ2020_MissingOperand, "compound-and-prefix-at-eof", "after AND", "right operand"),

        Invalid(
            "REC-107-P04", "PREDICATES",
            "select 1 from #system.dual() where 1 in (1, 2)", " (1, 2)", "/* omitted */",
            DiagnosticCode.MQ2002_MissingToken, "in-prefix-at-eof", "after IN", "opening parenthesis"),

        Invalid(
            "REC-107-P05", "PREDICATES",
            "select 1 from #system.dual() where 1 between 2 and 3", " and 3", "/* omitted */",
            DiagnosticCode.MQ2002_MissingToken, "between-prefix-before-and", "after BETWEEN lower bound", "AND separator"),

        Control(
            "REC-107-P06", "PREDICATES",
            "select 1 from #system.dual() where 1 not in (1, 2)", "not ", "/* omitted */",
            "optional-negative-predicate-modifier", "optional NOT modifier", "IN predicate"),

        Invalid(
            "REC-107-J01", "JOIN_APPLY",
            "select 1 from #rec107.left() a inner join #rec107.right() b on 1 = 1", " #rec107.right() b", "/* omitted */",
            DiagnosticCode.MQ2001_UnexpectedToken, "join-prefix-before-right-source", "after INNER JOIN", "right source"),

        Invalid(
            "REC-107-J02", "JOIN_APPLY",
            "select 1 from #rec107.left() a inner join #rec107.right() b on 1 = 1", " on 1 = 1", "/* omitted */",
            DiagnosticCode.MQ2007_InvalidJoinCondition, "join-prefix-before-on", "after right source alias", "ON condition"),

        Invalid(
            "REC-107-J03", "JOIN_APPLY",
            "select 1 from #rec107.left() a inner join #rec107.right() b on 1 = 1", " 1 = 1", "/* omitted */",
            DiagnosticCode.MQ2007_InvalidJoinCondition, "join-prefix-at-on-eof", "after ON", "join condition expression"),

        Invalid(
            "REC-107-J04", "JOIN_APPLY",
            "select 1 from #rec107.left() a inner join #rec107.right() b on 1 = 1", "a inner", "/* omitted */",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "join-source-alias-prefix", "before INNER JOIN", "first source alias"),

        Invalid(
            "REC-107-J05", "JOIN_APPLY",
            "select 1 from #rec107.left() a cross apply a.Values b with ordinality", " b with ordinality", "/* omitted */",
            DiagnosticCode.MQ2035_MissingRequiredAlias, "apply-source-alias-prefix-at-eof", "after APPLY source", "APPLY source alias"),

        Control(
            "REC-107-J06", "JOIN_APPLY",
            "select 1 from #rec107.left() a cross apply a.Values b with ordinality", " with ordinality", "/* omitted */",
            "optional-apply-modifier", "optional WITH ORDINALITY modifier", "APPLY source end"),

        Invalid(
            "REC-107-G01", "GROUPING",
            "select 1 from #system.dual() group by 1", "group by 1", "group by /* omitted */",
            DiagnosticCode.MQ2006_MissingGroupByColumn, "group-by-prefix-at-eof", "after GROUP BY", "grouping expression"),

        Invalid(
            "REC-107-G02", "GROUPING",
            "select 1 from #system.dual() group by 1, 2", "group by 1, 2", "group by 1, /* omitted */",
            DiagnosticCode.MQ2001_UnexpectedToken, "group-by-trailing-prefix", "after grouping separator", "second grouping expression"),

        Invalid(
            "REC-107-G03", "GROUPING",
            "select 1 from #system.dual() group by 1 having 1 = 1", "having 1 = 1", "having /* omitted */",
            DiagnosticCode.MQ2001_UnexpectedToken, "having-prefix-at-eof", "after HAVING", "HAVING expression"),

        Invalid(
            "REC-107-G04", "GROUPING",
            "select 1 from #system.dual() group by 1 having 1 = 1", "= 1", "= /* omitted */",
            DiagnosticCode.MQ2020_MissingOperand, "having-comparison-prefix-at-eof", "after HAVING equals", "right operand"),

        Invalid(
            "REC-107-G05", "GROUPING",
            "select 1 from #system.dual() group by all having 1 = 1", "all", "/* omitted */",
            DiagnosticCode.MQ2006_MissingGroupByColumn, "group-by-all-prefix", "before HAVING", "ALL or grouping expression"),

        Control(
            "REC-107-G06", "GROUPING",
            "select 1 from #system.dual() group by 1 having 1 = 1", " having 1 = 1", "/* omitted */",
            "optional-having-clause", "optional HAVING clause", "end of GROUP BY clause"),

        Invalid(
            "REC-107-O01", "ORDERING",
            "select 1 from #system.dual() order by 1", "order by 1", "order by /* omitted */",
            DiagnosticCode.MQ2001_UnexpectedToken, "order-by-prefix-at-eof", "after ORDER BY", "ordering expression"),

        Invalid(
            "REC-107-O02", "ORDERING",
            "select 1 from #system.dual() order by 1, 2", "order by 1, 2", "order by 1, /* omitted */",
            DiagnosticCode.MQ2014_TrailingComma, "order-by-trailing-prefix", "after ordering separator", "second ordering expression"),

        Invalid(
            "REC-107-O03", "ORDERING",
            "select 1 from #system.dual() order by 1 nulls first", "first", "/* omitted */",
            DiagnosticCode.MQ2009_InvalidOrderByExpression, "nulls-compound-keyword-prefix", "after NULLS", "FIRST or LAST selector"),

        Invalid(
            "REC-107-O04-v2", "ORDERING",
            "select 1 from #system.dual() order by 1 skip 2", " 2", " /* omitted */",
            DiagnosticCode.MQ2038_InvalidSliceCount, "skip-prefix-at-eof", "after SKIP", "non-negative SKIP count"),

        Invalid(
            "REC-107-O05-v2", "ORDERING",
            "select 1 from #system.dual() order by 1 take 2", " 2", " /* omitted */",
            DiagnosticCode.MQ2038_InvalidSliceCount, "take-prefix-at-eof", "after TAKE", "non-negative TAKE count"),

        Control(
            "REC-107-O06", "ORDERING",
            "select 1 from #system.dual() order by 1 desc", "desc", "/* omitted */",
            "optional-order-direction", "optional DESC modifier", "end of ordering expression"),

        Invalid(
            "REC-107-C01", "CTE",
            "with states as (select 1 from #system.dual()) select 1 from states", "states as", " as",
            DiagnosticCode.MQ2001_UnexpectedToken, "cte-name-prefix", "after WITH", "CTE name"),

        Invalid(
            "REC-107-C02", "CTE",
            "with states as (select 1 from #system.dual()) select 1 from states", " as (", " (",
            DiagnosticCode.MQ2001_UnexpectedToken, "cte-as-prefix", "after CTE name", "AS and opening parenthesis"),

        Invalid(
            "REC-107-C03", "CTE",
            "with states as (select 1 from #system.dual()) select 1 from states", "as (", "as ",
            DiagnosticCode.MQ2001_UnexpectedToken, "cte-body-prefix", "after CTE AS", "opening parenthesis"),

        Invalid(
            "REC-107-C04", "CTE",
            "with states as (select 1 from #system.dual()) select 1 from states", ") select", " select",
            DiagnosticCode.MQ2001_UnexpectedToken, "cte-inner-prefix-before-close", "after CTE inner query", "closing parenthesis"),

        Invalid(
            "REC-107-C05", "CTE",
            "with states as (select 1 from #system.dual()) select 1 from states", " select 1 from states", "/* omitted */",
            DiagnosticCode.MQ2030_UnsupportedSyntax, "cte-prefix-without-outer-query", "after CTE declaration", "outer query"),

        Control(
            "REC-107-C06", "CTE",
            "with states (Value) as (select 1 from #system.dual()) select Value from states", " (Value)", "/* omitted */",
            "optional-cte-column-list", "optional CTE column list", "AS clause"),

        Invalid(
            "REC-107-D01", "DECLARATIONS",
            "table Jobs { Id: int }", "Jobs", "/* omitted */",
            DiagnosticCode.MQ2001_UnexpectedToken, "table-name-prefix-at-eof", "after TABLE keyword", "opening brace"),

        Invalid(
            "REC-107-D02", "DECLARATIONS",
            "table Jobs { Id: int }", "{", "/* omitted */",
            DiagnosticCode.MQ2001_UnexpectedToken, "table-body-prefix-before-open", "after TABLE name", "first column"),

        Invalid(
            "REC-107-D03", "DECLARATIONS",
            "table Jobs { Id: int }", "Id: ", " : ",
            DiagnosticCode.MQ2001_UnexpectedToken, "table-column-prefix-before-name", "before column type separator", "column name"),

        Invalid(
            "REC-107-D04", "DECLARATIONS",
            "table Jobs { Id: int }", ":", "/* omitted */",
            DiagnosticCode.MQ2001_UnexpectedToken, "table-column-prefix-before-colon", "after column name", "type name"),

        Invalid(
            "REC-107-D05", "DECLARATIONS",
            "table Jobs { Id: int }", "int", "/* omitted */",
            DiagnosticCode.MQ2001_UnexpectedToken, "table-column-prefix-before-type", "after column colon", "closing brace"),

        Control(
            "REC-107-D06", "DECLARATIONS",
            "table Jobs { Id: int, Name: string, }", ", }", " }",
            "optional-declaration-trailing-comma", "optional trailing column separator", "closing brace"),

    ];

    private static PrefixCase Invalid(string caseId, string family, string seed, string before, string after, DiagnosticCode expectedCode, string rootCause, string insertionPoint, string expectedBoundaryToken) =>
        Create(caseId, family, seed, before, after, expectedCode, rootCause, insertionPoint, expectedBoundaryToken);

    private static PrefixCase Control(string caseId, string family, string seed, string before, string after, string rootCause, string insertionPoint, string expectedBoundaryToken) =>
        Create(caseId, family, seed, before, after, null, rootCause, insertionPoint, expectedBoundaryToken);

    private static PrefixCase Create(string caseId, string family, string seed, string before, string after, DiagnosticCode? expectedCode, string rootCause, string insertionPoint, string expectedBoundaryToken)
    {
        var marker = $" /* {caseId} */";
        var seedQuery = seed + marker;
        var resultQuery = ReplaceOnce(seed, before, after) + marker;
        return new PrefixCase(caseId, family, seedQuery, resultQuery, expectedCode, rootCause, insertionPoint, expectedBoundaryToken);
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

    private sealed record PrefixCase(
        string CaseId,
        string Family,
        string SeedQuery,
        string ResultQuery,
        DiagnosticCode? ExpectedCode,
        string RootCause,
        string InsertionPoint,
        string ExpectedBoundaryToken);
}
