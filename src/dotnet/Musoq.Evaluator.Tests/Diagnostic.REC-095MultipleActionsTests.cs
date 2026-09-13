using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Recovery campaign REC-095: compose individual actions only when their
///     original coordinate system is valid, and reject conflicts explicitly.
/// </summary>
[TestClass]
public sealed class DiagnosticREC095MultipleActionsTests
{
    private const string Utf16CoordinateUnit = "utf16-code-units";

    [TestMethod]
    [DynamicData(nameof(RecoveryCases))]
    public void ActionComposition_ShouldApplyIndependentEditsOrRejectConflicts(object candidateData)
    {
        var candidate = (RecoveryCase)candidateData;

        switch (candidate.Family)
        {
            case "non-overlapping":
                AssertIndependentBatch(candidate, replaceFirstWith: null);
                break;
            case "original-coordinate":
                AssertIndependentBatch(candidate, replaceFirstWith: candidate.FirstReplacement);
                break;
            case "overlapping":
                AssertOverlappingBatchRejected(candidate);
                break;
            case "alternative":
                AssertAlternativeBatchRejected(candidate);
                break;
            case "repeated":
                AssertRepeatedActionRejected(candidate);
                break;
            case "reanalyze-each":
                AssertReanalysisRefreshesActionCoordinates(candidate);
                break;
            default:
                Assert.Fail($"Unknown REC-095 family '{candidate.Family}'.");
                break;
        }
    }

    [TestMethod]
    public void CaseMatrix_ShouldCoverAllCompositionBoundaries()
    {
        Assert.HasCount(24, CandidateCases);
        Assert.HasCount(24, CandidateCases.Select(static candidate => candidate.CaseId).Distinct(StringComparer.Ordinal));

        var familyCounts = CandidateCases
            .GroupBy(static candidate => candidate.Family)
            .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(
            new[] { "non-overlapping", "original-coordinate", "overlapping", "alternative", "repeated", "reanalyze-each" },
            familyCounts.Keys.ToArray());
        Assert.IsTrue(familyCounts.Values.All(static count => count == 4));
    }

    [TestMethod]
    public void ExactIndependentBatch_ShouldApplyAgainstOneOriginalCoordinateSystem()
    {
        AssertIndependentBatch(CandidateCases[0], replaceFirstWith: null);
    }

    [TestMethod]
    public void ConflictingActions_ShouldBeRejectedWithoutTryingEitherApplicationOrder()
    {
        var candidate = CandidateCases.Single(static item => item.CaseId == "REC-095-O01");
        var original = Analyze(candidate.Query);
        var nativeEdit = GetSingleNativeEdit(original, candidate.Query, candidate.CaseId);
        var document = DocumentSnapshot.Create("query-1", 1, candidate.Query, Utf16CoordinateUnit);
        var actions = new[]
        {
            new CapturedAction(document, nativeEdit),
            new CapturedAction(document, TextEdit.Replace(nativeEdit.Span, "City"))
        };

        var result = CompositionHarness.ApplyBatch(actions, document);

        Assert.IsFalse(result.Applied);
        Assert.AreEqual(CompositionRejection.Conflict, result.Rejection);
        Assert.IsNull(result.AppliedText);
        Assert.AreEqual(candidate.Query, document.Text);
    }

