using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC111CompetingFailureTests
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public void CompetingFailures_ShouldHonorFrozenContract()
    {
        foreach (var candidate in CandidateCases)
        {
            var seed = Analyze(candidate.SeedQuery);
            Assert.IsTrue(seed.IsSuccess,
                $"{candidate.CaseId}: seed must be valid before observing the mutation: {Format(seed)}");

            var result = Analyze(candidate.ResultQuery);
            if (candidate.ExpectedCodes.Length == 0)
            {
                Assert.IsTrue(result.IsSuccess,
                    $"{candidate.CaseId}: valid control produced diagnostics: {Format(result)}");
                continue;
            }

            Assert.IsFalse(result.IsSuccess, $"{candidate.CaseId}: expected a diagnostic.");
            var diagnostics = result.Errors.ToArray();
            CollectionAssert.AreEqual(candidate.ExpectedCodes, diagnostics.Select(static error => error.Code).ToArray(),
                candidate.CaseId);
            Assert.HasCount(candidate.ExpectedCodes.Length, diagnostics, candidate.CaseId);

            for (var index = 0; index < diagnostics.Length; index++)
            {
                var diagnostic = diagnostics[index];
                Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase, candidate.CaseId);
                Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind, candidate.CaseId);
                Assert.IsTrue(diagnostic.Location.IsValid, candidate.CaseId);
                Assert.IsTrue(diagnostic.EndLocation.IsValid, candidate.CaseId);
                StringAssert.Contains(diagnostic.Message, candidate.ExpectedTokens[index], candidate.CaseId);
            }

            foreach (var forbidden in candidate.ForbiddenCodes)
                Assert.IsFalse(diagnostics.Any(error => error.Code == forbidden),
                    $"{candidate.CaseId}: forbidden dependent code {forbidden} was emitted.");
        }
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainFortyEightCasesAcrossEightFamilies()
    {
        Assert.HasCount(48, CandidateCases);
        Assert.HasCount(39, CandidateCases.Where(static candidate => candidate.ExpectedCodes.Length > 0));
        Assert.HasCount(9, CandidateCases.Where(static candidate => candidate.ExpectedCodes.Length == 0));
        CollectionAssert.AllItemsAreUnique(CandidateCases.Select(static candidate => candidate.CaseId).ToArray());
        foreach (var family in Families)
            Assert.HasCount(6, CandidateCases.Where(candidate => candidate.Family == family), family);
    }

    [TestMethod]
    public void CandidateMetadata_ShouldDeclarePrimaryOrderAndForbiddenSecondaryCodes()
    {
        foreach (var candidate in CandidateCases)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.RootCause), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.ScopeBoundary), candidate.CaseId);
            Assert.IsTrue(candidate.SeedQuery.Contains($"/* {candidate.CaseId} */", StringComparison.Ordinal),
                candidate.CaseId);
            Assert.IsTrue(candidate.ResultQuery.Contains($"/* {candidate.CaseId} */", StringComparison.Ordinal),
                candidate.CaseId);
            if (candidate.ExpectedCodes.Length == 0)
            {
                Assert.AreEqual("valid_ordinary", candidate.ExpectedClassification, candidate.CaseId);
                Assert.IsEmpty(candidate.ForbiddenCodes, candidate.CaseId);
            }
            else
            {
                Assert.AreEqual("invalid", candidate.ExpectedClassification, candidate.CaseId);
                Assert.HasCount(candidate.ExpectedCodes.Length, candidate.ExpectedTokens, candidate.CaseId);
            }
        }
    }

    [TestMethod]
    public void ValidControls_ShouldExecuteWithoutChangingResolution()
    {
        foreach (var candidate in CandidateCases.Where(static candidate => candidate.ExpectedCodes.Length == 0))
        {
            Assert.IsTrue(Analyze(candidate.SeedQuery).IsSuccess, candidate.CaseId);
            Assert.IsTrue(Analyze(candidate.ResultQuery).IsSuccess, candidate.CaseId);
        }
    }

    private static readonly string[] Families =
    [
        "SCHEMA_SOURCE_ARGUMENTS", "ALIAS_CHAIN_COMPETITION", "OWNER_MEMBER_COMPETITION",
        "ROOT_PRECEDENCE", "QUERY_BLOCK_SCOPE", "INDEPENDENT_SIBLINGS", "CALLABLE_ARGUMENTS",
        "EXPORTED_NAME_BOUNDARIES"
    ];

    private static readonly IReadOnlyList<RecoveryCase> CandidateCases =
    [
        Invalid("A01", "SCHEMA_SOURCE_ARGUMENTS", "select Name from #A.Entities()", "#A.Entities()", "#missing.Entites()", DiagnosticCode.MQ3010_UnknownSchema, "missing", [DiagnosticCode.MQ3085_UnknownSource, DiagnosticCode.MQ3087_InvalidCallableArity], "unknown schema owns unknown source"),
        Invalid("A02", "SCHEMA_SOURCE_ARGUMENTS", "select Name from #A.Entities()", "#A.Entities()", "#A.Entites()", DiagnosticCode.MQ3085_UnknownSource, "Entites", [DiagnosticCode.MQ3010_UnknownSchema, DiagnosticCode.MQ3087_InvalidCallableArity], "known schema owns unknown source"),
        Invalid("A03", "SCHEMA_SOURCE_ARGUMENTS", "select Substring(Name, 0, 2) from #A.Entities()", "Substring(Name, 0, 2)", "Substring(Name)", DiagnosticCode.MQ3087_InvalidCallableArity, "Substring", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3086_UnknownCallable], "known callable owns wrong arity"),
        Invalid("A04", "SCHEMA_SOURCE_ARGUMENTS", "select Name from #A.Entities()", "#A.Entities()", "#A.Entites(1)", DiagnosticCode.MQ3085_UnknownSource, "Entites", [DiagnosticCode.MQ3010_UnknownSchema, DiagnosticCode.MQ3087_InvalidCallableArity], "unknown source precedes its arguments"),
        Invalid("A05", "SCHEMA_SOURCE_ARGUMENTS", "select Name from #A.Entities()", "#A.Entities()", "#missing.Entities(1)", DiagnosticCode.MQ3010_UnknownSchema, "missing", [DiagnosticCode.MQ3085_UnknownSource, DiagnosticCode.MQ3087_InvalidCallableArity], "unknown schema suppresses dependent source and arguments"),
        Valid("A06", "SCHEMA_SOURCE_ARGUMENTS", "select Name from #A.Entities()"),

        Invalid("B01", "ALIAS_CHAIN_COMPETITION", "select people.Name from #A.Entities() people", "people.Name", "peple.Name", DiagnosticCode.MQ3015_UnknownAlias, "peple", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3028_UnknownProperty], "unknown alias owns a simple column chain"),
        Invalid("B02", "ALIAS_CHAIN_COMPETITION", "select people.Self.Name from #A.Entities() people", "people.Self.Name", "peple.Self.Naem", DiagnosticCode.MQ3015_UnknownAlias, "peple", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3028_UnknownProperty], "unknown alias precedes plausible property and member"),
        Invalid("B03", "ALIAS_CHAIN_COMPETITION", "select Name from #A.Entities() people where people.Self.Name = 'WARSAW'", "people.Self.Name", "peple.Self.Naem", DiagnosticCode.MQ3015_UnknownAlias, "peple", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3028_UnknownProperty], "predicate alias wins over member spelling"),
        Invalid("B04", "ALIAS_CHAIN_COMPETITION", "select people.Name from #A.Entities() people", "#A.Entities() people", "#A.Entities() peple", DiagnosticCode.MQ3015_UnknownAlias, "people", [DiagnosticCode.MQ3001_UnknownColumn], "renamed declaration leaves one stale alias"),
        Invalid("B05", "ALIAS_CHAIN_COMPETITION", "select people.Name from #A.Entities() people where people.City = 'WARSAW'", "people.City", "peple.Ctiy", DiagnosticCode.MQ3015_UnknownAlias, "peple", [DiagnosticCode.MQ3001_UnknownColumn], "alias spelling wins over similar column spelling"),
        Valid("B06", "ALIAS_CHAIN_COMPETITION", "select people.Self.Name from #A.Entities() people"),

        Invalid("C01", "OWNER_MEMBER_COMPETITION", "select Self.Name from #A.Entities()", "Self.Name", "Self.Naem", DiagnosticCode.MQ3028_UnknownProperty, "Naem", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3015_UnknownAlias], "known object owns unknown property"),
        Invalid("C02", "OWNER_MEMBER_COMPETITION", "select Name from #A.Entities() where Self.City = 'WARSAW'", "Self.City", "Self.Ctiy", DiagnosticCode.MQ3028_UnknownProperty, "Ctiy", [DiagnosticCode.MQ3001_UnknownColumn], "known object property in predicate"),
        Invalid("C03", "OWNER_MEMBER_COMPETITION", "select Self.Name from #A.Entities()", "Self.Name", "Self.Dictinary", DiagnosticCode.MQ3028_UnknownProperty, "Dictinary", [DiagnosticCode.MQ3001_UnknownColumn], "property candidate is scoped to the known object"),
        Invalid("C04", "OWNER_MEMBER_COMPETITION", "select a.Name from #A.Entities() a", "a.Name", "a.Naem", DiagnosticCode.MQ3001_UnknownColumn, "Naem", [DiagnosticCode.MQ3028_UnknownProperty, DiagnosticCode.MQ3015_UnknownAlias], "known source owns unknown column"),
        Invalid("C05", "OWNER_MEMBER_COMPETITION", "select a.Name from #A.Entities() a where a.City = 'WARSAW'", "a.City", "a.Ctiy", DiagnosticCode.MQ3001_UnknownColumn, "Ctiy", [DiagnosticCode.MQ3028_UnknownProperty], "qualified column candidate stays on its source"),
        Valid("C06", "OWNER_MEMBER_COMPETITION", "select Self.Name, Name from #A.Entities()"),

        Invalid("D01", "ROOT_PRECEDENCE", "select a.Name from #A.Entities() a", "a.Name", "missing.Name", DiagnosticCode.MQ3015_UnknownAlias, "missing", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3028_UnknownProperty], "unknown qualifier suppresses dependent member"),
        Invalid("D02", "ROOT_PRECEDENCE", "select a.Name from #A.Entities() a", "a.Name", "a.Missing.Name", DiagnosticCode.MQ3001_UnknownColumn, "Missing", [DiagnosticCode.MQ3028_UnknownProperty], "unknown source column suppresses dependent member"),
        Invalid("D03", "ROOT_PRECEDENCE", "select Self.Name from #A.Entities()", "Self.Name", "Self.Missing.Name", DiagnosticCode.MQ3028_UnknownProperty, "Missing", [DiagnosticCode.MQ3001_UnknownColumn], "unknown object property suppresses dependent member"),
        Invalid("D04", "ROOT_PRECEDENCE", "select Substring(Name, 0, 2) from #A.Entities()", "Substring(Name, 0, 2)", "Substrng(Naem, 0, 2)", DiagnosticCode.MQ3086_UnknownCallable, "Substrng", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3087_InvalidCallableArity], "unknown callable suppresses invalid argument"),
        Invalid("D05", "ROOT_PRECEDENCE", "select Substring(Name, 0, 2) from #A.Entities()", "Substring(Name, 0, 2)", "Substring(Naem, 0, 2)", DiagnosticCode.MQ3001_UnknownColumn, "Naem", [DiagnosticCode.MQ3086_UnknownCallable, DiagnosticCode.MQ3087_InvalidCallableArity], "known callable exposes its unknown argument"),
        Valid("D06", "ROOT_PRECEDENCE", "select Substring(Name, 0, 2) from #A.Entities()"),

        Invalid("E01", "QUERY_BLOCK_SCOPE", "with p as (select a.Name as NameOut from #A.Entities() a) select p.NameOut from p", "p.NameOut", "q.NameOut", DiagnosticCode.MQ3015_UnknownAlias, "q", [DiagnosticCode.MQ3001_UnknownColumn], "outer alias does not borrow CTE output candidates"),
        Invalid("E02", "QUERY_BLOCK_SCOPE", "with p as (select a.Name as NameOut from #A.Entities() a) select p.NameOut from p", "p.NameOut", "p.NameOt", DiagnosticCode.MQ3001_UnknownColumn, "NameOt", [DiagnosticCode.MQ3015_UnknownAlias], "known CTE alias owns its output spelling"),
        Invalid("E03", "QUERY_BLOCK_SCOPE", "select d.Name from (select a.Name from #A.Entities() a) d", "d.Name", "q.Name", DiagnosticCode.MQ3015_UnknownAlias, "q", [DiagnosticCode.MQ3001_UnknownColumn], "derived-table alias is local to its block"),
        Invalid("E04", "QUERY_BLOCK_SCOPE", "select d.Name from (select a.Name from #A.Entities() a) d", "a.Name", "q.Self.Naem", DiagnosticCode.MQ3015_UnknownAlias, "q", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3028_UnknownProperty], "inner alias owns the nested failure"),
        Invalid("E05", "QUERY_BLOCK_SCOPE", "with p as (select a.Name from #A.Entities() a) select Name from p", "select Name from p", "select [a.Name] from p", DiagnosticCode.MQ3001_UnknownColumn, "a.Name", [DiagnosticCode.MQ3015_UnknownAlias], "source qualifier is not exported by a CTE"),
        Valid("E06", "QUERY_BLOCK_SCOPE", "select d.Name from (select a.Name from #A.Entities() a) d"),

        Invalid("F01", "INDEPENDENT_SIBLINGS", "select Name, City from #A.Entities()", "Name, City", "Missing, Unknown", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3001_UnknownColumn], ["Missing", "Unknown"], [], "independent unqualified column roots remain ordered"),
        Invalid("F02", "INDEPENDENT_SIBLINGS", "select a.Name, a.City from #A.Entities() a", "a.Name, a.City", "a.Missing, a.Unknown", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3001_UnknownColumn], ["Missing", "Unknown"], [], "independent qualified column roots remain ordered"),
        Invalid("F03", "INDEPENDENT_SIBLINGS", "select Self.Name, a.City from #A.Entities() a", "Self.Name, a.City", "Self.Missing, a.Unknown", [DiagnosticCode.MQ3028_UnknownProperty, DiagnosticCode.MQ3001_UnknownColumn], ["Missing", "Unknown"], [], "property and column roots retain distinct roles"),
        Invalid("F04", "INDEPENDENT_SIBLINGS", "select a.Name, a.City from #A.Entities() a", "a.Name, a.City", "missing.Name, a.Unknown", [DiagnosticCode.MQ3015_UnknownAlias, DiagnosticCode.MQ3001_UnknownColumn], ["missing", "Unknown"], [], "alias and column roots retain distinct roles"),
        Valid("F05", "INDEPENDENT_SIBLINGS", "select Name, City from #A.Entities()"),
        Valid("F06", "INDEPENDENT_SIBLINGS", "select a.Name, b.Name from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id"),

        Invalid("G01", "CALLABLE_ARGUMENTS", "select Substring(Name, 0, 2) from #A.Entities()", "Substring(Name, 0, 2)", "Substrng(Self.Naem, 0, 2)", DiagnosticCode.MQ3086_UnknownCallable, "Substrng", [DiagnosticCode.MQ3028_UnknownProperty], "callable name owns its dependent object argument"),
        Invalid("G02", "CALLABLE_ARGUMENTS", "select Substring(Name, 0, 2) from #A.Entities()", "Substring(Name, 0, 2)", "Substring(Self.Naem, 0, 2)", DiagnosticCode.MQ3028_UnknownProperty, "Naem", [DiagnosticCode.MQ3086_UnknownCallable], "known callable exposes known-object member failure"),
        Invalid("G03", "CALLABLE_ARGUMENTS", "select Length(Name) from #A.Entities()", "Length(Name)", "Length(Self.Naem)", DiagnosticCode.MQ3028_UnknownProperty, "Naem", [DiagnosticCode.MQ3087_InvalidCallableArity], "known unary callable preserves member root"),
        Invalid("G04", "CALLABLE_ARGUMENTS", "select Substring(Name, 0, 2) from #A.Entities()", "Substring(Name, 0, 2)", "Substring(Naem, 0, 2)", DiagnosticCode.MQ3001_UnknownColumn, "Naem", [DiagnosticCode.MQ3086_UnknownCallable], "known callable preserves column root"),
        Valid("G05", "CALLABLE_ARGUMENTS", "select Substring(Name, 0, 2) from #A.Entities()"),
        Valid("G06", "CALLABLE_ARGUMENTS", "select Length(Name) from #A.Entities()"),

        Invalid("H01", "EXPORTED_NAME_BOUNDARIES", "with p as (select a.Name from #A.Entities() a), q as (select p.Name from p) select q.Name from q", "q.Name", "r.Name", DiagnosticCode.MQ3015_UnknownAlias, "r", [DiagnosticCode.MQ3001_UnknownColumn], "outer block does not borrow nested aliases"),
        Invalid("H02", "EXPORTED_NAME_BOUNDARIES", "with p as (select a.Name from #A.Entities() a), q as (select p.Name from p) select q.Name from q", "p.Name", "x.Name", DiagnosticCode.MQ3015_UnknownAlias, "x", [DiagnosticCode.MQ3001_UnknownColumn], "inner CTE alias is not a source candidate outside its block"),
        Invalid("H03", "EXPORTED_NAME_BOUNDARIES", "select d.Name from (select a.Name from #A.Entities() a) d", "a.Name", "x.Name", DiagnosticCode.MQ3015_UnknownAlias, "x", [DiagnosticCode.MQ3001_UnknownColumn], "derived body alias remains block-local"),
        Invalid("H04", "EXPORTED_NAME_BOUNDARIES", "with p as (select a.Name from #A.Entities() a) select Name from p", "select Name from p", "select a.Name from p", DiagnosticCode.MQ3015_UnknownAlias, "a", [DiagnosticCode.MQ3001_UnknownColumn], "outer CTE block cannot use body source alias"),
        Invalid("H05", "EXPORTED_NAME_BOUNDARIES", "with p as (select a.Name as NameOut from #A.Entities() a) select NameOut from p", "select NameOut from p", "select NameOt from p", DiagnosticCode.MQ3001_UnknownColumn, "NameOt", [DiagnosticCode.MQ3015_UnknownAlias], "exported output candidates stay in the consumer block"),
        Invalid("H06", "EXPORTED_NAME_BOUNDARIES", "with p as (select a.Name as NameOut from #A.Entities() a) select p.NameOut from p", "p.NameOut", "p.NameOt", DiagnosticCode.MQ3001_UnknownColumn, "NameOt", [DiagnosticCode.MQ3015_UnknownAlias], "qualified exported output remains a column failure")
    ];

    private static RecoveryCase Invalid(
        string id,
        string family,
        string seed,
        string before,
        string after,
        DiagnosticCode code,
        string token,
        DiagnosticCode[] forbidden,
        string rootCause) =>
        Invalid(id, family, seed, before, after, [code], [token], forbidden, rootCause);

    private static RecoveryCase Invalid(
        string id,
        string family,
        string seed,
        string before,
        string after,
        DiagnosticCode[] codes,
        string[] tokens,
        DiagnosticCode[] forbidden,
        string rootCause) =>
        Create(id, family, seed, before, after, "invalid", codes, tokens, forbidden, rootCause);

    private static RecoveryCase Valid(string id, string family, string query) =>
        Create(id, family, query, query, query, "valid_ordinary", [], [], [], "ordinary control");

    private static RecoveryCase Create(
        string id,
        string family,
        string seed,
        string before,
        string after,
        string classification,
        DiagnosticCode[] codes,
        string[] tokens,
        DiagnosticCode[] forbidden,
        string rootCause)
    {
        var marker = $" /* {id} */";
        return new RecoveryCase(
            id,
            family,
            seed + marker,
            ReplaceOnce(seed, before, after) + marker,
            classification,
            codes,
            tokens,
            forbidden,
            rootCause,
            $"{family}:{id}");
    }

    private static string ReplaceOnce(string source, string before, string after)
    {
        var start = source.IndexOf(before, StringComparison.Ordinal);
        if (start < 0 || source.IndexOf(before, start + before.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException($"Expected exactly one '{before}' occurrence in '{source}'.");

        return source[..start] + after + source[(start + before.Length)..];
    }

    private static QueryAnalysisResult Analyze(string query) =>
        new QueryAnalyzer(
                new BasicSchemaProvider<BasicEntity>(CreateSources()),
                compilationOptions: CompilationOptions)
            .Analyze(query);

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources() =>
        new()
        {
            ["#A"] = [new BasicEntity("WARSAW", "PL", 100)],
            ["#B"] = [new BasicEntity("BERLIN", "DE", 200)]
        };

    private static string Format(QueryAnalysisResult result) =>
        string.Join(" | ", result.Errors.Select(static error => $"[{error.Code}] {error.Message}"));

    private sealed record RecoveryCase(
        string CaseId,
        string Family,
        string SeedQuery,
        string ResultQuery,
        string ExpectedClassification,
        DiagnosticCode[] ExpectedCodes,
        string[] ExpectedTokens,
        DiagnosticCode[] ForbiddenCodes,
        string RootCause,
        string ScopeBoundary);

}
