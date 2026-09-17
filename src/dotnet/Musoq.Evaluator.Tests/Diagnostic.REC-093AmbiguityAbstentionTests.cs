using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Recovery campaign REC-093: uncertainty must produce bounded facts and
///     abstention rather than an arbitrary repair.
/// </summary>
[TestClass]
public sealed class DiagnosticREC093AmbiguityAbstentionTests
{
    [TestMethod]
    [DynamicData(nameof(RecoveryCases))]
    public void AmbiguousAndUncertainReferences_ShouldAbstainOrOfferOnlyUniqueEdits(object candidateData)
    {
        var candidate = (RecoveryCase)candidateData;
        var result = Analyze(candidate, candidate.Query);
        var diagnostics = result.Errors.ToArray();

        Assert.HasCount(1, diagnostics,
            $"{candidate.CaseId}: expected one root diagnostic, got " +
            string.Join(" | ", diagnostics.Select(static diagnostic => $"[{diagnostic.Code}] {diagnostic.Message}")));

        var diagnostic = diagnostics[0];
        Assert.AreEqual(candidate.ExpectedCode, diagnostic.Code, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedPhase, diagnostic.Phase, candidate.CaseId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation), candidate.CaseId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference), candidate.CaseId);
        Assert.IsTrue(diagnostic.Message.Contains(candidate.MessageFragment, StringComparison.OrdinalIgnoreCase),
            $"{candidate.CaseId}: expected message fragment '{candidate.MessageFragment}', got '{diagnostic.Message}'.");

        AssertCandidateFacts(candidate, diagnostic);

        var textEdits = diagnostic.SuggestedFixes
            .Where(static action => action.TextEdit != null)
            .Select(static action => action.TextEdit!)
            .ToArray();

        if (candidate.ExpectedReplacement is null)
        {
            Assert.IsEmpty(textEdits,
                $"{candidate.CaseId}: an underdetermined or unsupported reference received a guaranteed edit.");
            return;
        }

        Assert.HasCount(1, textEdits,
            $"{candidate.CaseId}: expected one unique spelling edit, got {textEdits.Length}.");
        var edit = textEdits[0];
        Assert.AreEqual(candidate.ExpectedReplacement, edit.NewText, candidate.CaseId);
        Assert.IsTrue(edit.Span.Start >= 0 && edit.Span.End <= candidate.Query.Length, candidate.CaseId);
        Assert.AreEqual(candidate.FaultText,
            candidate.Query.Substring(edit.Span.Start, edit.Span.Length),
            $"{candidate.CaseId}: edit did not target only the faulty occurrence.");

        var repairedQuery = ApplyEdit(candidate.Query, edit);
        var repaired = Analyze(candidate, repairedQuery);
        Assert.IsTrue(repaired.IsSuccess,
            $"{candidate.CaseId}: unique repair did not produce a valid query. {FormatDiagnostics(repaired)}");
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainTwelveCasesPerAbstentionFamily()
    {
        Assert.HasCount(48, CandidateCases);
        Assert.HasCount(48, CandidateCases.Select(static candidate => candidate.CaseId).Distinct(StringComparer.Ordinal));
        Assert.IsTrue(CandidateCases.All(static candidate => !string.IsNullOrWhiteSpace(candidate.Query)));

        var familyCounts = CandidateCases
            .GroupBy(static candidate => candidate.Family)
            .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(
            new[]
            {
                "unique-justified-spelling",
                "equally-plausible-visible",
                "out-of-scope",
                "missing-or-unsupported"
            },
            familyCounts.Keys.ToArray());
        Assert.IsTrue(familyCounts.Values.All(static count => count == 12));
    }

    [TestMethod]
    public void ForeignProviderNames_ShouldNotEnterBasicCandidateSets()
    {
        var foreignAnalyzer = new QueryAnalyzer(new RecoverySchemaProvider(RecoveryProviderKind.Foreign));
        foreach (var sourceName in new[] { "Rows", "Events", "Lookup" })
        {
            var valid = foreignAnalyzer.Analyze($"select Nami from #foreign.{sourceName}()");
            Assert.IsTrue(valid.IsSuccess,
                $"Foreign provider source '{sourceName}' should expose its registered name. {FormatDiagnostics(valid)}");
        }

        var foreignCallable = foreignAnalyzer.Analyze("select ZZQAlpha(Name) from #foreign.Rows()");
        Assert.IsTrue(foreignCallable.IsSuccess, FormatDiagnostics(foreignCallable));

        var basicColumn = AnalyzeSemantic("select Nami from #A.Entities()", RecoveryProviderKind.Basic);
        var columnDiagnostic = basicColumn.Errors.Single();
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, columnDiagnostic.Code);
        Assert.AreEqual("Name", columnDiagnostic.Arguments["candidateColumns"]);
        Assert.IsFalse(columnDiagnostic.Arguments["candidateColumns"].Contains("Nami", StringComparison.Ordinal));

        var basicCallable = AnalyzeSemantic("select ZZQAlpa(Name) from #A.Entities()", RecoveryProviderKind.Basic);
        var callableDiagnostic = basicCallable.Errors.Single();
        Assert.AreEqual(DiagnosticCode.MQ3086_UnknownCallable, callableDiagnostic.Code);
        Assert.IsFalse(callableDiagnostic.Arguments.GetValueOrDefault("candidateCallables", "")
            .Contains("ZZQAlpha", StringComparison.Ordinal));
        Assert.IsFalse(callableDiagnostic.Arguments.GetValueOrDefault("candidateCallables", "")
            .Contains("ZZQAlpah", StringComparison.Ordinal));
        Assert.IsEmpty(callableDiagnostic.SuggestedFixes.Where(static action => action.TextEdit != null));
    }

    [TestMethod]
    public void AmbiguousCandidateLists_ShouldBeBoundedAndStable()
    {
        const string query = "select Nme from #ambiguous.Rows()";
        var analyzer = new QueryAnalyzer(new RecoverySchemaProvider(RecoveryProviderKind.Ambiguous));
        var first = analyzer.Analyze(query);
        var second = analyzer.Analyze(query);
        var firstDiagnostic = first.Errors.Single();
        var secondDiagnostic = second.Errors.Single();

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, firstDiagnostic.Code);
        Assert.AreEqual("Name, Nime", firstDiagnostic.Arguments["candidateColumns"]);
        Assert.AreEqual(firstDiagnostic.Arguments["candidateColumns"], secondDiagnostic.Arguments["candidateColumns"]);
        Assert.IsEmpty(firstDiagnostic.SuggestedFixes.Where(static action => action.TextEdit != null));
        Assert.IsTrue(firstDiagnostic.Arguments["candidateColumns"]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length <= 5);
    }

    [TestMethod]
    public void MissingMetadata_ShouldRetainExplanationWithoutInventingAnEdit()
    {
        var result = AnalyzeSemantic("select Name from #missing.Rows()", RecoveryProviderKind.MissingMetadata);
        var diagnostic = result.Errors.Single();

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.IsFalse(diagnostic.Arguments.GetValueOrDefault("candidateColumns", "")
            .Contains("Name", StringComparison.Ordinal));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsEmpty(diagnostic.SuggestedFixes.Where(static action => action.TextEdit != null));
    }

    [TestMethod]
    public void UnsupportedSyntax_ShouldRemainAParseRootCauseWithoutAnEdit()
    {
        const string query = "select @ from #system.dual()";
        var result = new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(CreateBasicSources())).ValidateSyntax(query);
        var diagnostic = result.Errors.Single();

        Assert.AreEqual(DiagnosticCode.MQ1001_UnknownToken, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsEmpty(diagnostic.SuggestedFixes.Where(static action => action.TextEdit != null));
    }

    public static IEnumerable<object[]> RecoveryCases()
    {
        foreach (var candidate in CandidateCases)
            yield return [candidate];
    }

    private static readonly IReadOnlyList<RecoveryCase> CandidateCases =
    [
        // One close spelling candidate is a justified edit. Each symbol is exercised in three clauses.
        new("REC-093-U01", "unique-justified-spelling", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Naem from #A.Entities()", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "Name", "Name", "Naem", "Naem"),
        new("REC-093-U02", "unique-justified-spelling", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() where Naem = 'Warsaw'", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "Name", "Name", "Naem", "Naem"),
        new("REC-093-U03", "unique-justified-spelling", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() order by Naem", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "Name", "Name", "Naem", "Naem"),
        new("REC-093-U04", "unique-justified-spelling", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select peple.Name from #A.Entities() people", DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind,
            "availableAliases", "people", "people", "peple", "peple"),
        new("REC-093-U05", "unique-justified-spelling", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() people where peple.Name = 'Warsaw'", DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind,
            "availableAliases", "people", "people", "peple", "peple"),
        new("REC-093-U06", "unique-justified-spelling", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() people order by peple.Name", DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind,
            "availableAliases", "people", "people", "peple", "peple"),
        new("REC-093-U07", "unique-justified-spelling", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Self.Naem from #A.Entities()", DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind,
            "candidateProperties", "Name", "Name", "Naem", "Naem"),
        new("REC-093-U08", "unique-justified-spelling", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() where Self.Naem = 'Warsaw'", DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind,
            "candidateProperties", "Name", "Name", "Naem", "Naem"),
        new("REC-093-U09", "unique-justified-spelling", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() order by Self.Naem", DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind,
            "candidateProperties", "Name", "Name", "Naem", "Naem"),
        new("REC-093-U10", "unique-justified-spelling", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Substrng(Name, 0, 2) from #A.Entities()", DiagnosticCode.MQ3086_UnknownCallable, DiagnosticPhase.Bind,
            "candidateCallables", "Substring", "Substring", "Substrng", "Substrng"),
        new("REC-093-U11", "unique-justified-spelling", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() where Substrng(Name, 0, 2) = 'Wa'", DiagnosticCode.MQ3086_UnknownCallable, DiagnosticPhase.Bind,
            "candidateCallables", "Substring", "Substring", "Substrng", "Substrng"),
        new("REC-093-U12", "unique-justified-spelling", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() order by Substrng(Name, 0, 2)", DiagnosticCode.MQ3086_UnknownCallable, DiagnosticPhase.Bind,
            "candidateCallables", "Substring", "Substring", "Substrng", "Substrng"),

        // Tied spellings are visible in the same scope. They are guidance, never automatic edits.
        new("REC-093-A01", "equally-plausible-visible", RecoveryBoundary.Semantic, RecoveryProviderKind.Ambiguous,
            "select Nme from #ambiguous.Rows()", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "Name, Nime", null, "Nme", "Nme"),
        new("REC-093-A02", "equally-plausible-visible", RecoveryBoundary.Semantic, RecoveryProviderKind.Ambiguous,
            "select Name from #ambiguous.Rows() where Nme = 'left'", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "Name, Nime", null, "Nme", "Nme"),
        new("REC-093-A03", "equally-plausible-visible", RecoveryBoundary.Semantic, RecoveryProviderKind.Ambiguous,
            "select Name from #ambiguous.Rows() order by Nme", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "Name, Nime", null, "Nme", "Nme"),
        new("REC-093-A04", "equally-plausible-visible", RecoveryBoundary.Semantic, RecoveryProviderKind.Ambiguous,
            "select let.Name from #ambiguous.Rows() left inner join #ambiguous.Rows() lest on left.Name = lest.Name",
            DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "availableAliases", "left, lest", null, "let", "let"),
        new("REC-093-A05", "equally-plausible-visible", RecoveryBoundary.Semantic, RecoveryProviderKind.Ambiguous,
            "select left.Name from #ambiguous.Rows() left inner join #ambiguous.Rows() lest on left.Name = lest.Name where let.Name = 'left'",
            DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "availableAliases", "left, lest", null, "let", "let"),
        new("REC-093-A06", "equally-plausible-visible", RecoveryBoundary.Semantic, RecoveryProviderKind.Ambiguous,
            "select left.Name from #ambiguous.Rows() left inner join #ambiguous.Rows() lest on left.Name = lest.Name order by let.Name",
            DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "availableAliases", "left, lest", null, "let", "let"),
        new("REC-093-A07", "equally-plausible-visible", RecoveryBoundary.Semantic, RecoveryProviderKind.Ambiguous,
            "select ZZQAlpa(Name) from #ambiguous.Rows()", DiagnosticCode.MQ3086_UnknownCallable, DiagnosticPhase.Bind,
            "candidateCallables", "ZZQAlpah, ZZQAlpha", null, "ZZQAlpa", "ZZQAlpa"),
        new("REC-093-A08", "equally-plausible-visible", RecoveryBoundary.Semantic, RecoveryProviderKind.Ambiguous,
            "select Name from #ambiguous.Rows() where ZZQAlpa(Name) = 'left'", DiagnosticCode.MQ3086_UnknownCallable, DiagnosticPhase.Bind,
            "candidateCallables", "ZZQAlpah, ZZQAlpha", null, "ZZQAlpa", "ZZQAlpa"),
        new("REC-093-A09", "equally-plausible-visible", RecoveryBoundary.Semantic, RecoveryProviderKind.Ambiguous,
            "select ZZQAlpa(Name) from #ambiguous.Rows() order by ZZQAlpa(Name)", DiagnosticCode.MQ3086_UnknownCallable, DiagnosticPhase.Bind,
            "candidateCallables", "ZZQAlpah, ZZQAlpha", null, "ZZQAlpa", "ZZQAlpa"),
        new("REC-093-A10", "equally-plausible-visible", RecoveryBoundary.Semantic, RecoveryProviderKind.Ambiguous,
            "select Self.Nme from #ambiguous.Rows()", DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind,
            "candidateProperties", "Name, Nime", null, "Nme", "Nme"),
        new("REC-093-A11", "equally-plausible-visible", RecoveryBoundary.Semantic, RecoveryProviderKind.Ambiguous,
            "select Name from #ambiguous.Rows() where Self.Nme = 'left'", DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind,
            "candidateProperties", "Name, Nime", null, "Nme", "Nme"),
        new("REC-093-A12", "equally-plausible-visible", RecoveryBoundary.Semantic, RecoveryProviderKind.Ambiguous,
            "select Name from #ambiguous.Rows() order by Self.Nme", DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind,
            "candidateProperties", "Name, Nime", null, "Nme", "Nme"),

        // Names from another provider and unrelated names must not leak into this scope.
        new("REC-093-O01", "out-of-scope", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Nami from #A.Entities()", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "Name", "Name", "Nami", "Nami"),
        new("REC-093-O02", "out-of-scope", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() where Nami = 'Warsaw'", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "Name", "Name", "Nami", "Nami"),
        new("REC-093-O03", "out-of-scope", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() order by Nami", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "Name", "Name", "Nami", "Nami"),
        new("REC-093-O04", "out-of-scope", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select ZZQAlpa(Name) from #A.Entities()", DiagnosticCode.MQ3086_UnknownCallable, DiagnosticPhase.Bind,
            "candidateCallables", "", null, "ZZQAlpa", "ZZQAlpa"),
        new("REC-093-O05", "out-of-scope", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() where ZZQAlpa(Name) = 'Warsaw'", DiagnosticCode.MQ3086_UnknownCallable, DiagnosticPhase.Bind,
            "candidateCallables", "", null, "ZZQAlpa", "ZZQAlpa"),
        new("REC-093-O06", "out-of-scope", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() order by ZZQAlpa(Name)", DiagnosticCode.MQ3086_UnknownCallable, DiagnosticPhase.Bind,
            "candidateCallables", "", null, "ZZQAlpa", "ZZQAlpa"),
        new("REC-093-O07", "out-of-scope", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Rows()", DiagnosticCode.MQ3085_UnknownSource, DiagnosticPhase.Bind,
            null, null, null, "Rows", "Rows"),
        new("REC-093-O08", "out-of-scope", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Events()", DiagnosticCode.MQ3085_UnknownSource, DiagnosticPhase.Bind,
            null, null, null, "Events", "Events"),
        new("REC-093-O09", "out-of-scope", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Lookup()", DiagnosticCode.MQ3085_UnknownSource, DiagnosticPhase.Bind,
            null, null, null, "Lookup", "Lookup"),
        new("REC-093-O10", "out-of-scope", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select zzzz.Name from #A.Entities() people", DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind,
            "availableAliases", "people", null, "zzzz", "zzzz"),
        new("REC-093-O11", "out-of-scope", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() people where zzzz.Name = 'Warsaw'", DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind,
            "availableAliases", "people", null, "zzzz", "zzzz"),
        new("REC-093-O12", "out-of-scope", RecoveryBoundary.Semantic, RecoveryProviderKind.Basic,
            "select Name from #A.Entities() people order by zzzz.Name", DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind,
            "availableAliases", "people", null, "zzzz", "zzzz"),

        // Empty metadata and genuinely unsupported syntax have different root phases and codes.
        new("REC-093-M01", "missing-or-unsupported", RecoveryBoundary.Semantic, RecoveryProviderKind.MissingMetadata,
            "select Name from #missing.Rows()", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "", null, "Name", "Name"),
        new("REC-093-M02", "missing-or-unsupported", RecoveryBoundary.Semantic, RecoveryProviderKind.MissingMetadata,
            "select 1 from #missing.Rows() where Name = 'x'", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "", null, "Name", "Name"),
        new("REC-093-M03", "missing-or-unsupported", RecoveryBoundary.Semantic, RecoveryProviderKind.MissingMetadata,
            "select 1 from #missing.Rows() order by Name", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "", null, "Name", "Name"),
        new("REC-093-M04", "missing-or-unsupported", RecoveryBoundary.Semantic, RecoveryProviderKind.MissingMetadata,
            "select 1 from #missing.Rows() group by Name", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "", null, "Name", "Name"),
        new("REC-093-M05", "missing-or-unsupported", RecoveryBoundary.Semantic, RecoveryProviderKind.MissingMetadata,
            "select ZZQAlpha(Name) from #missing.Rows()", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "", null, "Name", "Name"),
        new("REC-093-M06", "missing-or-unsupported", RecoveryBoundary.Semantic, RecoveryProviderKind.MissingMetadata,
            "select 1 from #missing.Rows() where Name is null", DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind,
            "candidateColumns", "", null, "Name", "Name"),
        new("REC-093-M07", "missing-or-unsupported", RecoveryBoundary.Syntax, RecoveryProviderKind.Basic,
            "select @ from #system.dual()", DiagnosticCode.MQ1001_UnknownToken, DiagnosticPhase.Parse,
            null, null, null, "@", "@"),
        new("REC-093-M08", "missing-or-unsupported", RecoveryBoundary.Syntax, RecoveryProviderKind.Basic,
            "select 1 from #system.dual() where @", DiagnosticCode.MQ1001_UnknownToken, DiagnosticPhase.Parse,
            null, null, null, "@", "@"),
        new("REC-093-M09", "missing-or-unsupported", RecoveryBoundary.Syntax, RecoveryProviderKind.Basic,
            "select 1 from #system.dual() order by @", DiagnosticCode.MQ1001_UnknownToken, DiagnosticPhase.Parse,
            null, null, null, "@", "@"),
        new("REC-093-M10", "missing-or-unsupported", RecoveryBoundary.Syntax, RecoveryProviderKind.Basic,
            "select ^ from #system.dual()", DiagnosticCode.MQ2020_MissingOperand, DiagnosticPhase.Parse,
            null, null, null, "^", "binary operator"),
        new("REC-093-M11", "missing-or-unsupported", RecoveryBoundary.Syntax, RecoveryProviderKind.Basic,
            "select 1 from #system.dual() where ^", DiagnosticCode.MQ2020_MissingOperand, DiagnosticPhase.Parse,
            null, null, null, "^", "binary operator"),
        new("REC-093-M12", "missing-or-unsupported", RecoveryBoundary.Syntax, RecoveryProviderKind.Basic,
            "select 1 from #system.dual() order by ^", DiagnosticCode.MQ2020_MissingOperand, DiagnosticPhase.Parse,
            null, null, null, "^", "binary operator")
    ];

    private static QueryAnalysisResult Analyze(RecoveryCase candidate, string query)
    {
        return candidate.Boundary == RecoveryBoundary.Syntax
            ? new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(CreateBasicSources())).ValidateSyntax(query)
            : AnalyzeSemantic(query, candidate.Provider);
    }

    private static QueryAnalysisResult AnalyzeSemantic(string query, RecoveryProviderKind providerKind)
    {
        ISchemaProvider provider = providerKind == RecoveryProviderKind.Basic
            ? new BasicSchemaProvider<BasicEntity>(CreateBasicSources())
            : new RecoverySchemaProvider(providerKind);
        return new QueryAnalyzer(provider).Analyze(query);
    }

    private static void AssertCandidateFacts(RecoveryCase candidate, Diagnostic diagnostic)
    {
        if (candidate.CandidateArgument is null)
            return;

        var actual = diagnostic.Arguments.GetValueOrDefault(candidate.CandidateArgument, string.Empty);
        Assert.AreEqual(candidate.ExpectedCandidates, actual, candidate.CaseId);

        if (candidate.CandidateArgument is "candidateColumns" or "candidateProperties" or "candidateCallables")
        {
            var count = actual.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length;
            Assert.IsTrue(count <= 5, $"{candidate.CaseId}: candidate list exceeded the bound.");
        }
    }

    private static string ApplyEdit(string query, TextEdit edit)
    {
        return query[..edit.Span.Start] + edit.NewText + query[edit.Span.End..];
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"[{diagnostic.Code}] {diagnostic.Message}"));
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateBasicSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("Warsaw", "PL", 100)],
            ["#B"] = [new BasicEntity("Berlin", "DE", 200)]
        };
    }

    private enum RecoveryBoundary
    {
        Semantic,
        Syntax
    }

    private enum RecoveryProviderKind
    {
        Basic,
        Ambiguous,
        MissingMetadata,
        Foreign
    }

    private sealed record RecoveryCase(
        string CaseId,
        string Family,
        RecoveryBoundary Boundary,
        RecoveryProviderKind Provider,
        string Query,
        DiagnosticCode ExpectedCode,
        DiagnosticPhase ExpectedPhase,
        string? CandidateArgument,
        string? ExpectedCandidates,
        string? ExpectedReplacement,
        string FaultText,
        string MessageFragment);

    private sealed class RecoverySchemaProvider(RecoveryProviderKind providerKind) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new RecoverySchema(CreateProfile(providerKind));
        }
    }

    private sealed class RecoverySchema(RecoveryProfile profile)
        : SchemaBase(profile.SchemaName, CreateMethods())
    {
        private readonly RecoveryTable _table = new(profile.Columns);

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (!profile.SourceNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                throw new SchemaNotFoundException();

            return _table;
        }

        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext)
        {
            return profile.SourceNames
                .Select(static source => new SchemaMethodInfo(source, ConstructorInfo.Empty()))
                .ToArray();
        }

        public override SchemaMethodInfo[] GetRawConstructors(
            string methodName,
            SourceMetadataContext metadataContext)
        {
            return profile.SourceNames
                .Where(source => source.Equals(methodName, StringComparison.OrdinalIgnoreCase))
                .Select(static source => new SchemaMethodInfo(source, ConstructorInfo.Empty()))
                .ToArray();
        }

        private static MethodsAggregator CreateMethods()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new RecoveryLibrary());
            return new MethodsAggregator(manager);
        }
    }

    private sealed class RecoveryTable(IReadOnlyList<(string Name, Type Type)> columns) : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = columns
            .Select((column, index) => (ISchemaColumn)new SchemaColumn(column.Name, index, column.Type))
            .ToArray();

        public SchemaTableMetadata Metadata { get; } = new(typeof(IReadOnlyDictionary<string, object?>));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column =>
                column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns
                .Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }

    private sealed class RecoveryLibrary : LibraryBase
    {
        [BindableMethod]
        public string ZZQAlpha(string value) => value;

        [BindableMethod]
        public string ZZQAlpah(string value) => value;
    }

    private sealed class AmbiguousPropertyRow
    {
        public string Name { get; set; } = string.Empty;

        public string Nime { get; set; } = string.Empty;
    }

    private sealed record RecoveryProfile(
        string SchemaName,
        IReadOnlyList<string> SourceNames,
        IReadOnlyList<(string Name, Type Type)> Columns);

    private static RecoveryProfile CreateProfile(RecoveryProviderKind providerKind)
    {
        return providerKind switch
        {
            RecoveryProviderKind.Ambiguous => new RecoveryProfile(
                "ambiguous",
                ["Rows"],
                [
                    ("Name", typeof(string)),
                    ("Nime", typeof(string)),
                    ("Self", typeof(AmbiguousPropertyRow))
                ]),
            RecoveryProviderKind.MissingMetadata => new RecoveryProfile(
                "missing",
                ["Rows"],
                []),
            RecoveryProviderKind.Foreign => new RecoveryProfile(
                "foreign",
                ["Rows", "Events", "Lookup"],
                [
                    ("Name", typeof(string)),
                    ("Nami", typeof(string)),
                    ("Self", typeof(AmbiguousPropertyRow))
                ]),
            _ => throw new ArgumentOutOfRangeException(nameof(providerKind), providerKind, null)
        };
    }
}