    [TestMethod]
    public void ReanalysisAfterFirstAction_ShouldRequireAFreshSecondAction()
    {
        var candidate = CandidateCases.Single(static item => item.CaseId == "REC-095-R01");
        var originalResult = Analyze(candidate.Query);
        var originalEdits = GetNativeEdits(originalResult, candidate.Query, candidate.CaseId);
        Assert.HasCount(2, originalEdits);

        var original = DocumentSnapshot.Create("query-1", 1, candidate.Query, Utf16CoordinateUnit);
        var firstAction = new CapturedAction(original, originalEdits[0]);
        var first = CompositionHarness.ApplyBatch([firstAction], original);
        Assert.IsTrue(first.Applied);
        Assert.IsNotNull(first.AppliedText);

        var afterFirst = DocumentSnapshot.Create("query-1", 2, first.AppliedText!, Utf16CoordinateUnit);
        var staleSecond = CompositionHarness.ApplyBatch(
            [new CapturedAction(original, originalEdits[1])],
            afterFirst);
        Assert.IsFalse(staleSecond.Applied);
        Assert.AreEqual(CompositionRejection.StaleIdentity, staleSecond.Rejection);

        var refreshed = Analyze(afterFirst.Text);
        var refreshedEdit = GetSingleNativeEdit(refreshed, afterFirst.Text, candidate.CaseId);
        var final = CompositionHarness.ApplyBatch(
            [new CapturedAction(afterFirst, refreshedEdit)],
            afterFirst);

        Assert.IsTrue(final.Applied);
        Assert.IsTrue(Analyze(final.AppliedText!).IsSuccess, FormatDiagnostics(Analyze(final.AppliedText!)));
    }

