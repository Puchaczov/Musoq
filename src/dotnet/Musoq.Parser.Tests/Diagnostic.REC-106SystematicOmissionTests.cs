using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticREC106SystematicOmissionTests
{
    [TestMethod]
    public void SystematicOmissionCandidates_ShouldHonorFrozenContract()
    {
        foreach (var candidate in CandidateCases())
        {
            var seed = ParseWithDiagnostics(candidate.SeedQuery);
            Assert.IsTrue(seed.Success, $"{candidate.CaseId}: valid seed produced diagnostics. {seed.FormatDiagnostics()}");
            Assert.IsEmpty(seed.Diagnostics, $"{candidate.CaseId}: valid seed produced diagnostics.");

            var result = ParseWithDiagnostics(candidate.ResultQuery);
            if (candidate.IsValidationControl)
            {
                Assert.IsTrue(result.Success, $"{candidate.CaseId}: optional-token control failed. {result.FormatDiagnostics()}");
                Assert.IsEmpty(result.Diagnostics, $"{candidate.CaseId}: optional-token control produced diagnostics.");
                continue;
            }

            Assert.IsFalse(result.Success, $"{candidate.CaseId}: omission unexpectedly parsed successfully.");
            Assert.HasCount(1, result.Diagnostics, $"{candidate.CaseId}: {result.FormatDiagnostics()}");

            var diagnostic = result.Diagnostics.Single();
            Assert.AreEqual(candidate.ExpectedCode, diagnostic.Code, candidate.CaseId);
            Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity, candidate.CaseId);
            Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase, candidate.CaseId);
            Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind, candidate.CaseId);
            Assert.IsTrue(
                diagnostic.Span.Start >= 0 && diagnostic.Span.End <= candidate.ResultQuery.Length,
                $"{candidate.CaseId}: diagnostic span escaped the query.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference), candidate.CaseId);
            Assert.IsNotEmpty(diagnostic.SuggestedFixes, candidate.CaseId);
            Assert.IsNotNull(diagnostic.ContextSnippet, candidate.CaseId);
        }
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainFortyEightRegisteredCasesAcrossRecoveryFamilies()
    {
        var candidates = CandidateCases();

        Assert.HasCount(48, candidates);
        Assert.HasCount(40, candidates.Where(static candidate => !candidate.IsValidationControl));
        Assert.HasCount(8, candidates.Where(static candidate => candidate.IsValidationControl));
        CollectionAssert.AllItemsAreUnique(candidates.Select(static candidate => candidate.CaseId).ToArray());

        foreach (var family in Families)
        {
            Assert.HasCount(
                6,
                candidates.Where(candidate => candidate.Family == family),
                $"{family} must contain five omissions and one optional-token control.");
        }

        Assert.IsTrue(candidates.All(static candidate =>
            !string.IsNullOrWhiteSpace(candidate.InsertionPoint) &&
            !string.IsNullOrWhiteSpace(candidate.ExpectedBoundaryToken)));
    }

    [TestMethod]
    public void OptionalTokenControls_ShouldRemainValid()
    {
        foreach (var candidate in CandidateCases().Where(static candidate => candidate.IsValidationControl))
        {
            var seed = ParseWithDiagnostics(candidate.SeedQuery);
            var result = ParseWithDiagnostics(candidate.ResultQuery);

            Assert.IsTrue(seed.Success, $"{candidate.CaseId}: control seed failed. {seed.FormatDiagnostics()}");
            Assert.IsEmpty(seed.Diagnostics, $"{candidate.CaseId}: control seed produced diagnostics.");
            Assert.IsTrue(result.Success, $"{candidate.CaseId}: control result failed. {result.FormatDiagnostics()}");
            Assert.IsEmpty(result.Diagnostics, $"{candidate.CaseId}: control result produced diagnostics.");
        }
    }

    [TestMethod]
    public void OmissionMetadata_ShouldNameInsertionPointAndExpectedBoundaryForEveryCase()
    {
        foreach (var candidate in CandidateCases())
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.InsertionPoint), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.ExpectedBoundaryToken), candidate.CaseId);
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
        "DECLARATIONS"
    ];

    private static IReadOnlyList<RecoveryCase> CandidateCases() =>
    [
        Invalid(
            "REC-106-S01",
            "SELECT",
            "select 1 from #system.dual()",
            "select",
            " ",
            DiagnosticCode.MQ2025_MissingSelectKeyword,
            "statement start before projection",
            "first projection expression",
            "SELECT"),
        Invalid(
            "REC-106-S02",
            "SELECT",
            "select 1 from #system.dual()",
            "select 1",
            "select ",
            DiagnosticCode.MQ2005_InvalidSelectList,
            "projection list before FROM",
            "SELECT projection expression",
            "FROM"),
        Invalid(
            "REC-106-S03",
            "SELECT",
            "select #system.dual(1, 2) from #system.dual()",
            ", ",
            " ",
            DiagnosticCode.MQ2018_MissingOperator,
            "function argument list between the first and second argument",
            "comma separator",
            "2"),
        Invalid(
            "REC-106-S04",
            "SELECT",
            "select 1 as item from #system.dual()",
            "item",
            " ",
            DiagnosticCode.MQ2022_InvalidAlias,
            "projection alias after AS",
            "alias identifier",
            "FROM"),
        Invalid(
            "REC-106-S05",
            "SELECT",
            "select * exclude (Name) from #system.dual()",
            ") from",
            " from",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "EXCLUDE column list before FROM",
            "closing parenthesis",
            "FROM"),
        Control(
            "REC-106-S06",
            "SELECT",
            "select distinct 1 from #system.dual()",
            "distinct",
            " ",
            "projection modifier before the first expression",
            "SELECT projection"),

        Invalid(
            "REC-106-F01",
            "FROM",
            "select 1 from #system.dual()",
            "from #system.dual()",
            " ",
            DiagnosticCode.MQ2004_MissingFromClause,
            "after the SELECT projection",
            "FROM clause",
            "end of statement"),
        Invalid(
            "REC-106-F02",
            "FROM",
            "select 1 from #system.dual()",
            "#system.dual()",
            " ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "FROM source position after the FROM keyword",
            "source expression",
            "end of statement"),
        Invalid(
            "REC-106-F03",
            "FROM",
            "select 1 from #system.dual()",
            "dual",
            " ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "qualified source method after the schema separator",
            "method name",
            "left parenthesis"),
        Invalid(
            "REC-106-F04",
            "FROM",
            "select 1 from #system.dual()",
            ")",
            " ",
            DiagnosticCode.MQ2021_UnclosedFunctionCall,
            "source method argument list at end of input",
            "closing parenthesis",
            "end of statement"),
        Invalid(
            "REC-106-F05",
            "FROM",
            "select 1 from #system.dual()",
            ".",
            " ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "schema and method qualification",
            "dot separator",
            "method name"),
        Control(
            "REC-106-F06",
            "FROM",
            "select 1 from #system.dual() as source",
            "as ",
            " ",
            "source alias introducer",
            "source identifier"),

        Invalid(
            "REC-106-P01",
            "PREDICATES",
            "select 1 from #system.dual() where 1 = 1",
            "1 = 1",
            " ",
            DiagnosticCode.MQ2017_UnexpectedEndOfFile,
            "WHERE clause after its keyword",
            "predicate expression",
            "end of statement"),
        Invalid(
            "REC-106-P02",
            "PREDICATES",
            "select 1 from #system.dual() where 1 = 1",
            "where 1",
            "where ",
            DiagnosticCode.MQ2020_MissingOperand,
            "predicate before the comparison operator",
            "left operand",
            "equals sign"),
        Invalid(
            "REC-106-P03",
            "PREDICATES",
            "select 1 from #system.dual() where 1 = 1",
            "= ",
            " ",
            DiagnosticCode.MQ2018_MissingOperator,
            "predicate between adjacent scalar expressions",
            "comparison operator",
            "second literal"),
        Invalid(
            "REC-106-P04",
            "PREDICATES",
            "select 1 from #system.dual() where 1 = 1",
            "= 1",
            "= ",
            DiagnosticCode.MQ2020_MissingOperand,
            "predicate after the comparison operator",
            "right operand",
            "end of statement"),
        Invalid(
            "REC-106-P05",
            "PREDICATES",
            "select 1 from #system.dual() where 1 in (1, 2)",
            "in (",
            "in  ",
            DiagnosticCode.MQ2002_MissingToken,
            "IN predicate after its left expression",
            "opening parenthesis",
            "first value"),
        Control(
            "REC-106-P06",
            "PREDICATES",
            "select 1 from #system.dual() where 1 not in (1, 2)",
            "not ",
            " ",
            "negative IN predicate modifier",
            "IN"),

        Invalid(
            "REC-106-J01",
            "JOIN_APPLY",
            "select 1 from #rec106.left() a inner join #rec106.right() b on 1 = 1",
            "#rec106.right() b",
            " ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "right side of INNER JOIN after the join operator",
            "right source",
            "ON"),
        Invalid(
            "REC-106-J02",
            "JOIN_APPLY",
            "select 1 from #rec106.left() a inner join #rec106.right() b on 1 = 1",
            "on ",
            " ",
            DiagnosticCode.MQ2007_InvalidJoinCondition,
            "INNER JOIN after the right source alias",
            "ON condition",
            "first condition operand"),
        Invalid(
            "REC-106-J03",
            "JOIN_APPLY",
            "select 1 from #rec106.left() a inner join #rec106.right() b on 1 = 1",
            "on 1 = 1",
            "on ",
            DiagnosticCode.MQ2007_InvalidJoinCondition,
            "INNER JOIN condition after ON",
            "join condition expression",
            "end of statement"),
        Invalid(
            "REC-106-J04",
            "JOIN_APPLY",
            "select 1 from #rec106.left() a inner join #rec106.right() b on 1 = 1",
            "a inner",
            " inner",
            DiagnosticCode.MQ2035_MissingRequiredAlias,
            "first JOIN source before INNER JOIN",
            "first source alias",
            "INNER JOIN"),
        Invalid(
            "REC-106-J05",
            "JOIN_APPLY",
            "select 1 from #rec106.left() a cross apply a.Values b with ordinality",
            "b with",
            " with",
            DiagnosticCode.MQ2035_MissingRequiredAlias,
            "APPLY source before WITH ORDINALITY",
            "APPLY source alias",
            "WITH ORDINALITY"),
        Control(
            "REC-106-J06",
            "JOIN_APPLY",
            "select 1 from #rec106.left() a cross apply a.Values b with ordinality",
            "with ordinality",
            " ",
            "optional APPLY source modifier",
            "APPLY source end"),

        Invalid(
            "REC-106-G01",
            "GROUPING",
            "select 1 from #system.dual() group by 1",
            "group by 1",
            "group by ",
            DiagnosticCode.MQ2006_MissingGroupByColumn,
            "GROUP BY after its clause keyword",
            "grouping expression",
            "end of statement"),
        Invalid(
            "REC-106-G02",
            "GROUPING",
            "select 1 from #system.dual() group by 1, 2",
            "group by 1, 2",
            "group by 1, ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "GROUP BY list after its separator",
            "second grouping expression",
            "end of statement"),
        Invalid(
            "REC-106-G03",
            "GROUPING",
            "select 1 from #system.dual() group by 1 having 1 = 1",
            "having 1 = 1",
            "having ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "HAVING after its clause keyword",
            "HAVING expression",
            "end of statement"),
        Invalid(
            "REC-106-G04",
            "GROUPING",
            "select 1 from #system.dual() group by all having 1 = 1",
            "all",
            " ",
            DiagnosticCode.MQ2006_MissingGroupByColumn,
            "GROUP BY alternative before HAVING",
            "ALL or grouping expression",
            "HAVING"),
        Invalid(
            "REC-106-G05",
            "GROUPING",
            "select 1 from #system.dual() group by 1 having 1 = 1",
            "having 1",
            "having ",
            DiagnosticCode.MQ2020_MissingOperand,
            "HAVING predicate before its comparison operator",
            "HAVING left operand",
            "equals sign"),
        Control(
            "REC-106-G06",
            "GROUPING",
            "select 1 from #system.dual() group by 1 having 1 = 1",
            " having 1 = 1",
            " ",
            "optional HAVING clause",
            "end of GROUP BY clause"),

        Invalid(
            "REC-106-O01",
            "ORDERING",
            "select 1 from #system.dual() order by 1",
            "order by 1",
            "order by ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "ORDER BY after its clause keyword",
            "ordering expression",
            "end of statement"),
        Invalid(
            "REC-106-O02",
            "ORDERING",
            "select 1 from #system.dual() order by 1, 2",
            "order by 1, 2",
            "order by 1, ",
            DiagnosticCode.MQ2014_TrailingComma,
            "ORDER BY list after its separator",
            "second ordering expression",
            "end of statement"),
        Invalid(
            "REC-106-O03",
            "ORDERING",
            "select 1 from #system.dual() order by 1 nulls first",
            "first",
            " ",
            DiagnosticCode.MQ2009_InvalidOrderByExpression,
            "NULLS ordering modifier after NULLS",
            "FIRST or LAST selector",
            "end of statement"),
        Invalid(
            "REC-106-O04",
            "ORDERING",
            "select 1 from #system.dual() order by 1 skip 2",
            "2",
            " ",
            DiagnosticCode.MQ2038_InvalidSliceCount,
            "SKIP clause after its keyword",
            "non-negative SKIP count",
            "end of statement"),
        Invalid(
            "REC-106-O05",
            "ORDERING",
            "select 1 from #system.dual() order by 1 take 2",
            "2",
            " ",
            DiagnosticCode.MQ2038_InvalidSliceCount,
            "TAKE clause after its keyword",
            "non-negative TAKE count",
            "end of statement"),
        Control(
            "REC-106-O06",
            "ORDERING",
            "select 1 from #system.dual() order by 1 desc",
            "desc",
            " ",
            "optional ordering direction",
            "end of ordering expression"),

        Invalid(
            "REC-106-C01",
            "CTE",
            "with states as (select 1 from #system.dual()) select 1 from states",
            "states as",
            " as",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "CTE declaration after WITH",
            "CTE name",
            "AS"),
        Invalid(
            "REC-106-C02",
            "CTE",
            "with states as (select 1 from #system.dual()) select 1 from states",
            " as (",
            " (",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "CTE name before its query declaration",
            "AS keyword",
            "opening parenthesis"),
        Invalid(
            "REC-106-C03",
            "CTE",
            "with states as (select 1 from #system.dual()) select 1 from states",
            "as (",
            "as ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "CTE declaration before its query body",
            "opening parenthesis",
            "SELECT"),
        Invalid(
            "REC-106-C04",
            "CTE",
            "with states as (select 1 from #system.dual()) select 1 from states",
            ") select",
            " select",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "CTE query body before the outer query",
            "closing parenthesis",
            "SELECT"),
        Invalid(
            "REC-106-C05",
            "CTE",
            "with states as (select 1 from #system.dual()) select 1 from states",
            " select 1 from states",
            " ",
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "CTE declaration after its closing parenthesis",
            "outer query",
            "end of statement"),
        Control(
            "REC-106-C06",
            "CTE",
            "with states (Value) as (select 1 from #system.dual()) select Value from states",
            " (Value)",
            " ",
            "optional CTE output column list",
            "AS"),

        Invalid(
            "REC-106-D01",
            "DECLARATIONS",
            "table Jobs { Id: int }",
            "Jobs",
            " ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "TABLE declaration after its keyword",
            "table name",
            "opening brace"),
        Invalid(
            "REC-106-D02",
            "DECLARATIONS",
            "table Jobs { Id: int }",
            "{",
            " ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "TABLE name before its column body",
            "opening brace",
            "first column"),
        Invalid(
            "REC-106-D03",
            "DECLARATIONS",
            "table Jobs { Id: int }",
            "Id: ",
            ": ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "TABLE body before the first column name",
            "column name",
            "colon"),
        Invalid(
            "REC-106-D04",
            "DECLARATIONS",
            "table Jobs { Id: int }",
            ":",
            " ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "TABLE column name before its type separator",
            "colon separator",
            "type name"),
        Invalid(
            "REC-106-D05",
            "DECLARATIONS",
            "table Jobs { Id: int }",
            "int",
            " ",
            DiagnosticCode.MQ2001_UnexpectedToken,
            "TABLE column separator before its type name",
            "column type",
            "closing brace"),
        Control(
            "REC-106-D06",
            "DECLARATIONS",
            "table Jobs { Id: int, Name: string, }",
            ", }",
            " }",
            "optional trailing TABLE column separator",
            "closing brace")
    ];

    private static RecoveryCase Invalid(
        string caseId,
        string family,
        string seed,
        string before,
        string after,
        DiagnosticCode expectedCode,
        string insertionPoint,
        string expectedBoundaryToken,
        string rootCause) =>
        Create(caseId, family, seed, before, after, expectedCode, false, insertionPoint, expectedBoundaryToken, rootCause);

    private static RecoveryCase Control(
        string caseId,
        string family,
        string seed,
        string before,
        string after,
        string insertionPoint,
        string expectedBoundaryToken) =>
        Create(caseId, family, seed, before, after, null, true, insertionPoint, expectedBoundaryToken, "optional-token-removal");

    private static RecoveryCase Create(
        string caseId,
        string family,
        string seed,
        string before,
        string after,
        DiagnosticCode? expectedCode,
        bool isValidationControl,
        string insertionPoint,
        string expectedBoundaryToken,
        string rootCause)
    {
        const string markerPrefix = " /* ";
        var marker = $"{markerPrefix}{caseId} */";
        var effectiveAfter = string.IsNullOrWhiteSpace(after) ? "/* omitted */" : after;
        var seedQuery = seed + marker;
        var resultQuery = ReplaceOnce(seed, before, effectiveAfter) + marker;
        return new RecoveryCase(
            caseId,
            family,
            seedQuery,
            resultQuery,
            expectedCode,
            isValidationControl,
            insertionPoint,
            expectedBoundaryToken,
            rootCause);
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
        DiagnosticCode? ExpectedCode,
        bool IsValidationControl,
        string InsertionPoint,
        string ExpectedBoundaryToken,
        string RootCause);
}
