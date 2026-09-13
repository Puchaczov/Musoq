using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC123IndependentRootsTests
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    private static readonly IReadOnlyList<RecoveryCase> CandidateCases =
    [
        Invalid("A01", "PROJECTION_POISON", "select a.Name from #A.Entities() a", "a.Name", "missing.Name.Deep", [DiagnosticCode.MQ3015_UnknownAlias], ["missing"], "unknown qualifier poisons the whole projection chain", ProviderKind.Basic),
        Invalid("A02", "PROJECTION_POISON", "select a.Name from #A.Entities() a", "a.Name", "a.Missing.Deep", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "unknown source column poisons dependent members", ProviderKind.Basic),
        Invalid("A03", "PROJECTION_POISON", "select Self.Name from #A.Entities()", "Self.Name", "Self.Missing.Deep", [DiagnosticCode.MQ3028_UnknownProperty], ["Missing"], "unknown object property poisons dependent members", ProviderKind.Basic),
        Invalid("A04", "PROJECTION_POISON", "select Length(a.Name) from #A.Entities() a", "a.Name", "Length(a.Missing)", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "known callable exposes one invalid argument root", ProviderKind.Basic),
        Invalid("A05", "PROJECTION_POISON", "select a.Name from #A.Entities() a", "a.Name", "(a.Name + a.Population)::Int32", [DiagnosticCode.MQ3007_InvalidOperandTypes], ["+"], "invalid operand root suppresses a dependent cast", ProviderKind.Basic),
        Invalid("A06", "PROJECTION_POISON", "select a.Name, a.City from #A.Entities() a", "a.Name, a.City", "missing.Name.Deep, a.Unknown.Other", [DiagnosticCode.MQ3015_UnknownAlias, DiagnosticCode.MQ3001_UnknownColumn], ["missing", "Unknown"], "two projection roots retain source order", ProviderKind.Basic),

        Invalid("B01", "AGGREGATE_POISON", "select Sum(a.Population) from #A.Entities() a", "Sum(a.Population)", "Sum(a.Missing.Deep)", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "aggregate argument keeps its unknown column root", ProviderKind.Basic),
        Invalid("B02", "AGGREGATE_POISON", "select Sum(a.Population) from #A.Entities() a", "Sum(a.Population)", "Sum(a.Name)", [DiagnosticCode.MQ3088_NoMatchingCallableOverload], ["Sum"], "aggregate overload failure is the root for a string argument", ProviderKind.Basic),
        Invalid("B03", "AGGREGATE_POISON", "select Sum(a.Population) from #A.Entities() a", "Sum(a.Population)", "Sum(a.Missing)::Decimal", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "aggregate result cast does not duplicate an invalid argument", ProviderKind.Basic),
        Invalid("B04", "AGGREGATE_POISON", "select Sum(a.Population) from #A.Entities() a", "Sum(a.Population)", "Sum(a.Name + a.Population)", [DiagnosticCode.MQ3007_InvalidOperandTypes], ["+"], "aggregate type resolution stops at the invalid operand root", ProviderKind.Basic),
        Invalid("B05", "AGGREGATE_POISON", "select a.City, Count(*) from #A.Entities() a group by a.City", "group by a.City", "group by a.City having Sum(a.Missing) > 0", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "HAVING retains its invalid aggregate argument root", ProviderKind.Basic),
        Invalid("B06", "AGGREGATE_POISON", "select Sum(a.Population), a.City from #A.Entities() a group by a.City", "Sum(a.Population), a.City", "Sum(a.Missing), a.Unknown", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3001_UnknownColumn], ["Missing", "Unknown"], "aggregate and projection roots remain independently visible", ProviderKind.Basic),

        Invalid("C01", "WINDOW_POISON", "select RowNumber() over (order by a.Name) from #A.Entities() a", "order by a.Name", "partition by a.Missing order by a.Name", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "window partition root remains precise", ProviderKind.Basic),
        Invalid("C02", "WINDOW_POISON", "select Sum(a.Population) over (order by a.Name) from #A.Entities() a", "order by a.Name", "order by a.Missing", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "window order root remains precise", ProviderKind.Basic),
        Invalid("C03", "WINDOW_POISON", "select Sum(a.Population) over (order by a.Population) from #A.Entities() a", "over (order by a.Population)", "over (range between unbounded preceding and current row)", [DiagnosticCode.MQ3052_RangeFrameRequiresOrderBy], ["RANGE"], "window frame root is not replaced by a callable cascade", ProviderKind.Basic),
        Invalid("C04", "WINDOW_POISON", "select Sum(a.Population) over (order by a.Population rows between unbounded preceding and current row) from #A.Entities() a", "over (order by a.Population rows between unbounded preceding and current row)", "over (order by a.Name rows between unbounded following and current row)", [DiagnosticCode.MQ3053_InvalidWindowFrameBounds], ["UNBOUNDED FOLLOWING"], "invalid window bounds remain one root", ProviderKind.Basic),
        Invalid("C05", "WINDOW_POISON", "select a.Name from #A.Entities() a qualify RowNumber() over (order by a.Name) <= 2", "order by a.Name", "order by a.Missing", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "QUALIFY retains an invalid window key root", ProviderKind.Basic),
        Invalid("C06", "WINDOW_POISON", "select RowNumber() over (order by a.Name), Sum(a.Population) over (order by a.City) from #A.Entities() a", "order by a.Name), Sum(a.Population) over (order by a.City)", "order by a.Missing), Sum(a.Unknown) over (order by a.City)", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3001_UnknownColumn], ["Missing", "Unknown"], "independent window roots retain source order", ProviderKind.Basic),

        Invalid("D01", "CAST_POISON", "select a.Population from #A.Entities() a", "a.Population", "a.Missing::Int32", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "cast preserves an unknown source-column root", ProviderKind.Basic),
        Invalid("D02", "CAST_POISON", "select a.Population from #A.Entities() a", "a.Population", "a.Missing::Int32::String", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "nested casts do not create dependent member noise", ProviderKind.Basic),
        Invalid("D03", "CAST_POISON", "select a.Name from #A.Entities() a", "a.Name", "a.Name::text", [DiagnosticCode.MQ3090_UnsupportedCastTarget], ["text"], "unsupported cast target remains the cast root", ProviderKind.Basic),
        Invalid("D04", "CAST_POISON", "select a.Population from #A.Entities() a", "a.Population", "ToInt32(a.Missing)::String", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "callable and cast preserve the innermost invalid root", ProviderKind.Basic),
        Invalid("D05", "CAST_POISON", "select Sum(a.Population) from #A.Entities() a", "Sum(a.Population)", "Sum(a.Missing::Int32)", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "aggregate cast keeps the source root", ProviderKind.Basic),
        Invalid("D06", "CAST_POISON", "select a.Population, a.Money from #A.Entities() a", "a.Population, a.Money", "a.Missing::Int32, a.Unknown::String", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3001_UnknownColumn], ["Missing", "Unknown"], "two cast roots retain source order", ProviderKind.Basic),

        Invalid("E01", "SET_OUTPUT_POISON", "select a.Name from #A.Entities() a union select b.Name from #B.Entities() b", "a.Name", "a.Missing", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "set output retains an invalid left-arm root", ProviderKind.Basic),
        Invalid("E02", "SET_OUTPUT_POISON", "select a.Name from #A.Entities() a union select b.Name from #B.Entities() b", "a.Name", "a.Missing.Deep", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "set output suppresses dependent member noise", ProviderKind.Basic),
        Invalid("E03", "SET_OUTPUT_POISON", "select a.Name from #A.Entities() a union select b.Name from #B.Entities() b", "a.Name from #A.Entities() a union select b.Name", "a.Unknown from #A.Entities() a union select b.Missing", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3001_UnknownColumn], ["Unknown", "Missing"], "independent set-arm roots retain set order", ProviderKind.Basic),
        Invalid("E04", "SET_OUTPUT_POISON", "select a.Name from #A.Entities() a union select b.Name from #B.Entities() b", "b.Name", "b.Population", [DiagnosticCode.MQ3020_SetOperatorColumnTypes], ["types"], "set output type mismatch remains the set root", ProviderKind.Basic),
        Invalid("E05", "SET_OUTPUT_POISON", "select a.Name from #A.Entities() a union select b.Name from #B.Entities() b", "a.Name", "a.Name, a.City", [DiagnosticCode.MQ3019_SetOperatorColumnCount], ["columns"], "set output count mismatch remains the set root", ProviderKind.Basic),
        Invalid("E06", "SET_OUTPUT_POISON", "select a.Name from #A.Entities() a except select b.Name from #B.Entities() b", "b.Name", "b.Population", [DiagnosticCode.MQ3020_SetOperatorColumnTypes], ["types"], "EXCEPT output type mismatch remains the set root", ProviderKind.Basic),

        Invalid("F01", "RECURSIVE_MEMBER_POISON", RecursiveSeed, "select c.Value + 1", "select c.Missing + 1", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "recursive member retains its unknown source root", ProviderKind.Basic),
        Invalid("F02", "RECURSIVE_MEMBER_POISON", RecursiveSeed, "select c.Value + 1", "select c.Value.Missing + 1", [DiagnosticCode.MQ3028_UnknownProperty], ["Missing"], "recursive member suppresses dependent property noise", ProviderKind.Basic),
        Invalid("F03", "RECURSIVE_MEMBER_POISON", RecursiveSeed, "select c.Value + 1", "select c.Value + 1, c.Value", [DiagnosticCode.MQ3076_RecursiveCteOutputMismatch], ["projects 2"], "recursive member output shape owns its dependent result failure", ProviderKind.Basic),
        Invalid("F04", "RECURSIVE_MEMBER_POISON", RecursiveSeed, "select c.Value + 1", "select distinct c.Value + 1", [DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator], ["DISTINCT"], "recursive member operator owns the failure", ProviderKind.Basic),
        Invalid("F05", "RECURSIVE_MEMBER_POISON", RecursiveSeed, "select c.Value + 1 from counter c where c.Value < 3", "select c.Value + 1 from counter c group by c.Value", [DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator], ["GROUP BY"], "recursive member grouping is rejected at its operator root", ProviderKind.Basic),
        Invalid("F06", "RECURSIVE_MEMBER_POISON", RecursiveSeed, "select c.Value + 1 from counter c where c.Value < 3", "select RowNumber() over (order by c.Value) from counter c", [DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator], ["WINDOW"], "recursive member window use is rejected at its operator root", ProviderKind.Basic),

        Invalid("G01", "INTERPRETATION_FIELD_POISON", BinaryInterpretationSeed, "p.Value", "p.Missing", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "binary interpretation output exposes one unknown field root", ProviderKind.Binary),
        Invalid("G02", "INTERPRETATION_FIELD_POISON", BinaryInterpretationSeed, "p.Value", "p.Missing.Deep", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "binary interpretation field poisons dependent members", ProviderKind.Binary),
        Invalid("G03", "INTERPRETATION_FIELD_POISON", BinaryInterpretationSeed, "p.Value", "p.Missing::Int32", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "binary interpretation field poisons a dependent cast", ProviderKind.Binary),
        Invalid("G04", "INTERPRETATION_FIELD_POISON", TextInterpretationSeed, "p.Value", "p.Missing", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "text interpretation output exposes one unknown field root", ProviderKind.Text),
        Invalid("G05-v2", "INTERPRETATION_FIELD_POISON", TextInterpretationSeed, "from #test.lines() f cross apply Parse<Record>(f.Text) p", "from #test.lines() f cross apply Parse<Record>(f.Text) p where p.Missing = 'x'", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "text interpretation field root remains visible in WHERE", ProviderKind.Text),
        Invalid("G06-v2", "INTERPRETATION_FIELD_POISON", PartialBinaryInterpretationSeed, "p.ErrorField", "p.Missing, p.ErrorField", [DiagnosticCode.MQ3001_UnknownColumn], ["Missing"], "partial interpretation keeps known fields beside one invalid root", ProviderKind.Binary),

        Invalid("H01", "INDEPENDENT_SIBLINGS", "select a.Name, a.City from #A.Entities() a", "a.Name, a.City", "a.Missing, a.Unknown", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3001_UnknownColumn], ["Missing", "Unknown"], "independent qualified roots retain source order", ProviderKind.Basic),
        Invalid("H02", "INDEPENDENT_SIBLINGS", "select a.Name, a.City from #A.Entities() a", "a.Name, a.City", "missing.Name, a.Unknown", [DiagnosticCode.MQ3015_UnknownAlias, DiagnosticCode.MQ3001_UnknownColumn], ["missing", "Unknown"], "alias and column roots retain distinct roles", ProviderKind.Basic),
        Invalid("H03", "INDEPENDENT_SIBLINGS", "select a.Name, a.City from #A.Entities() a", "a.Name, a.City", "a.Missing.Deep, a.Unknown", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3001_UnknownColumn], ["Missing", "Unknown"], "dependent chain and sibling root remain distinct", ProviderKind.Basic),
        Invalid("H04", "INDEPENDENT_SIBLINGS", "select Sum(a.Population) from #A.Entities() a where a.City = 'x'", "Sum(a.Population) from #A.Entities() a where a.City = 'x'", "Sum(a.Name) from #A.Entities() a where a.Unknown = 1", [DiagnosticCode.MQ3088_NoMatchingCallableOverload, DiagnosticCode.MQ3001_UnknownColumn], ["Sum", "Unknown"], "aggregate root and sibling column remain distinct", ProviderKind.Basic),
        Invalid("H05", "INDEPENDENT_SIBLINGS", "select RowNumber() over (order by a.Name), a.City from #A.Entities() a", "order by a.Name), a.City", "order by a.Missing), a.Unknown", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3001_UnknownColumn], ["Missing", "Unknown"], "window root and sibling column remain distinct", ProviderKind.Basic),
        Invalid("H06", "INDEPENDENT_SIBLINGS", "select a.Name, a.City from #A.Entities() a", "a.Name, a.City", "a.Missing::Int32, a.Unknown", [DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3001_UnknownColumn], ["Missing", "Unknown"], "cast root and sibling column remain distinct", ProviderKind.Basic)
    ];

    private const string RecursiveSeed =
        "with recursive counter (Value) as (" +
        "select seed.Value from values {{ Value: 1 }} seed union all " +
        "select c.Value + 1 from counter c where c.Value < 3) " +
        "select Value from counter";

    private const string BinaryInterpretationSeed =
        "binary Packet { Value: int le }; " +
        "select p.Value from #test.files() f cross apply Interpret<Packet>(f.Content) p";

    private const string TextInterpretationSeed =
        "text Record { Value: rest }; " +
        "select p.Value from #test.lines() f cross apply Parse<Record>(f.Text) p";

    private const string PartialBinaryInterpretationSeed =
        "binary Packet { Value: int le }; " +
        "select p.ErrorField from #test.files() f cross apply PartialInterpret<Packet>(f.Content) p";

    [TestMethod]
    public void IndependentRoots_ShouldHonorFrozenContract()
    {
        Assert.HasCount(48, CandidateCases);

        foreach (var candidate in CandidateCases)
        {
            var seed = Analyze(candidate.SeedQuery, candidate.Provider);
            Assert.IsTrue(seed.IsSuccess, $"{candidate.Id} seed is not valid: {Format(seed)}\n{candidate.SeedQuery}");

            var result = Analyze(candidate.ResultQuery, candidate.Provider);
            var diagnostics = result.Errors.ToArray();
            Assert.IsFalse(result.IsSuccess, $"{candidate.Id} unexpectedly succeeded.");
            CollectionAssert.AreEqual(candidate.ExpectedCodes.ToArray(), diagnostics.Select(static item => item.Code).ToArray(), candidate.Id);
            Assert.HasCount(candidate.ExpectedCodes.Count, diagnostics, $"{candidate.Id}: {Format(result)}");

            for (var index = 0; index < diagnostics.Length; index++)
            {
                var diagnostic = diagnostics[index];
                Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity, candidate.Id);
                Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase, candidate.Id);
                Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind, candidate.Id);
                Assert.IsTrue(diagnostic.Location.IsValid, candidate.Id);
                Assert.IsTrue(diagnostic.EndLocation.IsValid, candidate.Id);
                Assert.IsTrue(diagnostic.Span.Start >= 0 && diagnostic.Span.End <= candidate.ResultQuery.Length, candidate.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet), candidate.Id);
                Assert.IsTrue(
                    diagnostic.Message.Contains(candidate.ExpectedTokens[index], StringComparison.OrdinalIgnoreCase) ||
                    candidate.ResultQuery.Substring(diagnostic.Span.Start, diagnostic.Span.Length)
                        .Contains(candidate.ExpectedTokens[index], StringComparison.OrdinalIgnoreCase),
                    $"{candidate.Id}: token '{candidate.ExpectedTokens[index]}' was not located in '{diagnostic.Message}'.");
            }

            Assert.IsFalse(diagnostics.Any(static item => item.Code is
                DiagnosticCode.MQ8001_CodeGenerationFailed or
                DiagnosticCode.MQ9001_InternalCompilerError or
                DiagnosticCode.MQ9002_InternalExecutionError),
                $"{candidate.Id}: {Format(result)}");
        }
    }

    [TestMethod]
    public void CandidateMatrix_ShouldCoverAllRecoveryFamilies()
    {
        var groups = CandidateCases.GroupBy(static candidate => candidate.Family, StringComparer.Ordinal).ToArray();

        Assert.HasCount(8, groups);
        Assert.IsTrue(groups.All(static group => group.Count() == 6));
        CollectionAssert.AllItemsAreUnique(CandidateCases.Select(static candidate => candidate.Id).ToArray());
        Assert.IsTrue(CandidateCases.All(static candidate => candidate.SeedQuery.Contains($"/* {candidate.Id} */", StringComparison.Ordinal)));
        Assert.IsTrue(CandidateCases.All(static candidate => candidate.ResultQuery.Contains($"/* {candidate.Id} */", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void IndependentRoots_ShouldPreserveSourceOrderAcrossRepeatedAnalysis()
    {
        foreach (var candidate in CandidateCases.Where(static candidate => candidate.ExpectedCodes.Count > 1))
        {
            var first = Analyze(candidate.ResultQuery, candidate.Provider).Errors
                .Select(static item => (Code: item.Code, Offset: item.Location.Offset, EndOffset: item.EndLocation.Offset, Message: item.Message))
                .ToArray();
            var second = Analyze(candidate.ResultQuery, candidate.Provider).Errors
                .Select(static item => (Code: item.Code, Offset: item.Location.Offset, EndOffset: item.EndLocation.Offset, Message: item.Message))
                .ToArray();

            CollectionAssert.AreEqual(first, second, candidate.Id);
            Assert.IsTrue(first.Zip(first.Skip(1), static (left, right) => left.Offset <= right.Offset).All(static value => value), candidate.Id);
        }
    }

    [TestMethod]
    public void RepairTrajectory_ShouldLeaveExactlyTheNextGenuineRoot()
    {
        const string malformed = "select missing.Name, a.Unknown from #A.Entities() a";
        const string repaired = "select a.Name, a.Unknown from #A.Entities() a";

        var malformedDiagnostics = Analyze(malformed, ProviderKind.Basic).Errors.ToArray();
        CollectionAssert.AreEqual(
            new[] { DiagnosticCode.MQ3015_UnknownAlias, DiagnosticCode.MQ3001_UnknownColumn },
            malformedDiagnostics.Select(static item => item.Code).ToArray());

        var repairedDiagnostics = Analyze(repaired, ProviderKind.Basic).Errors.ToArray();
        Assert.HasCount(1, repairedDiagnostics);
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, repairedDiagnostics[0].Code);
        StringAssert.Contains(repairedDiagnostics[0].Message, "Unknown");
        Assert.IsFalse(repairedDiagnostics.Any(static item => item.Code == DiagnosticCode.MQ3015_UnknownAlias));
    }

    private static RecoveryCase Invalid(
        string id,
        string family,
        string seed,
        string before,
        string after,
        IReadOnlyList<DiagnosticCode> expectedCodes,
        IReadOnlyList<string> expectedTokens,
        string rootCause,
        ProviderKind provider)
    {
        var marker = $" /* {id} */";
        return new RecoveryCase(
            id,
            family,
            seed + marker,
            ReplaceOnce(seed, before, after) + marker,
            expectedCodes,
            expectedTokens,
            rootCause,
            provider);
    }

    private static string ReplaceOnce(string source, string before, string after)
    {
        var start = source.IndexOf(before, StringComparison.Ordinal);
        if (start < 0 || source.IndexOf(before, start + before.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException($"Expected exactly one '{before}' occurrence in '{source}'.");

        return source[..start] + after + source[(start + before.Length)..];
    }

    private static QueryAnalysisResult Analyze(string query, ProviderKind provider)
    {
        ISchemaProvider schemaProvider = provider switch
        {
            ProviderKind.Basic => new BasicSchemaProvider<BasicEntity>(CreateBasicSources()),
            ProviderKind.Binary => new MixedSchemaProvider(
                new Dictionary<string, IEnumerable<BinaryEntity>> { ["#test"] = [] },
                new Dictionary<string, IEnumerable<TextEntity>>()),
            ProviderKind.Text => new MixedSchemaProvider(
                new Dictionary<string, IEnumerable<BinaryEntity>>(),
                new Dictionary<string, IEnumerable<TextEntity>> { ["#test"] = [] }),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };

        return new QueryAnalyzer(schemaProvider, compilationOptions: CompilationOptions).Analyze(query);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateBasicSources() =>
        new()
        {
            ["#A"] = [new BasicEntity("WARSAW", "PL", 100) { Id = 1, Money = 10m }],
            ["#B"] = [new BasicEntity("BERLIN", "DE", 200) { Id = 2, Money = 20m }]
        };

    private static string Format(QueryAnalysisResult result) =>
        string.Join(" | ", result.Errors.Select(static item =>
            $"{item.Code} {item.Span}: {item.Message}"));

    private enum ProviderKind
    {
        Basic,
        Binary,
        Text
    }

    private sealed record RecoveryCase(
        string Id,
        string Family,
        string SeedQuery,
        string ResultQuery,
        IReadOnlyList<DiagnosticCode> ExpectedCodes,
        IReadOnlyList<string> ExpectedTokens,
        string RootCause,
        ProviderKind Provider);
}