    [TestMethod]
    public void NativeDiagnosticActions_ShouldRemainIndividualActions()
    {
        var actionProperties = typeof(DiagnosticAction)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static property => property.Name)
            .ToArray();
        var textEditProperties = typeof(TextEdit)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static property => property.Name)
            .ToArray();

        CollectionAssert.AreEquivalent(new[] { "Title", "Kind", "TextEdit" }, actionProperties);
        CollectionAssert.AreEquivalent(new[] { "Span", "NewText" }, textEditProperties);
        Assert.IsFalse(actionProperties.Contains("Edits", StringComparer.Ordinal));
        Assert.IsFalse(textEditProperties.Contains("Edits", StringComparer.Ordinal));
    }

    public static IEnumerable<object[]> RecoveryCases()
    {
        foreach (var candidate in CandidateCases)
            yield return [candidate];
    }

    private static readonly IReadOnlyList<RecoveryCase> CandidateCases =
    [
        new("REC-095-N01", "non-overlapping", "select Naem, Ctiy from #A.Entities()", null),
        new("REC-095-N02", "non-overlapping", "select Name from #A.Entities() where Naem = 'Warsaw' and Ctiy = 'Warsaw'", null),
        new("REC-095-N03", "non-overlapping", "select Naem from #A.Entities() order by Ctiy", null),
        new("REC-095-N04", "non-overlapping", "select Naem, Name from #A.Entities()\r\nwhere Ctiy = 'Warsaw'", null),

        new("REC-095-D01", "original-coordinate", "select Naem, Ctiy from #A.Entities()", "Country"),
        new("REC-095-D02", "original-coordinate", "select Name from #A.Entities() where Naem = 'Warsaw' and Ctiy = 'Warsaw'", "Country"),
        new("REC-095-D03", "original-coordinate", "select Naem from #A.Entities() order by Ctiy", "Country"),
        new("REC-095-D04", "original-coordinate", "select Naem, Name from #A.Entities()\r\nwhere Ctiy = 'Warsaw'", "Country"),

        new("REC-095-O01", "overlapping", "select Naem from #A.Entities()", null),
        new("REC-095-O02", "overlapping", "select Name from #A.Entities() where Naem = 'Warsaw'", null),
        new("REC-095-O03", "overlapping", "select Naem from #A.Entities() order by Name", null),
        new("REC-095-O04", "overlapping", "select '😀' as Marker from #A.Entities() where Naem = 'Warsaw'", null),

        new("REC-095-X01", "alternative", "select Naem from #A.Entities()", "City"),
        new("REC-095-X02", "alternative", "select Name from #A.Entities() where Naem = 'Warsaw'", "Country"),
        new("REC-095-X03", "alternative", "select Naem from #A.Entities() order by Name", ""),
        new("REC-095-X04", "alternative", "select '😀' as Marker from #A.Entities() where Naem = 'Warsaw'", "City"),

        new("REC-095-P01", "repeated", "select Naem from #A.Entities()", null),
        new("REC-095-P02", "repeated", "select Name from #A.Entities() where Naem = 'Warsaw'", null),
        new("REC-095-P03", "repeated", "select Naem from #A.Entities() order by Name", null),
        new("REC-095-P04", "repeated", "select '😀' as Marker from #A.Entities() where Naem = 'Warsaw'", null),

        new("REC-095-R01", "reanalyze-each", "select Naem, Ctiy from #A.Entities()", null),
        new("REC-095-R02", "reanalyze-each", "select Name from #A.Entities() where Naem = 'Warsaw' and Ctiy = 'Warsaw'", null),
        new("REC-095-R03", "reanalyze-each", "select Naem from #A.Entities() order by Ctiy", null),
        new("REC-095-R04", "reanalyze-each", "select Naem, Name from #A.Entities()\r\nwhere Ctiy = 'Warsaw'", null)
    ];

    private static void AssertIndependentBatch(RecoveryCase candidate, string? replaceFirstWith)
    {
        var analysis = Analyze(candidate.Query);
        var edits = GetNativeEdits(analysis, candidate.Query, candidate.CaseId);
        Assert.HasCount(2, edits, candidate.CaseId);
        Assert.IsFalse(edits[0].Span.Overlaps(edits[1].Span), candidate.CaseId);

        var document = DocumentSnapshot.Create("query-1", 1, candidate.Query, Utf16CoordinateUnit);
        var firstEdit = replaceFirstWith is null
            ? edits[0]
            : TextEdit.Replace(edits[0].Span, replaceFirstWith);
        var actions = new CapturedAction[]
        {
            new(document, firstEdit),
            new(document, edits[1])
        };

        var result = CompositionHarness.ApplyBatch(actions, document);

        Assert.IsTrue(result.Applied, candidate.CaseId);
        Assert.IsNull(result.Rejection, candidate.CaseId);
        Assert.IsNotNull(result.AppliedText, candidate.CaseId);
        var repaired = Analyze(result.AppliedText!);
        Assert.IsTrue(repaired.IsSuccess,
            $"{candidate.CaseId}: independent original-coordinate edits did not repair the query. {FormatDiagnostics(repaired)}");
    }

    private static void AssertOverlappingBatchRejected(RecoveryCase candidate)
    {
        var analysis = Analyze(candidate.Query);
        var nativeEdit = GetSingleNativeEdit(analysis, candidate.Query, candidate.CaseId);
        var document = DocumentSnapshot.Create("query-1", 1, candidate.Query, Utf16CoordinateUnit);
        var overlappingEdit = candidate.CaseId switch
        {
            "REC-095-O01" => TextEdit.Replace(
                new TextSpan(nativeEdit.Span.Start - 1, nativeEdit.Span.Length + 1), "Name"),
            "REC-095-O02" => TextEdit.Replace(
                new TextSpan(nativeEdit.Span.Start, nativeEdit.Span.Length - 1), "Name"),
            "REC-095-O03" => TextEdit.Replace(
                new TextSpan(nativeEdit.Span.Start + 1, nativeEdit.Span.Length - 1), "Name"),
            _ => TextEdit.Replace(
                new TextSpan(nativeEdit.Span.Start - 1, nativeEdit.Span.Length + 2), "Name")
        };

        var result = CompositionHarness.ApplyBatch(
            [new CapturedAction(document, nativeEdit), new CapturedAction(document, overlappingEdit)],
            document);

        Assert.IsFalse(result.Applied, candidate.CaseId);
        Assert.AreEqual(CompositionRejection.Conflict, result.Rejection, candidate.CaseId);
        Assert.IsNull(result.AppliedText, candidate.CaseId);
        Assert.IsFalse(Analyze(document.Text).IsSuccess, candidate.CaseId);
    }

    private static void AssertAlternativeBatchRejected(RecoveryCase candidate)
    {
        var analysis = Analyze(candidate.Query);
        var nativeEdit = GetSingleNativeEdit(analysis, candidate.Query, candidate.CaseId);
        var document = DocumentSnapshot.Create("query-1", 1, candidate.Query, Utf16CoordinateUnit);
        var alternative = TextEdit.Replace(nativeEdit.Span, candidate.FirstReplacement!);

        var result = CompositionHarness.ApplyBatch(
            [new CapturedAction(document, nativeEdit), new CapturedAction(document, alternative)],
            document);

        Assert.IsFalse(result.Applied, candidate.CaseId);
        Assert.AreEqual(CompositionRejection.Conflict, result.Rejection, candidate.CaseId);
        Assert.IsNull(result.AppliedText, candidate.CaseId);
        Assert.AreEqual(candidate.Query, document.Text, candidate.CaseId);
    }

    private static void AssertRepeatedActionRejected(RecoveryCase candidate)
    {
        var original = DocumentSnapshot.Create("query-1", 1, candidate.Query, Utf16CoordinateUnit);
        var nativeEdit = GetSingleNativeEdit(Analyze(candidate.Query), candidate.Query, candidate.CaseId);
        var captured = new CapturedAction(original, nativeEdit);
        var first = CompositionHarness.ApplyBatch([captured], original);

        Assert.IsTrue(first.Applied, candidate.CaseId);
        Assert.IsNotNull(first.AppliedText, candidate.CaseId);
        Assert.IsTrue(Analyze(first.AppliedText!).IsSuccess, candidate.CaseId);

        var currentText = candidate.CaseId switch
        {
            "REC-095-P02" => first.AppliedText! + "  ",
            "REC-095-P03" => ReplaceOnce(first.AppliedText!, "Name", "City"),
            "REC-095-P04" => first.AppliedText!.Replace("Marker", "Marker2", StringComparison.Ordinal),
            _ => first.AppliedText!
        };
        var current = DocumentSnapshot.Create("query-1", 2, currentText, Utf16CoordinateUnit);
        var repeated = CompositionHarness.ApplyBatch([captured], current);

        Assert.IsFalse(repeated.Applied, candidate.CaseId);
        Assert.AreEqual(CompositionRejection.StaleIdentity, repeated.Rejection, candidate.CaseId);
        Assert.IsNull(repeated.AppliedText, candidate.CaseId);
        Assert.AreEqual(current.Text, currentText, candidate.CaseId);
    }

    private static void AssertReanalysisRefreshesActionCoordinates(RecoveryCase candidate)
    {
        var original = DocumentSnapshot.Create("query-1", 1, candidate.Query, Utf16CoordinateUnit);
        var originalEdits = GetNativeEdits(Analyze(candidate.Query), candidate.Query, candidate.CaseId);
        Assert.HasCount(2, originalEdits, candidate.CaseId);

        var first = CompositionHarness.ApplyBatch(
            [new CapturedAction(original, originalEdits[0])],
            original);
        Assert.IsTrue(first.Applied, candidate.CaseId);

        var afterFirst = DocumentSnapshot.Create("query-1", 2, first.AppliedText!, Utf16CoordinateUnit);
        var staleSecond = CompositionHarness.ApplyBatch(
            [new CapturedAction(original, originalEdits[1])],
            afterFirst);
        Assert.IsFalse(staleSecond.Applied, candidate.CaseId);
        Assert.AreEqual(CompositionRejection.StaleIdentity, staleSecond.Rejection, candidate.CaseId);
        Assert.IsNull(staleSecond.AppliedText, candidate.CaseId);

        var refreshed = Analyze(afterFirst.Text);
        var refreshedEdits = GetNativeEdits(refreshed, afterFirst.Text, candidate.CaseId);
        Assert.HasCount(1, refreshedEdits, candidate.CaseId);
        var final = CompositionHarness.ApplyBatch(
            [new CapturedAction(afterFirst, refreshedEdits[0])],
            afterFirst);

        Assert.IsTrue(final.Applied, candidate.CaseId);
        Assert.IsTrue(Analyze(final.AppliedText!).IsSuccess,
            $"{candidate.CaseId}: fresh action did not repair current text. {FormatDiagnostics(Analyze(final.AppliedText!))}");
    }

    private static TextEdit GetSingleNativeEdit(QueryAnalysisResult result, string sourceText, string caseId)
    {
        var edits = GetNativeEdits(result, sourceText, caseId);
        Assert.HasCount(1, edits, caseId);
        return edits[0];
    }

    private static IReadOnlyList<TextEdit> GetNativeEdits(
        QueryAnalysisResult result,
        string sourceText,
        string caseId)
    {
        Assert.IsFalse(result.IsSuccess, caseId);
        var edits = result.Errors
            .SelectMany(static diagnostic => diagnostic.SuggestedFixes)
            .Where(static action => action.TextEdit != null)
            .Select(static action => action.TextEdit!)
            .OrderBy(static edit => edit.Span.Start)
            .ToArray();
        Assert.IsTrue(edits.All(edit => edit.Span.Start >= 0 && edit.Span.End <= sourceText.Length),
            $"{caseId}: native edit span was not bounded.");
        return edits;
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(CreateBasicSources())).Analyze(query);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateBasicSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("Warsaw", "PL", 100)],
            ["#B"] = [new BasicEntity("Berlin", "DE", 200)]
        };
    }

    private static string ApplyEdit(string text, TextEdit edit)
    {
        return text[..edit.Span.Start] + edit.NewText + text[edit.Span.End..];
    }

    private static string ReplaceOnce(string text, string oldText, string newText)
    {
        var index = text.IndexOf(oldText, StringComparison.Ordinal);
        if (index < 0)
            throw new InvalidOperationException($"Expected '{oldText}' in composition text.");

        return text[..index] + newText + text[(index + oldText.Length)..];
    }

    private static string ComputeDigest(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"[{diagnostic.Code}] {diagnostic.Message}"));
    }

    private enum CompositionRejection
    {
        Conflict,
        StaleIdentity
    }

    private sealed record RecoveryCase(string CaseId, string Family, string Query, string? FirstReplacement);

    private sealed record DocumentSnapshot(
        string DocumentId,
        int Version,
        string Text,
        string CoordinateUnit,
        string TextDigest)
    {
        public static DocumentSnapshot Create(string documentId, int version, string text, string coordinateUnit)
        {
            return new DocumentSnapshot(documentId, version, text, coordinateUnit, ComputeDigest(text));
        }
    }

    private sealed record CapturedAction(DocumentSnapshot OriginalDocument, TextEdit Edit);

    private sealed record BatchOutcome(
        bool Applied,
        string? AppliedText,
        CompositionRejection? Rejection);

    private static class CompositionHarness
    {
        public static BatchOutcome ApplyBatch(
            IReadOnlyList<CapturedAction> actions,
            DocumentSnapshot current)
        {
            if (actions.Count == 0)
                throw new ArgumentException("At least one action is required.", nameof(actions));

            var original = actions[0].OriginalDocument;
            if (actions.Any(action => !SameIdentity(original, action.OriginalDocument)) ||
                !SameIdentity(original, current))
            {
                return new BatchOutcome(false, null, CompositionRejection.StaleIdentity);
            }

            var ordered = actions
                .Select(static action => action.Edit)
                .OrderBy(static edit => edit.Span.Start)
                .ToArray();
            if (ordered.Any(edit => edit.Span.Start < 0 || edit.Span.End > current.Text.Length))
                return new BatchOutcome(false, null, CompositionRejection.Conflict);

            for (var index = 1; index < ordered.Length; index++)
            {
                if (ordered[index - 1].Span.Overlaps(ordered[index].Span))
                    return new BatchOutcome(false, null, CompositionRejection.Conflict);
            }

            var text = current.Text;
            for (var index = ordered.Length - 1; index >= 0; index--)
                text = ApplyEdit(text, ordered[index]);

            return new BatchOutcome(true, text, null);
        }

        private static bool SameIdentity(DocumentSnapshot left, DocumentSnapshot right)
        {
            return string.Equals(left.DocumentId, right.DocumentId, StringComparison.Ordinal) &&
                   left.Version == right.Version &&
                   string.Equals(left.CoordinateUnit, right.CoordinateUnit, StringComparison.Ordinal) &&
                   string.Equals(left.TextDigest, right.TextDigest, StringComparison.Ordinal);
        }
    }
}
