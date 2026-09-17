using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC112CallableDecisionTreeTests
{
    private static readonly string[] Families =
    [
        "CALLABLE_STAGE", "NAMED_SOURCE_METADATA", "HIDDEN_DEFAULTS", "OVERLOAD_STABILITY",
        "OWNER_RESOLUTION", "MIXED_PRECEDENCE", "CANONICAL_VECTOR", "ENUMERATION_ORDER"
    ];

    [TestMethod]
    public void CallableDecisionTree_ShouldHonorFrozenContract()
    {
        foreach (var candidate in CandidateCases)
        {
            var seed = Analyze(candidate, candidate.SeedQuery);
            Assert.IsTrue(seed.IsSuccess,
                $"{candidate.CaseId}: seed must be valid before the mutation: {Format(seed)}");

            var result = Analyze(candidate, candidate.ResultQuery);
            var diagnostics = result.Errors.ToArray();
            if (candidate.ExpectedCodes.Length == 0)
            {
                Assert.IsTrue(result.IsSuccess,
                    $"{candidate.CaseId}: valid control produced diagnostics: {Format(result)}");
                continue;
            }

            Assert.IsFalse(result.IsSuccess, $"{candidate.CaseId}: expected a diagnostic.");
            CollectionAssert.AreEqual(candidate.ExpectedCodes, diagnostics.Select(static error => error.Code).ToArray(), candidate.CaseId);
            Assert.HasCount(candidate.ExpectedCodes.Length, diagnostics, candidate.CaseId);

            for (var index = 0; index < diagnostics.Length; index++)
            {
                var diagnostic = diagnostics[index];
                var expectedPhase = candidate.CaseId is "A05" or "C04"
                    ? DiagnosticPhase.Parse
                    : DiagnosticPhase.Bind;
                Assert.AreEqual(expectedPhase, diagnostic.Phase, candidate.CaseId);
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
        Assert.HasCount(8, Families);
        CollectionAssert.AllItemsAreUnique(CandidateCases.Select(static candidate => candidate.CaseId).ToArray());
        foreach (var family in Families)
            Assert.HasCount(6, CandidateCases.Where(candidate => candidate.Family == family), family);
    }

    [TestMethod]
    public void NamedArguments_ShouldDeliverCanonicalRuntimeVectors()
    {
        var captures = new List<object?[]>();
        var candidate = CandidateCases.Single(static item => item.CaseId == "G01");
        var result = Analyze(candidate, candidate.ResultQuery, captures);

        Assert.IsTrue(result.IsSuccess, Format(result));
        CollectionAssert.AreEqual(
            new object?[] { "value", 4, "last" },
            captures.Single(),
            $"captures={string.Join(" | ", captures.Select(static item => $"[{string.Join(",", item.Select(value => value?.ToString() ?? "<null>"))}]"))}");
    }

    [TestMethod]
    public void NamedArguments_ShouldInsertUsableOptionalDefaults()
    {
        var captures = new List<object?[]>();
        var candidate = CandidateCases.Single(static item => item.CaseId == "G02");
        var result = Analyze(candidate, candidate.ResultQuery, captures);

        Assert.IsTrue(result.IsSuccess, Format(result));
        CollectionAssert.AreEqual(new object?[] { "value", 7 }, captures.Single());
    }

    [TestMethod]
    public void ReversedReflectionEnumeration_ShouldKeepAmbiguityStable()
    {
        var forward = Analyze(CandidateCases.Single(static item => item.CaseId == "D02"),
            CandidateCases.Single(static item => item.CaseId == "D02").ResultQuery);
        var reverse = Analyze(CandidateCases.Single(static item => item.CaseId == "D03"),
            CandidateCases.Single(static item => item.CaseId == "D03").ResultQuery);

        var forwardDiagnostic = forward.Errors.Single();
        var reverseDiagnostic = reverse.Errors.Single();
        Assert.AreEqual(DiagnosticCode.MQ3089_AmbiguousCallableOverload, forwardDiagnostic.Code);
        Assert.AreEqual(forwardDiagnostic.Code, reverseDiagnostic.Code);
        Assert.AreEqual(forwardDiagnostic.Message, reverseDiagnostic.Message);
    }

    [TestMethod]
    public void OwnerAmbiguity_ShouldRemainDistinctFromOverloadAmbiguity()
    {
        var overload = Analyze(CandidateCases.Single(static item => item.CaseId == "D02"),
            CandidateCases.Single(static item => item.CaseId == "D02").ResultQuery);
        var owner = Analyze(CandidateCases.Single(static item => item.CaseId == "E03"),
            CandidateCases.Single(static item => item.CaseId == "E03").ResultQuery);

        Assert.AreEqual(DiagnosticCode.MQ3089_AmbiguousCallableOverload, overload.Errors.Single().Code);
        Assert.AreEqual(DiagnosticCode.MQ3035_AmbiguousMethodOwner, owner.Errors.Single().Code);
    }

    private static QueryAnalysisResult Analyze(
        RecoveryCase candidate,
        string query,
        List<object?[]>? captures = null)
    {
        if (candidate.Scenario.StartsWith("owner-", StringComparison.Ordinal))
            return new QueryAnalyzer(CreateOwnerProvider(candidate.Scenario)).Analyze(query);

        var scenario = candidate.Scenario;
        if ((scenario is "ambiguous" or "ambiguous-reverse") &&
            !query.Contains("'text'", StringComparison.Ordinal))
        {
            scenario = "integer-only";
        }

        if (scenario == "optional" && query.Contains("first:", StringComparison.Ordinal))
            scenario = "optional-text";

        if (scenario == "unknown-schema" && !query.Contains("#missing", StringComparison.Ordinal))
            scenario = "required";

        if (candidate.CaseId == "A05" && query.Contains("MissingSource", StringComparison.Ordinal))
            return AnalyzeStandaloneNamedSource(query);

        return new QueryAnalyzer(new MatrixSchemaProvider(
            scenario,
            captures is null ? null : captures.Add)).Analyze(query);
    }

    private static QueryAnalysisResult AnalyzeStandaloneNamedSource(string query)
    {
        try
        {
            var tree = new Musoq.Parser.Parser(new Lexer(query, true)).ComposeAll();
            var visitor = new BuildMetadataAndInferTypesVisitor(
                new MatrixSchemaProvider("required", null),
                new Dictionary<string, string[]>(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<BuildMetadataAndInferTypesVisitor>.Instance);
            tree.Accept(new BuildMetadataAndInferTypesTraverseVisitor(visitor));
            return new QueryAnalysisResult { Root = tree };
        }
        catch (Exception exception)
        {
            return new QueryAnalysisResult
            {
                Diagnostics = [exception.ToDiagnosticOrGeneric(new SourceText(query))]
            };
        }
    }

    private static GenericSchemaProvider CreateOwnerProvider(string scenario)
    {
        return scenario switch
        {
            "owner-shared" => CreateOwnerProvider<MethodOwnerAutoResolutionTests.SharedOnlyLibraryA,
                MethodOwnerAutoResolutionTests.SharedOnlyLibraryB>(),
            "owner-unique" => CreateOwnerProvider<MethodOwnerAutoResolutionTests.UniqueMethodLibraryA,
                MethodOwnerAutoResolutionTests.SharedOnlyLibraryB>(),
            "owner-ambiguous" or "owner-ambiguous-reverse" => CreateOwnerProvider<
                MethodOwnerAutoResolutionTests.AmbiguousMethodLibraryA,
                MethodOwnerAutoResolutionTests.AmbiguousMethodLibraryB>(),
            "owner-injected" => CreateOwnerProvider<Library, Library>(),
            _ => throw new InvalidOperationException($"Unknown owner scenario '{scenario}'.")
        };
    }

    private static GenericSchemaProvider CreateOwnerProvider<TLeft, TRight>()
        where TLeft : LibraryBase, new()
        where TRight : LibraryBase, new()
    {
        return new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            ["#A"] = CreateOwnerSchema<TLeft>([new BasicEntity("Warsaw", "Poland", 100)]),
            ["#B"] = CreateOwnerSchema<TRight>([new BasicEntity("Warsaw", "Poland", 200)])
        });
    }

    private static GenericSchema<TLibrary> CreateOwnerSchema<TLibrary>(BasicEntity[] source)
        where TLibrary : LibraryBase, new()
    {
        return new GenericSchema<TLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["entities"] = (new BasicEntityTable(), new MultiRowSource<BasicEntity>(source))
        });
    }

    private static string Format(QueryAnalysisResult result) =>
        string.Join(" | ", result.Errors.Select(static error => $"[{error.Code}] {error.Message}"));

    private static readonly IReadOnlyList<RecoveryCase> CandidateCases =
    [
        Invalid("A01", "CALLABLE_STAGE", "scalar", "select Substring('abc', 0, 2) from #matrix.source(value: 1)",
            "Substring('abc', 0, 2)", "Substrng(NotAColumn, 'zero', 5)", DiagnosticCode.MQ3086_UnknownCallable, "Substrng",
            [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3087_InvalidCallableArity, DiagnosticCode.MQ3088_NoMatchingCallableOverload],
            "callable name precedes argument roots"),
        Invalid("A02", "CALLABLE_STAGE", "scalar", "select Substring('abc', 0, 2) from #matrix.source(value: 1)",
            "Substring('abc', 0, 2)", "Substring(NotAColumn)", DiagnosticCode.MQ3087_InvalidCallableArity, "Substring",
            [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3086_UnknownCallable],
            "known callable arity precedes argument roots"),
        Invalid("A03", "CALLABLE_STAGE", "scalar", "select Substring('abc', 0, 2) from #matrix.source(value: 1)",
            "Substring('abc', 0, 2)", "Substring('abc', 'zero', 5)", DiagnosticCode.MQ3088_NoMatchingCallableOverload, "Substring",
            [DiagnosticCode.MQ3086_UnknownCallable, DiagnosticCode.MQ3087_InvalidCallableArity],
            "known callable types follow arity"),
        Invalid("A04", "CALLABLE_STAGE", "scalar", "select Substring('abc', 0, 2) from #matrix.source(value: 1)",
            "Substring('abc', 0, 2)", "Sum('text')", DiagnosticCode.MQ3088_NoMatchingCallableOverload, "Sum",
            [DiagnosticCode.MQ3086_UnknownCallable, DiagnosticCode.MQ3087_InvalidCallableArity],
            "aggregate overload remains distinct"),
        Invalid("A05", "CALLABLE_STAGE", "scalar", "select Substring('abc', 0, 2) from #matrix.source(value: 1)",
            "#matrix.source(value: 1)", "MissingSource(value: 1)", DiagnosticCode.MQ2034_InvalidNamedSourceArgument, "Named arguments",
            [DiagnosticCode.MQ3086_UnknownCallable, DiagnosticCode.MQ3085_UnknownSource],
            "named arguments do not fall through standalone resolution"),
        Valid("A06", "CALLABLE_STAGE", "scalar", "select Substring('abc', 0, 2) from #matrix.source(value: 1)"),

        Invalid("B01", "NAMED_SOURCE_METADATA", "required", "select 1 from #matrix.source(value: 1)",
            "value", "vlaue", DiagnosticCode.MQ3079_UnknownSourceArgument, "vlaue", [],
            "unknown named parameter precedes binding"),
        Invalid("B02", "NAMED_SOURCE_METADATA", "required", "select 1 from #matrix.source(value: 1)",
            "value: 1", "value: 1, VALUE: 2", DiagnosticCode.MQ3080_DuplicateSourceArgument, "VALUE", [],
            "duplicate assignment is rejected"),
        Invalid("B03", "NAMED_SOURCE_METADATA", "required-two", "select 1 from #matrix.source(value: 1, other: 2)",
            "value: 1, other: 2", "value: 1", DiagnosticCode.MQ3081_MissingRequiredSourceArgument, "other", [],
            "missing required named parameter"),
        Invalid("B04", "NAMED_SOURCE_METADATA", "metadata-less", "select 1 from #matrix.source(1)",
            "#matrix.source(1)", "#matrix.source(value: 1)", DiagnosticCode.MQ3083_NamedSourceArgumentsRequireMetadata, "metadata", [],
            "named binding requires metadata"),
        Invalid("B05", "NAMED_SOURCE_METADATA", "mismatched", "select 1 from #matrix.source('text')",
            "#matrix.source('text')", "#matrix.source(value: 'text')", DiagnosticCode.MQ3083_NamedSourceArgumentsRequireMetadata, "metadata", [],
            "metadata mismatch disables names"),
        Valid("B06", "NAMED_SOURCE_METADATA", "required", "select 1 from #matrix.source(value: 1)"),

        Valid("C01", "HIDDEN_DEFAULTS", "hidden", "select 1 from #matrix.source(value: 1)"),
        Valid("C02", "HIDDEN_DEFAULTS", "optional", "select 1 from #matrix.source(value: 1)"),
        Invalid("C03", "HIDDEN_DEFAULTS", "unusable-default", "select 1 from #matrix.source(1)",
            "#matrix.source(1)", "#matrix.source()", DiagnosticCode.MQ3087_InvalidCallableArity, "source", [],
            "unusable reflected default remains required"),
        Invalid("C04", "HIDDEN_DEFAULTS", "triple", "select 1 from #matrix.source(first: 'a', second: 2, last: 'c')",
            "first: 'a', second: 2, last: 'c'", "last: 'c', 'a', second: 2", DiagnosticCode.MQ2034_InvalidNamedSourceArgument, "Positional datasource", [],
            "positional arguments cannot follow named arguments"),
        Valid("C05", "HIDDEN_DEFAULTS", "required", "select 1 from #matrix.source(VALUE: 1)"),
        Valid("C06", "HIDDEN_DEFAULTS", "required", "select 1 from #matrix.source(value: 1)"),

        Valid("D01", "OVERLOAD_STABILITY", "exact", "select 1 from #matrix.source(value: 'text')"),
        Invalid("D02", "OVERLOAD_STABILITY", "ambiguous", "select 1 from #matrix.source(value: 1)",
            "#matrix.source(value: 1)", "#matrix.source(value: 'text')", DiagnosticCode.MQ3089_AmbiguousCallableOverload, "Multiple datasource", [],
            "equal assignable overloads are ambiguous"),
        Invalid("D03", "OVERLOAD_STABILITY", "ambiguous-reverse", "select 1 from #matrix.source(value: 1)",
            "#matrix.source(value: 1)", "#matrix.source(value: 'text')", DiagnosticCode.MQ3089_AmbiguousCallableOverload, "Multiple datasource", [],
            "reflection order does not select an overload"),
        Invalid("D04", "OVERLOAD_STABILITY", "integer-only", "select 1 from #matrix.source(1)",
            "#matrix.source(1)", "#matrix.source('text')", DiagnosticCode.MQ3088_NoMatchingCallableOverload, "No datasource overload", [],
            "incompatible source argument types"),
        Valid("D05", "OVERLOAD_STABILITY", "positional-overloads", "select 1 from #matrix.source(1)"),
        Valid("D06", "OVERLOAD_STABILITY", "required", "select 1 from #matrix.source(value: 1)"),

        Valid("E01", "OWNER_RESOLUTION", "owner-shared", "select ToDecimal(a.Population) from #A.entities() a inner join #B.entities() b on a.City = b.City"),
        Valid("E02", "OWNER_RESOLUTION", "owner-unique", "select UniqueToA(b.Population) from #A.entities() a inner join #B.entities() b on a.City = b.City"),
        Invalid("E03", "OWNER_RESOLUTION", "owner-ambiguous", "select ToDecimal(a.Population) from #A.entities() a inner join #B.entities() b on a.City = b.City",
            "ToDecimal(a.Population)", "AmbiguousMethod(a.Population)", DiagnosticCode.MQ3035_AmbiguousMethodOwner, "ambiguous", [],
            "different implementations create owner ambiguity"),
        Invalid("E04", "OWNER_RESOLUTION", "owner-injected", "select a.GetCountry() from #A.entities() a inner join #B.entities() b on a.City = b.City",
            "a.GetCountry()", "GetCountry()", DiagnosticCode.MQ3035_AmbiguousMethodOwner, "ambiguous", [],
            "source-injected methods remain owner-specific"),
        Valid("E05", "OWNER_RESOLUTION", "owner-ambiguous", "select a.AmbiguousMethod(b.Population) from #A.entities() a inner join #B.entities() b on a.City = b.City"),
        Invalid("E06", "OWNER_RESOLUTION", "owner-ambiguous-reverse", "select a.AmbiguousMethod(b.Population) from #A.entities() a inner join #B.entities() b on a.City = b.City",
            "select a.AmbiguousMethod(b.Population) from #A.entities() a inner join #B.entities() b", "select AmbiguousMethod(b.Population) from #B.entities() b inner join #A.entities() a", DiagnosticCode.MQ3035_AmbiguousMethodOwner, "ambiguous", [],
            "owner ambiguity is source-order independent"),

        Invalid("F01", "MIXED_PRECEDENCE", "unknown-schema", "select 1 from #matrix.source(value: 1)",
            "#matrix.source(value: 1)", "#missing.source(vlaue: 1)", DiagnosticCode.MQ3010_UnknownSchema, "missing", [DiagnosticCode.MQ3079_UnknownSourceArgument, DiagnosticCode.MQ3085_UnknownSource],
            "schema lookup precedes source argument binding"),
        Invalid("F02", "MIXED_PRECEDENCE", "required", "select 1 from #matrix.source(1)",
            "#matrix.source(1)", "#matrix.source(1, 'extra')", DiagnosticCode.MQ3087_InvalidCallableArity, "argument count", [DiagnosticCode.MQ3088_NoMatchingCallableOverload],
            "source arity precedes source types"),
        Invalid("F03", "MIXED_PRECEDENCE", "required", "select 1 from #matrix.source(value: 1)",
            "value: 1", "vlaue: 1", DiagnosticCode.MQ3079_UnknownSourceArgument, "vlaue", [DiagnosticCode.MQ3088_NoMatchingCallableOverload],
            "named spelling precedes compatible type"),
        Invalid("F04", "MIXED_PRECEDENCE", "integer-only", "select 1 from #matrix.source(1)",
            "#matrix.source(1)", "#matrix.source(value: 'text')", DiagnosticCode.MQ3088_NoMatchingCallableOverload, "No datasource overload", [DiagnosticCode.MQ3086_UnknownCallable],
            "source overload failure is not unknown callable"),
        Invalid("F05", "MIXED_PRECEDENCE", "required", "select 1 from #matrix.source(value: 1)",
            "value: 1", "value: 1, VALUE: 2", DiagnosticCode.MQ3080_DuplicateSourceArgument, "VALUE", [DiagnosticCode.MQ3086_UnknownCallable],
            "duplicate named assignment remains source-specific"),
        Valid("F06", "MIXED_PRECEDENCE", "required", "select 1 from #matrix.source(value: 1)"),

        Valid("G01", "CANONICAL_VECTOR", "triple", "select 1 from #matrix.source(second: 4, first: 'value', last: 'last')"),
        Valid("G02", "CANONICAL_VECTOR", "optional", "select 1 from #matrix.source(first: 'value')"),
        Valid("G03", "CANONICAL_VECTOR", "triple", "select 1 from #matrix.source('first', last: 'last', second: 4)"),
        Valid("G04", "CANONICAL_VECTOR", "hidden", "select 1 from #matrix.source(value: 1)"),
        Valid("G05", "CANONICAL_VECTOR", "metadata-less", "select 1 from #matrix.source(1)"),
        Valid("G06", "CANONICAL_VECTOR", "required", "select 1 from #matrix.source(value: 1)"),

        Valid("H01", "ENUMERATION_ORDER", "exact-reverse", "select 1 from #matrix.source(value: 'text')"),
        Invalid("H02", "ENUMERATION_ORDER", "ambiguous-reverse", "select 1 from #matrix.source(value: 1)",
            "#matrix.source(value: 1)", "#matrix.source(value: 'text')", DiagnosticCode.MQ3089_AmbiguousCallableOverload, "Multiple datasource", [],
            "candidate list is canonical under reflection permutation"),
        Valid("H03", "ENUMERATION_ORDER", "positional-overloads-reverse", "select 1 from #matrix.source(1)"),
        Invalid("H04", "ENUMERATION_ORDER", "owner-ambiguous-reverse", "select a.AmbiguousMethod(b.Population) from #A.entities() a inner join #B.entities() b on a.City = b.City",
            "select a.AmbiguousMethod(b.Population) from #A.entities() a inner join #B.entities() b", "select AmbiguousMethod(b.Population) from #B.entities() b inner join #A.entities() a", DiagnosticCode.MQ3035_AmbiguousMethodOwner, "ambiguous", [],
            "owner candidates are canonical under source permutation"),
        Valid("H05", "ENUMERATION_ORDER", "required", "select 1 from #matrix.source(value: 1)"),
        Valid("H06", "ENUMERATION_ORDER", "required", "select 1 from #matrix.source(value: 1)"),
    ];

    private static RecoveryCase Invalid(
        string id,
        string family,
        string scenario,
        string seed,
        string before,
        string after,
        DiagnosticCode code,
        string token,
        DiagnosticCode[] forbidden,
        string rootCause) =>
        Create(id, family, scenario, seed, before, after, "invalid", [code], [token], forbidden, rootCause);

    private static RecoveryCase Valid(string id, string family, string scenario, string query) =>
        Create(id, family, scenario, query, query, query, "valid_ordinary", [], [], [], "ordinary control");

    private static RecoveryCase Create(
        string id,
        string family,
        string scenario,
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
            scenario,
            seed + marker,
            ReplaceOnce(seed, before, after) + marker,
            classification,
            codes,
            tokens,
            forbidden,
            rootCause);
    }

    private static string ReplaceOnce(string source, string before, string after)
    {
        var start = source.IndexOf(before, StringComparison.Ordinal);
        if (start < 0 || source.IndexOf(before, start + before.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException($"Expected exactly one '{before}' occurrence in '{source}'.");

        return source[..start] + after + source[(start + before.Length)..];
    }

    private sealed record RecoveryCase(
        string CaseId,
        string Family,
        string Scenario,
        string SeedQuery,
        string ResultQuery,
        string ExpectedClassification,
        DiagnosticCode[] ExpectedCodes,
        string[] ExpectedTokens,
        DiagnosticCode[] ForbiddenCodes,
        string RootCause);

    private sealed class MatrixSchemaProvider(string scenario, Action<object?[]>? capture) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            var normalized = schema.StartsWith("#", StringComparison.Ordinal)
                ? schema[1..]
                : schema;
            if (!string.Equals(normalized, "matrix", StringComparison.OrdinalIgnoreCase))
                throw new SourceNotFoundException($"Schema '{schema}' was not found.");

            return new MatrixSchema(scenario, capture);
        }
    }

    private sealed class MatrixSchema(string scenario, Action<object?[]>? capture)
        : SchemaBase("matrix", CreateMethods())
    {
        private static MethodsAggregator CreateMethods()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(manager);
        }

        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext) =>
            MatrixSignatures.For(scenario);

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            capture?.Invoke(parameters);
            return new MatrixTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters) => throw new NotSupportedException();
    }

    private static class MatrixSignatures
    {
        public static SchemaMethodInfo[] For(string scenario)
        {
            return scenario switch
            {
                "required" or "scalar" => [Method(typeof(RequiredTable), ("value", typeof(int)))],
                "required-two" => [Method(typeof(RequiredTwoTable), ("value", typeof(int)), ("other", typeof(int)))],
                "metadata-less" => [new SchemaMethodInfo("source", SchemaConstructorInfo.Empty())],
                "mismatched" => [Method(typeof(MismatchedTable), ("value", typeof(string)))],
                "hidden" => [new SchemaMethodInfo("source", new SchemaConstructorInfo(
                    typeof(HiddenContextTable).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Single(),
                    false, ("value", typeof(int))))],
                "optional" => [Method(typeof(OptionalTable), ("value", typeof(int)), ("other", typeof(int)))],
                "optional-text" => [Method(typeof(OptionalTextTable), ("first", typeof(string)), ("second", typeof(int)))],
                "unusable-default" => [Method(typeof(UnusableDefaultTable), ("value", typeof(int)))],
                "triple" => [Method(typeof(TripleTable), ("first", typeof(string)), ("second", typeof(int)), ("last", typeof(string)))],
                "ambiguous" => [
                    Method(typeof(AmbiguousComparableTable), ("value", typeof(IComparable))),
                    Method(typeof(AmbiguousConvertibleTable), ("value", typeof(IConvertible)))
                ],
                "ambiguous-reverse" => [
                    Method(typeof(AmbiguousConvertibleTable), ("value", typeof(IConvertible))),
                    Method(typeof(AmbiguousComparableTable), ("value", typeof(IComparable)))
                ],
                "exact" => [
                    Method(typeof(StringTable), ("value", typeof(string))),
                    Method(typeof(ObjectTable), ("value", typeof(object)))
                ],
                "exact-reverse" => [
                    Method(typeof(ObjectTable), ("value", typeof(object))),
                    Method(typeof(StringTable), ("value", typeof(string)))
                ],
                "integer-only" => [Method(typeof(RequiredTable), ("value", typeof(int)))],
                "positional-overloads" => [
                    Method(typeof(RequiredTable), ("value", typeof(int))),
                    Method(typeof(OptionalTable), ("value", typeof(int)), ("other", typeof(int)))
                ],
                "positional-overloads-reverse" => [
                    Method(typeof(OptionalTable), ("value", typeof(int)), ("other", typeof(int))),
                    Method(typeof(RequiredTable), ("value", typeof(int)))
                ],
                _ => throw new InvalidOperationException($"Unknown matrix scenario '{scenario}'.")
            };
        }

        private static SchemaMethodInfo Method(Type tableType, params (string Name, Type Type)[] arguments)
        {
            var constructor = tableType
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(candidate => candidate.GetParameters().Length == arguments.Length);
            return new SchemaMethodInfo("source", new SchemaConstructorInfo(constructor, false, arguments));
        }
    }

    private sealed class MatrixTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => [];
        public ISchemaColumn? GetColumnByName(string name) => null;
        public ISchemaColumn[] GetColumnsByName(string name) => [];
        public SchemaTableMetadata Metadata { get; } = new(typeof(object));
    }

    private abstract class MatrixTableBase : ISchemaTable
    {
        public ISchemaColumn[] Columns => [];
        public ISchemaColumn? GetColumnByName(string name) => null;
        public ISchemaColumn[] GetColumnsByName(string name) => [];
        public SchemaTableMetadata Metadata { get; } = new(typeof(object));
    }

    private sealed class RequiredTable : MatrixTableBase
    {
        public RequiredTable(int value) => _ = value;
    }

    private sealed class RequiredTwoTable : MatrixTableBase
    {
        public RequiredTwoTable(int value, int other) => _ = (value, other);
    }

    private sealed class OptionalTable : MatrixTableBase
    {
        public OptionalTable(int value, int other = 7) => _ = (value, other);
    }

    private sealed class OptionalTextTable : MatrixTableBase
    {
        public OptionalTextTable(string first, int second = 7) => _ = (first, second);
    }

    private sealed class HiddenContextTable : MatrixTableBase
    {
        public HiddenContextTable(SourceExecutionContext context, int value) => _ = (context, value);
    }

    private sealed class MismatchedTable : MatrixTableBase
    {
        public MismatchedTable(int value) => _ = value;
    }

    private sealed class UnusableDefaultTable : MatrixTableBase
    {
        public UnusableDefaultTable([Optional] int value) => _ = value;
    }

    private sealed class TripleTable : MatrixTableBase
    {
        public TripleTable(string first, int second, string last) => _ = (first, second, last);
    }

    private sealed class AmbiguousComparableTable : MatrixTableBase
    {
        public AmbiguousComparableTable(IComparable value) => _ = value;
    }

    private sealed class AmbiguousConvertibleTable : MatrixTableBase
    {
        public AmbiguousConvertibleTable(IConvertible value) => _ = value;
    }

    private sealed class StringTable : MatrixTableBase
    {
        public StringTable(string value) => _ = value;
    }

    private sealed class ObjectTable : MatrixTableBase
    {
        public ObjectTable(object value) => _ = value;
    }
}
