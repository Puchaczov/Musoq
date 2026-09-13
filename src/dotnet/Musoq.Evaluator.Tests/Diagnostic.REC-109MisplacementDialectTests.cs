using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC109MisplacementDialectTests : BasicEntityTestBase
{
    [TestMethod]
    public void AsOfRightGuidance_ShouldBeSpecificAndContextual()
    {
        foreach (var query in new[]
        {
            "select 1 from #A.entities() a asof right join #B.entities() b on a.Id >= b.Id",
            "select 1 from #A.entities() a asof right outer join #B.entities() b on a.Id >= b.Id"
        })
        {
            var result = ParseWithDiagnostics(query);
            Assert.IsFalse(result.Success, result.FormatDiagnostics());
            Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
            var diagnostic = result.Diagnostics.Single();
            Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, diagnostic.Code);
            Assert.AreEqual(
                "Cannot compose statement, Identifier is not expected here. Musoq supports ASOF JOIN and ASOF LEFT JOIN, but not ASOF RIGHT JOIN.",
                diagnostic.Message);
            Assert.AreEqual("Musoq supports ASOF JOIN and ASOF LEFT JOIN, but not ASOF RIGHT JOIN.", diagnostic.Explanation);
            Assert.AreEqual("Core Spec - ASOF JOIN", diagnostic.DocsReference);
            CollectionAssert.AreEqual(
                new[]
                {
                    "Use ASOF LEFT JOIN when left-preserving semantics match the intended query.",
                    "Otherwise reverse the sources and inequality explicitly, then verify the result.",
                    "Check for missing keywords, commas, or parentheses near this location.",
                    "Verify the query follows Musoq SQL syntax."
                },
                diagnostic.SuggestedFixes.Select(static fix => fix.Title).ToArray());
        }

        foreach (var query in new[]
        {
            "select 1 from #system.dual() d order by 1 asc",
            "select 'asof right join' from #system.dual() d",
            "select 1 from #system.dual() d /* asof right join */"
        })
        {
            var result = ParseWithDiagnostics(query);
            Assert.IsTrue(result.Success, result.FormatDiagnostics());
            Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
        }
    }

    [TestMethod]
    public void MisplacementAndDialectCandidates_ShouldHonorFrozenContract()
    {
        foreach (var candidate in CandidateCases())
            ValidateCandidate(candidate);
    }

    private void ValidateCandidate(RecoveryCase candidate)
    {
        var seed = ParseWithDiagnostics(candidate.SeedQuery);
        Assert.IsTrue(seed.Success, $"{candidate.CaseId}: valid seed produced diagnostics. {seed.FormatDiagnostics()}");
        Assert.IsEmpty(seed.Diagnostics, $"{candidate.CaseId}: valid seed produced diagnostics.");

        if (candidate.ExecutionMode == "bind")
        {
            var exception = Assert.Throws<MusoqQueryException>(() =>
                CreateAndRunVirtualMachine(candidate.ResultQuery, AsOfSources()));

            AssertErrorEnvelope(exception, candidate.ExpectedCode!.Value, DiagnosticPhase.Bind);
            AssertHasGuidance(exception);
            Assert.IsTrue(
                (exception.PrimaryEnvelope.Message + " " + exception.PrimaryEnvelope.Explanation)
                .Contains(candidate.Guidance, StringComparison.OrdinalIgnoreCase),
                $"{candidate.CaseId}: missing guidance '{candidate.Guidance}'.");
            return;
        }

        var result = ParseWithDiagnostics(candidate.ResultQuery);
        if (candidate.ExpectedCode is null)
        {
            Assert.IsTrue(result.Success, $"{candidate.CaseId}: valid candidate failed. {result.FormatDiagnostics()}");
            Assert.IsEmpty(result.Diagnostics, $"{candidate.CaseId}: valid candidate produced diagnostics.");
            return;
        }

        Assert.IsFalse(result.Success, $"{candidate.CaseId}: malformed candidate parsed successfully.");
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
        Assert.IsTrue(
            (diagnostic.Message + " " + diagnostic.Explanation)
            .Contains(candidate.Guidance, StringComparison.OrdinalIgnoreCase),
            $"{candidate.CaseId}: missing guidance '{candidate.Guidance}'. Actual message: {diagnostic.Message}; explanation: {diagnostic.Explanation}; docs: {diagnostic.DocsReference}; fixes: {string.Join(" | ", diagnostic.SuggestedFixes.Select(fix => fix.Title))}.");
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainFortyEightCasesAcrossEightFamilies()
    {
        var candidates = CandidateCases();

        Assert.HasCount(48, candidates);
        Assert.HasCount(29, candidates.Where(static candidate => candidate.ExpectedCode is not null));
        Assert.HasCount(19, candidates.Where(static candidate => candidate.ExpectedCode is null));
        CollectionAssert.AllItemsAreUnique(candidates.Select(static candidate => candidate.CaseId).ToArray());

        foreach (var family in Families)
            Assert.HasCount(6, candidates.Where(candidate => candidate.Family == family), $"{family} must contain six registered cases.");
    }

    [TestMethod]
    public void CandidateMetadata_ShouldDeclareBoundaryAndClassificationForEveryCase()
    {
        foreach (var candidate in CandidateCases())
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.RootCause), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.InsertionPoint), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.ExpectedBoundaryToken), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.Guidance), candidate.CaseId);
            Assert.IsTrue(candidate.SeedQuery.Contains($"/* {candidate.CaseId} */", StringComparison.Ordinal), candidate.CaseId);
            if (candidate.ExpectedCode is null)
                Assert.AreEqual("valid_ordinary", candidate.ExpectedClassification, candidate.CaseId);
        }
    }

    private static readonly string[] Families =
    [
        "MISPLACED_CLAUSES",
        "MISSING_ALIASES",
        "FOREIGN_PAGINATION",
        "FOREIGN_DECLARATIONS_CASTS",
        "ASOF_BOUNDARIES",
        "RESERVED_IDENTIFIERS",
        "NATIVE_REPAIRS",
        "CONTEXTUAL_IDENTIFIERS"
    ];

    private static IReadOnlyList<RecoveryCase> CandidateCases() =>
    [
        Invalid("REC-109-M01", "MISPLACED_CLAUSES", "select 1 from a.first() a cross apply a.Column item where 1 = 1", "item where", "where", DiagnosticCode.MQ2035_MissingRequiredAlias, "parse", "clause-before-apply-alias", "CROSS APPLY source before WHERE", "WHERE", "WHERE"),
        Invalid("REC-109-M02", "MISPLACED_CLAUSES", "select 1 from a.first() a cross apply a.Column item group by 1", "item group", "group", DiagnosticCode.MQ2035_MissingRequiredAlias, "parse", "clause-before-apply-alias", "CROSS APPLY source before GROUP BY", "GROUP BY", "GROUP BY"),
        Invalid("REC-109-M03-v2", "MISPLACED_CLAUSES", "select 1 from a.first() a cross apply a.Column item group by 1 having 1 = 1", "item group", "group", DiagnosticCode.MQ2035_MissingRequiredAlias, "parse", "clause-before-apply-alias", "CROSS APPLY source before GROUP BY", "GROUP BY", "GROUP BY"),
        Invalid("REC-109-M04", "MISPLACED_CLAUSES", "select 1 from a.first() a cross apply a.Column item order by 1", "item order", "order", DiagnosticCode.MQ2035_MissingRequiredAlias, "parse", "clause-before-apply-alias", "CROSS APPLY source before ORDER BY", "ORDER BY", "ORDER BY"),
        Invalid("REC-109-M05", "MISPLACED_CLAUSES", "select 1 from a.first() a cross apply a.Column item skip 1", "item skip", "skip", DiagnosticCode.MQ2035_MissingRequiredAlias, "parse", "clause-before-apply-alias", "CROSS APPLY source before SKIP", "SKIP", "SKIP"),
        Invalid("REC-109-M06", "MISPLACED_CLAUSES", "select 1 from a.first() a cross apply a.Column item take 1", "item take", "take", DiagnosticCode.MQ2035_MissingRequiredAlias, "parse", "clause-before-apply-alias", "CROSS APPLY source before TAKE", "TAKE", "TAKE"),

        Invalid("REC-109-A01", "MISSING_ALIASES", "select 1 from a.first() first inner join b.second() b on 1 = 1", "b.second() b on", "b.second() on", DiagnosticCode.MQ2035_MissingRequiredAlias, "parse", "missing-right-join-alias", "INNER JOIN right source", "ON", "INNER JOIN"),
        Invalid("REC-109-A02", "MISSING_ALIASES", "select 1 from a.first() first asof left join b.second() b on a.Id >= b.Id", "b.second() b on", "b.second() on", DiagnosticCode.MQ2035_MissingRequiredAlias, "parse", "missing-right-asof-alias", "ASOF LEFT JOIN right source", "ON", "ASOF LEFT JOIN"),
        Invalid("REC-109-A03", "MISSING_ALIASES", "select 1 from a.first() first cross join b.second() b take 1", "b.second() b take", "b.second() take", DiagnosticCode.MQ2035_MissingRequiredAlias, "parse", "missing-right-cross-alias", "CROSS JOIN right source", "TAKE", "CROSS JOIN"),
        Invalid("REC-109-A04", "MISSING_ALIASES", "select * from (select 1 from source) derived", ") derived", ")", DiagnosticCode.MQ2035_MissingRequiredAlias, "parse", "missing-derived-alias", "derived table after closing parenthesis", "closing parenthesis", "derived table"),
        Invalid("REC-109-A05", "MISSING_ALIASES", "select * from values { { Name: 'A' } } rows", "} rows", "}", DiagnosticCode.MQ2035_MissingRequiredAlias, "parse", "missing-values-alias", "VALUES source after closing brace", "closing brace", "VALUES"),
        Invalid("REC-109-A06", "MISSING_ALIASES", "select 1 from a.first() first cross apply first.Column item take 1", "item take", "as take", DiagnosticCode.MQ2035_MissingRequiredAlias, "parse", "missing-alias-after-as", "CROSS APPLY alias after AS", "AS", "alias"),

        Invalid("REC-109-F01", "FOREIGN_PAGINATION", "select 1 from #system.dual() d", "dual() d", "dual() d limit 5", DiagnosticCode.MQ2001_UnexpectedToken, "parse", "foreign-limit-clause", "LIMIT pagination keyword", "LIMIT", "TAKE"),
        Invalid("REC-109-F02", "FOREIGN_PAGINATION", "select 1 from #system.dual() d", "dual() d", "dual() d offset 2", DiagnosticCode.MQ2001_UnexpectedToken, "parse", "foreign-offset-clause", "OFFSET pagination keyword", "OFFSET", "SKIP"),
        Invalid("REC-109-F03", "FOREIGN_PAGINATION", "select 1 from #system.dual() d order by 1", "order by 1", "order by 1 offset 2 rows fetch next 5 rows only", DiagnosticCode.MQ2009_InvalidOrderByExpression, "parse", "foreign-offset-fetch-clause", "SQL Server OFFSET/FETCH pagination", "OFFSET", "SKIP"),
        Invalid("REC-109-F04", "FOREIGN_PAGINATION", "select 1 from #system.dual() d", "select 1", "select top 5 1", DiagnosticCode.MQ2030_UnsupportedSyntax, "parse", "foreign-top-prefix", "TOP pagination prefix", "TOP", "TAKE"),
        Invalid("REC-109-F05", "FOREIGN_PAGINATION", "select 1 from #system.dual() d", "select 1", "select first 5 1", DiagnosticCode.MQ2030_UnsupportedSyntax, "parse", "foreign-first-prefix", "FIRST pagination prefix", "FIRST", "TAKE"),
        Invalid("REC-109-F06", "FOREIGN_PAGINATION", "select 1 from #system.dual() d where 1 = 1", "1 = 1", "1 ilike '1'", DiagnosticCode.MQ2001_UnexpectedToken, "parse", "foreign-ilike-predicate", "PostgreSQL ILIKE predicate", "ILIKE", "LIKE"),

        Invalid("REC-109-D01", "FOREIGN_DECLARATIONS_CASTS", "params(author: string = 'x') select 1 from #system.dual()", "params(author: string = 'x')", "param([string]$author)", DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax, "parse", "powershell-parameter-declaration", "PowerShell parameter declaration", "parameter block", "param("),
        Invalid("REC-109-D02", "FOREIGN_DECLARATIONS_CASTS", "params(author: string = 'x') select 1 from #system.dual()", "params(author: string = 'x')", "def query(author: str = 'x')", DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax, "parse", "python-query-declaration", "Python query declaration", "query declaration", "param("),
        Invalid("REC-109-D03", "FOREIGN_DECLARATIONS_CASTS", "params(author: string = 'x') select 1 from #system.dual()", "params(author: string = 'x')", "declare author string;", DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax, "parse", "sql-declare-declaration", "SQL DECLARE declaration", "declaration", "param("),
        Invalid("REC-109-D04-v2", "FOREIGN_DECLARATIONS_CASTS", "params(author: string = 'x') select 1 from #system.dual()", "params(author: string = 'x')", "param(string author)", DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration, "parse", "csharp-parameter-declaration", "C#-style parameter declaration", "parameter declaration", "param("),
        Invalid("REC-109-D05", "FOREIGN_DECLARATIONS_CASTS", "select 1 from #system.dual()", "select 1", "select cast(1 as int)", DiagnosticCode.MQ2021_UnclosedFunctionCall, "parse", "function-style-cast", "function-style CAST expression", "CAST", "postfix"),
        Invalid("REC-109-D06", "FOREIGN_DECLARATIONS_CASTS", "select Population from #A.entities()", "Population", "Population::text", DiagnosticCode.MQ3090_UnsupportedCastTarget, "bind", "postgres-postfix-cast", "PostgreSQL postfix cast target", "cast target", "cast"),

        Invalid("REC-109-S01", "ASOF_BOUNDARIES", "select 1 from #A.entities() a asof left join #B.entities() b on a.Id >= b.Id", "asof left", "asof right", DiagnosticCode.MQ2001_UnexpectedToken, "parse", "unsupported-asof-right", "ASOF RIGHT JOIN direction", "ASOF RIGHT", "ASOF LEFT"),
        Invalid("REC-109-S02", "ASOF_BOUNDARIES", "select 1 from #A.entities() a inner join #B.entities() b on a.Id = b.Id", "a.Id = b.Id", "a.Id = b.Id tie break by b.Id", DiagnosticCode.MQ2039_TieBreakRequiresAsOfJoin, "parse", "tie-break-outside-asof", "TIE BREAK outside ASOF JOIN", "TIE BREAK", "ASOF JOIN"),
        Invalid("REC-109-S03", "ASOF_BOUNDARIES", "select a.Name from #A.entities() a asof join #B.entities() b on a.Population >= b.Population tie break by b.Id", "tie break by b.Id", "tie break by a.Name", DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides, "bind", "asof-tie-break-left-reference", "ASOF TIE BREAK left-side expression", "TIE BREAK", "right-side"),
        Invalid("REC-109-S04", "ASOF_BOUNDARIES", "select a.Name from #A.entities() a asof join #B.entities() b on a.Population >= b.Population tie break by b.Id", "tie break by b.Id", "tie break by b.Array", DiagnosticCode.MQ3040_AsOfJoinInequalityColumnNotOrderable, "bind", "asof-tie-break-nonorderable", "ASOF TIE BREAK non-orderable expression", "TIE BREAK", "orderable"),
        Invalid("REC-109-S05", "ASOF_BOUNDARIES", "select 1 from #A.entities() a asof left join #B.entities() b on a.Id >= b.Id", "asof left", "asof right outer", DiagnosticCode.MQ2001_UnexpectedToken, "parse", "unsupported-asof-right-outer", "ASOF RIGHT OUTER JOIN direction", "ASOF RIGHT", "ASOF LEFT"),
        Valid("REC-109-S06", "ASOF_BOUNDARIES", "select 1 from #A.entities() a asof left join #B.entities() b on a.Population >= b.Population", "a.Population >= b.Population", "a.Population >= b.Population tie break by b.Name", "valid-asof-tie-break-placement", "ASOF LEFT JOIN tie-break repair", "ASOF LEFT JOIN"),

        Valid("REC-109-R01", "RESERVED_IDENTIFIERS", "select 1 as Value from #system.dual()", "Value", "[select]", "bracketed-select-identifier", "projection alias", "FROM"),
        Valid("REC-109-R02", "RESERVED_IDENTIFIERS", "select 1 as Value from #system.dual()", "Value", "[from]", "bracketed-from-identifier", "projection alias", "FROM"),
        Valid("REC-109-R03", "RESERVED_IDENTIFIERS", "select 1 from #system.dual() source", "source", "[order]", "bracketed-order-source-alias", "source alias", "end of source"),
        Valid("REC-109-R04", "RESERVED_IDENTIFIERS", "select 1 from #system.dual() source", "source", "[asof]", "bracketed-asof-source-alias", "source alias", "end of source"),
        Valid("REC-109-R05", "RESERVED_IDENTIFIERS", "select 1 from #system.dual() as source", "as source", "as [cross apply]", "bracketed-multiword-source-alias", "source alias after AS", "FROM"),
        Valid("REC-109-R06", "RESERVED_IDENTIFIERS", "select 1 as Value from #system.dual()", "Value", "[tie break by]", "bracketed-tie-break-identifier", "projection alias", "FROM"),

        Valid("REC-109-N01", "NATIVE_REPAIRS", "param(author: string = 'x') select 1 from #system.dual()", "param(", "params(", "native-params-declaration", "script parameter declaration", "SELECT"),
        Valid("REC-109-N02", "NATIVE_REPAIRS", "select 1 from #system.dual() where 'x' = 'x'", "'x' = 'x'", "'x' like 'x'", "native-like-predicate", "LIKE predicate", "end of predicate"),
        Valid("REC-109-N03", "NATIVE_REPAIRS", "select 1 from #system.dual()", "select 1", "select 1::Int32", "native-postfix-cast", "postfix cast expression", "FROM"),
        Valid("REC-109-N04", "NATIVE_REPAIRS", "select 1 from #system.dual() order by 1", "order by 1", "order by 1 skip 2 take 5", "native-skip-take-pagination", "result pagination clauses", "end of ordering"),
        Valid("REC-109-N05", "NATIVE_REPAIRS", "params(author: string = 'x') select 1 from #system.dual()", "params", "param", "native-param-declaration", "script parameter keyword", "SELECT"),
        Valid("REC-109-N06", "NATIVE_REPAIRS", "select a.Name from #A.entities() a inner join #B.entities() b on a.Id = b.Id", "inner join #B.entities() b on a.Id = b.Id", "asof left join #B.entities() b on a.Id >= b.Id", "native-asof-left-repair", "ASOF LEFT JOIN replacement", "end of join"),

        Valid("REC-109-C01", "CONTEXTUAL_IDENTIFIERS", "select 1 as Value from #system.dual()", "Value", "[limit]", "bracketed-limit-context", "projection alias", "FROM"),
        Valid("REC-109-C02", "CONTEXTUAL_IDENTIFIERS", "select 1 as Value from #system.dual()", "Value", "[offset]", "bracketed-offset-context", "projection alias", "FROM"),
        Valid("REC-109-C03", "CONTEXTUAL_IDENTIFIERS", "select 1 as Value from #system.dual()", "Value", "[enum]", "bracketed-enum-context", "projection alias", "FROM"),
        Valid("REC-109-C04", "CONTEXTUAL_IDENTIFIERS", "select 1 as Value from #system.dual()", "Value", "[flags]", "bracketed-flags-context", "projection alias", "FROM"),
        Valid("REC-109-C05", "CONTEXTUAL_IDENTIFIERS", "select 1 as Value from #system.dual()", "Value", "[tie]", "bracketed-tie-context", "projection alias", "FROM"),
        Valid("REC-109-C06", "CONTEXTUAL_IDENTIFIERS", "select 1 as Value from #system.dual()", "Value", "[break]", "bracketed-break-context", "projection alias", "FROM")
    ];

    private static RecoveryCase Invalid(
        string caseId,
        string family,
        string seed,
        string before,
        string after,
        DiagnosticCode expectedCode,
        string executionMode,
        string rootCause,
        string insertionPoint,
        string expectedBoundaryToken,
        string guidance) =>
        Create(caseId, family, seed, before, after, "invalid", expectedCode, executionMode, rootCause,
            insertionPoint, expectedBoundaryToken, guidance);

    private static RecoveryCase Valid(
        string caseId,
        string family,
        string seed,
        string before,
        string after,
        string rootCause,
        string insertionPoint,
        string expectedBoundaryToken) =>
        Create(caseId, family, seed, before, after, "valid_ordinary", null, "parse", rootCause,
            insertionPoint, expectedBoundaryToken, "valid");

    private static RecoveryCase Create(
        string caseId,
        string family,
        string seed,
        string before,
        string after,
        string expectedClassification,
        DiagnosticCode? expectedCode,
        string executionMode,
        string rootCause,
        string insertionPoint,
        string expectedBoundaryToken,
        string guidance)
    {
        var marker = $" /* {caseId} */";
        return new RecoveryCase(
            caseId,
            family,
            seed + marker,
            ReplaceOnce(seed, before, after) + marker,
            expectedClassification,
            expectedCode,
            executionMode,
            rootCause,
            insertionPoint,
            expectedBoundaryToken,
            guidance);
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
        return new Musoq.Parser.Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> AsOfSources() =>
        new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Name = "A1", Id = 1, Population = 100 }],
            ["#B"] = [new BasicEntity { Name = "B1", Id = 1, Population = 90 }]
        };

    private sealed record RecoveryCase(
        string CaseId,
        string Family,
        string SeedQuery,
        string ResultQuery,
        string ExpectedClassification,
        DiagnosticCode? ExpectedCode,
        string ExecutionMode,
        string RootCause,
        string InsertionPoint,
        string ExpectedBoundaryToken,
        string Guidance);
}
