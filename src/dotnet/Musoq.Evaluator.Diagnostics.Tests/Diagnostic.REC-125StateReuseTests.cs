using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC125StateReuseTests
{
    private static readonly IReadOnlyList<AnalyzerSequence> ValidInvalidValidCases =
    [
        new("A01", "select a.Name from #A.Entities() a", "select a.Missing from #A.Entities() a", DiagnosticCode.MQ3001_UnknownColumn),
        new("A02", "select a.City from #A.Entities() a where a.Name = 'WARSAW'", "select a.Ctiy from #A.Entities() a where a.Name = 'WARSAW'", DiagnosticCode.MQ3001_UnknownColumn),
        new("A03", "select Count(*) from #A.Entities() a group by a.City", "select Count(*) from #A.Entities() a group by a.Ctiy", DiagnosticCode.MQ3001_UnknownColumn),
        new("A04", "select a.Name from #A.Entities() a order by a.City", "select a.Name from #A.Entities() a order by a.Ctiy", DiagnosticCode.MQ3001_UnknownColumn),
        new("A05", "select a.Name from #A.Entities() a where a.City = 'WARSAW'", "select b.Name from #A.Entities() a where a.City = 'WARSAW'", DiagnosticCode.MQ3015_UnknownAlias),
        new("A06", "select a.Name from #A.Entities() a", "select Substrng(a.Name, 0, 2) from #A.Entities() a", DiagnosticCode.MQ3086_UnknownCallable)
    ];

    private static readonly IReadOnlyList<InvalidSequence> InvalidAInvalidBCases =
    [
        new("B01", "select a.Missing from #A.Entities() a", DiagnosticCode.MQ3001_UnknownColumn, "select a.Unknown from #A.Entities() a", DiagnosticCode.MQ3001_UnknownColumn),
        new("B02", "select b.Name from #A.Entities() a", DiagnosticCode.MQ3015_UnknownAlias, "select Substrng(a.Name, 0, 2) from #A.Entities() a", DiagnosticCode.MQ3086_UnknownCallable),
        new("B03", "select a.Name from #A.Entities() a where a.Missing = 'WARSAW'", DiagnosticCode.MQ3001_UnknownColumn, "select b.Name from #A.Entities() a", DiagnosticCode.MQ3015_UnknownAlias),
        new("B04", "select a.Name from #A.Entities() a order by a.Missing", DiagnosticCode.MQ3001_UnknownColumn, "select a.Missing from #A.Entities() a", DiagnosticCode.MQ3001_UnknownColumn),
        new("B05", "select Length(a.Missing) from #A.Entities() a", DiagnosticCode.MQ3001_UnknownColumn, "select UnknownCall(a.Name) from #A.Entities() a", DiagnosticCode.MQ3086_UnknownCallable),
        new("B06", "with p as (select a.Name from #A.Entities() a) select p.Missing from p", DiagnosticCode.MQ3001_UnknownColumn, "select d.Unknown from (select a.Name from #A.Entities() a) d", DiagnosticCode.MQ3001_UnknownColumn)
    ];

    private static readonly IReadOnlyList<SchemaFixtureCase> EqualTextSchemaCases =
    [
        new("C01", "select a.Name from #A.Entities() a", "select a.Missing from #A.Entities() a", DiagnosticCode.MQ3001_UnknownColumn),
        new("C02", "select a.City from #A.Entities() a", "select a.Ctiy from #A.Entities() a", DiagnosticCode.MQ3001_UnknownColumn),
        new("C03", "select a.Country from #A.Entities() a", "select b.Country from #A.Entities() a", DiagnosticCode.MQ3015_UnknownAlias),
        new("C04", "select a.Population from #A.Entities() a", "select Substrng(a.Name, 0, 2) from #A.Entities() a", DiagnosticCode.MQ3086_UnknownCallable)
    ];

    private static readonly IReadOnlyList<string> DiagnosticLimitCaseIds = ["D01", "D02", "D03", "D04"];
    private static readonly IReadOnlyList<string> PayloadCaseIds = ["E01", "E02", "E03", "E04"];

    [TestMethod]
    public void CandidateMatrix_ShouldCoverFrozenContract()
    {
        var ids = ValidInvalidValidCases.Select(static candidate => candidate.Id)
            .Concat(InvalidAInvalidBCases.Select(static candidate => candidate.Id))
            .Concat(EqualTextSchemaCases.Select(static candidate => candidate.Id))
            .Concat(DiagnosticLimitCaseIds)
            .Concat(PayloadCaseIds)
            .ToArray();

        Assert.HasCount(24, ids);
        Assert.HasCount(24, ids.Distinct(StringComparer.Ordinal));
        Assert.IsTrue(ids.Take(6).All(static id => id.StartsWith("A", StringComparison.Ordinal)));
        Assert.IsTrue(ids.Skip(6).Take(6).All(static id => id.StartsWith("B", StringComparison.Ordinal)));
        Assert.IsTrue(ids.Skip(12).Take(4).All(static id => id.StartsWith("C", StringComparison.Ordinal)));
        Assert.IsTrue(ids.Skip(16).Take(4).All(static id => id.StartsWith("D", StringComparison.Ordinal)));
        Assert.IsTrue(ids.Skip(20).Take(4).All(static id => id.StartsWith("E", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ReusableAnalyzer_ShouldHonorValidInvalidValidSequences()
    {
        foreach (var candidate in ValidInvalidValidCases)
        {
            var analyzer = CreateAnalyzer();
            Assert.IsTrue(analyzer.Analyze(candidate.ValidQuery).IsSuccess, candidate.Id);
            AssertSingleDiagnostic(analyzer.Analyze(candidate.InvalidQuery), candidate.ExpectedCode, candidate.Id);
            Assert.IsTrue(analyzer.Analyze(candidate.ValidQuery).IsSuccess, candidate.Id);
            AssertSingleDiagnostic(analyzer.Analyze(candidate.InvalidQuery), candidate.ExpectedCode, candidate.Id);
        }
    }

    [TestMethod]
    public void ReusableAnalyzer_ShouldReplaceInvalidRootsAcrossDistinctFaults()
    {
        foreach (var candidate in InvalidAInvalidBCases)
        {
            var analyzer = CreateAnalyzer();
            AssertSingleDiagnostic(analyzer.Analyze(candidate.InvalidA), candidate.ExpectedCodeA, candidate.Id + "/A");
            AssertSingleDiagnostic(analyzer.Analyze(candidate.InvalidB), candidate.ExpectedCodeB, candidate.Id + "/B");
            AssertSingleDiagnostic(analyzer.Analyze(candidate.InvalidA), candidate.ExpectedCodeA, candidate.Id + "/A-repeat");
        }
    }

    [TestMethod]
    public void EqualQueryTextAcrossSchemaFixtures_ShouldBeDeterministic()
    {
        foreach (var candidate in EqualTextSchemaCases)
        {
            var firstCandidateAnalyzer = CreateAnalyzer(new BasicSchemaProvider<BasicEntity>(CreateSources("first", "WARSAW")));
            var secondCandidateAnalyzer = CreateAnalyzer(new BasicSchemaProvider<BasicEntity>(CreateSources("second", "BERLIN")));
            var firstCandidate = Describe(firstCandidateAnalyzer.Analyze(candidate.InvalidQuery));
            var secondCandidate = Describe(secondCandidateAnalyzer.Analyze(candidate.InvalidQuery));
            Assert.AreEqual(firstCandidate, secondCandidate, candidate.Id);
            StringAssert.Contains(firstCandidate, candidate.ExpectedCode.ToString(), candidate.Id);
        }

        const string query = "select a.Missing, a.Unknown from #A.Entities() a";
        var signatures = new List<string>();
        foreach (var fixture in new[] { ("first", "WARSAW"), ("second", "BERLIN") })
        {
            var analyzer = CreateAnalyzer(new BasicSchemaProvider<BasicEntity>(CreateSources(fixture.Item1, fixture.Item2)));
            for (var iteration = 0; iteration < 6; iteration++)
                signatures.Add(Describe(analyzer.Analyze(query)));
        }

        Assert.IsNotEmpty(signatures[0]);
        Assert.IsTrue(signatures.All(signature => signature == signatures[0]), string.Join(Environment.NewLine, signatures));
        StringAssert.Contains(signatures[0], nameof(DiagnosticCode.MQ3001_UnknownColumn));
    }

    [TestMethod]
    public void DiagnosticLimits_ShouldRetainEarliestUsefulRoot()
    {
        var bag = new DiagnosticBag { MaxErrors = 1 };
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "root-A", new TextSpan(0, 1));
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "root-B", new TextSpan(1, 1));
        bag.AddWarning(DiagnosticCode.MQ5003_ImplicitTypeConversion, "warning-after-limit", new TextSpan(2, 1));
        Assert.AreEqual(1, bag.ErrorCount);
        Assert.AreEqual(1, bag.WarningCount);
        Assert.AreEqual("root-A", bag.ToSortedList().Single(diagnostic => diagnostic.IsError).Message);

        var context = new DiagnosticContext(new SourceText("abcdef"), maxErrors: 2);
        context.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "root-1", new TextSpan(0, 1));
        context.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "root-2", new TextSpan(2, 1));
        context.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "root-3", new TextSpan(4, 1));
        Assert.HasCount(2, context.Errors);
        CollectionAssert.AreEqual(new[] { "root-1", "root-2" }, context.Diagnostics.Select(diagnostic => diagnostic.Message).ToArray());

        var duplicateBag = new DiagnosticBag { MaxErrors = 2 };
        duplicateBag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "same-root", new TextSpan(0, 1));
        duplicateBag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "same-root", new TextSpan(0, 1));
        duplicateBag.AddError(DiagnosticCode.MQ3015_UnknownAlias, "independent-root", new TextSpan(3, 1));
        Assert.AreEqual(2, duplicateBag.ErrorCount);
        CollectionAssert.AreEqual(new[] { "same-root", "independent-root" }, duplicateBag.ToSortedList().Select(diagnostic => diagnostic.Message).ToArray());

        var reusableBag = new DiagnosticBag { MaxErrors = 1 };
        reusableBag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "stale-root", new TextSpan(0, 1));
        reusableBag.Clear();
        Assert.AreEqual(0, reusableBag.Count);
        Assert.AreEqual(0, reusableBag.ErrorCount);
        reusableBag.AddError(DiagnosticCode.MQ3015_UnknownAlias, "fresh-root", new TextSpan(4, 1));
        Assert.AreEqual("fresh-root", reusableBag.ToSortedList().Single().Message);
        Assert.HasCount(4, DiagnosticLimitCaseIds);
    }

    [TestMethod]
    public void StructuredPayloadCopies_ShouldSurviveFormatting()
    {
        var related = new DiagnosticRelatedLocation(
            new SourceLocation(12, 2, 3),
            new SourceLocation(15, 2, 6),
            "Declaration is here",
            DiagnosticSourceKind.Schema);
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ9001_InternalCompilerError,
            DiagnosticSeverity.Error,
            "Invariant failed",
            new SourceLocation(0, 1, 1),
            new SourceLocation(3, 1, 4),
            phase: DiagnosticPhase.Internal,
            sourceKind: DiagnosticSourceKind.GeneratedSource,
            arguments: new Dictionary<string, string> { ["symbol"] = "Name", ["actualTypes"] = "Int32" },
            relatedLocations: [related],
            correlationId: "rec-125")
            .WithRelatedInfo("Origin retained")
            .WithSuggestedFix(DiagnosticAction.QuickFix("Use the declared name", new TextSpan(0, 3), "Name"))
            .WithExplanation("The generated expression could not be lowered.")
            .WithDocsReference("Core Spec - Diagnostics")
            .WithArgument("phaseOwner", "renderer")
            .WithRelatedLocation(new DiagnosticRelatedLocation(new SourceLocation(20, 3, 1), message: "SQL origin"));

        var copy = diagnostic.WithLocations(new SourceLocation(2, 1, 3), new SourceLocation(5, 1, 6));
        Assert.AreEqual(DiagnosticPhase.Internal, copy.Phase);
        Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, copy.SourceKind);
        Assert.AreEqual("Name", copy.Arguments["symbol"]);
        Assert.AreEqual("renderer", copy.Arguments["phaseOwner"]);
        Assert.AreEqual("Declaration is here", copy.RelatedLocations[0].Message);
        Assert.AreEqual(DiagnosticSourceKind.Schema, copy.RelatedLocations[0].SourceKind);
        Assert.AreEqual("rec-125", copy.CorrelationId);
        Assert.HasCount(2, copy.RelatedLocations);
        Assert.HasCount(1, copy.SuggestedFixes);
        Assert.HasCount(1, copy.RelatedInfo);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(copy);
        var json = MusoqErrorEnvelopeFormatter.FormatJson(envelope);
        using var parsed = JsonDocument.Parse(json);
        Assert.AreEqual("MQ9001", parsed.RootElement.GetProperty("code").GetString());
        Assert.AreEqual("generated-source", parsed.RootElement.GetProperty("source").GetString());
        Assert.AreEqual(2, parsed.RootElement.GetProperty("related").GetArrayLength());
        StringAssert.Contains(MusoqErrorEnvelopeFormatter.FormatText(envelope), "Related:");
        StringAssert.Contains(MusoqErrorEnvelopeFormatter.FormatText(envelope), "Actions:");

        foreach (var caseId in PayloadCaseIds)
        {
            var repeated = copy.WithArgument("caseId", caseId);
            var repeatedEnvelope = MusoqErrorEnvelope.FromDiagnostic(repeated);
            var repeatedJson = MusoqErrorEnvelopeFormatter.FormatJson(repeatedEnvelope);
            using var repeatedDocument = JsonDocument.Parse(repeatedJson);
            Assert.AreEqual("MQ9001", repeatedDocument.RootElement.GetProperty("code").GetString(), caseId);
            Assert.AreEqual(2, repeatedDocument.RootElement.GetProperty("related").GetArrayLength(), caseId);
            StringAssert.Contains(repeatedJson, caseId, caseId);
        }

        Assert.HasCount(4, PayloadCaseIds);
    }

    private static void AssertSingleDiagnostic(QueryAnalysisResult result, DiagnosticCode expectedCode, string context)
    {
        Assert.IsTrue(result.IsParsed, context);
        var diagnostics = result.Errors.ToArray();
        Assert.HasCount(1, diagnostics, context);
        Assert.AreEqual(expectedCode, diagnostics[0].Code, context);
        Assert.AreEqual(DiagnosticPhaseMapping.FromCode(expectedCode), diagnostics[0].Phase, context);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostics[0].SourceKind, context);
        Assert.IsTrue(diagnostics[0].Location.IsValid, context);
        Assert.IsTrue(diagnostics[0].EndLocation.IsValid, context);
    }

    private static QueryAnalyzer CreateAnalyzer(ISchemaProvider? provider = null) =>
        new(provider ?? new BasicSchemaProvider<BasicEntity>(CreateSources("default", "WARSAW")));

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources(string name, string city) =>
        new()
        {
            ["#A"] = [new BasicEntity(city, "PL", 100) { Name = name }],
            ["#B"] = [new BasicEntity("BERLIN", "DE", 200) { Name = "other" }]
        };

    private static string Describe(QueryAnalysisResult result) =>
        string.Join(";", result.Diagnostics.Select(static diagnostic =>
            $"{diagnostic.Code}:{diagnostic.Phase}:{diagnostic.SourceKind}:{diagnostic.Span.Start}:{diagnostic.Span.Length}:{diagnostic.Message}"));

    private sealed record AnalyzerSequence(string Id, string ValidQuery, string InvalidQuery, DiagnosticCode ExpectedCode);

    private sealed record InvalidSequence(
        string Id,
        string InvalidA,
        DiagnosticCode ExpectedCodeA,
        string InvalidB,
        DiagnosticCode ExpectedCodeB);

    private sealed record SchemaFixtureCase(string Id, string ValidQuery, string InvalidQuery, DiagnosticCode ExpectedCode);
}
