using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Recovery campaign REC-094: a native text edit is coordinate-only; the
///     document identity and stale-action policy belong to the consumer harness.
/// </summary>
[TestClass]
public sealed class DiagnosticREC094DocumentIdentityTests
{
    private const string Utf16CoordinateUnit = "utf16-code-units";

    [TestMethod]
    [DynamicData(nameof(RecoveryCases))]
    public void StaleActions_ShouldBeRejectedAndCurrentTextReanalyzed(object candidateData)
    {
        var candidate = (RecoveryCase)candidateData;
        var original = Analyze(candidate.OriginalQuery);
        var originalDiagnostic = original.Errors.Single();

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, originalDiagnostic.Code, candidate.CaseId);
        Assert.AreEqual(DiagnosticPhase.Bind, originalDiagnostic.Phase, candidate.CaseId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(originalDiagnostic.Explanation), candidate.CaseId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(originalDiagnostic.DocsReference), candidate.CaseId);

        var textEdits = originalDiagnostic.SuggestedFixes
            .Where(static action => action.TextEdit != null)
            .Select(static action => action.TextEdit!)
            .ToArray();
        Assert.HasCount(1, textEdits, candidate.CaseId);

        var edit = textEdits[0];
        Assert.AreEqual("Name", edit.NewText, candidate.CaseId);
        Assert.AreEqual(candidate.OriginalQuery.IndexOf("Naem", StringComparison.Ordinal), edit.Span.Start,
            $"{candidate.CaseId}: native edit did not use UTF-16 string coordinates.");
        Assert.AreEqual(4, edit.Span.Length, candidate.CaseId);
        Assert.AreEqual("Naem", candidate.OriginalQuery.Substring(edit.Span.Start, edit.Span.Length), candidate.CaseId);
        Assert.AreEqual(edit.Span.Start, originalDiagnostic.Location.Offset, candidate.CaseId);

        var originalDocument = DocumentSnapshot.Create(
            candidate.OriginalDocumentId,
            candidate.OriginalVersion,
            candidate.OriginalQuery,
            candidate.OriginalCoordinateUnit);
        var currentDocument = DocumentSnapshot.Create(
            candidate.CurrentDocumentId,
            candidate.CurrentVersion,
            candidate.CurrentQuery,
            candidate.CurrentCoordinateUnit);
        var capturedAction = new CapturedAction(originalDocument, edit);

        var outcome = DocumentRecoveryHarness.ApplyOrReanalyze(capturedAction, currentDocument);

        Assert.IsFalse(outcome.Applied, candidate.CaseId);
        Assert.IsTrue(outcome.Reanalyzed, candidate.CaseId);
        Assert.IsNull(outcome.AppliedText, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedRejection, outcome.Rejection, candidate.CaseId);

        var naiveOffsetOnlyResult = Analyze(ApplyEdit(candidate.CurrentQuery, edit));
        if (!candidate.CurrentQueryShouldAnalyze)
        {
            Assert.AreNotEqual(candidate.CurrentQuery, ApplyEdit(candidate.CurrentQuery, edit), candidate.CaseId);
        }
        Assert.IsNotNull(outcome.Reanalysis, candidate.CaseId);

        if (candidate.CurrentQueryShouldAnalyze)
        {
            Assert.IsTrue(outcome.Reanalysis!.IsSuccess,
                $"{candidate.CaseId}: current text should analyze after reanalysis. {FormatDiagnostics(outcome.Reanalysis)}");
            Assert.IsTrue(naiveOffsetOnlyResult.IsSuccess,
                $"{candidate.CaseId}: the offset-only comparison must demonstrate why identity checking is required.");
        }
        else
        {
            Assert.IsFalse(outcome.Reanalysis!.IsSuccess, candidate.CaseId);
            var currentDiagnostic = outcome.Reanalysis.Errors.Single();
            Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, currentDiagnostic.Code, candidate.CaseId);
            Assert.AreEqual(DiagnosticPhase.Bind, currentDiagnostic.Phase, candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(currentDiagnostic.Explanation), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(currentDiagnostic.DocsReference), candidate.CaseId);
        }
    }

    [TestMethod]
    public void CaseMatrix_ShouldCoverEveryRequiredStaleMutationBoundary()
    {
        Assert.HasCount(24, StaleActionCases);
        Assert.HasCount(24, StaleActionCases.Select(static candidate => candidate.CaseId).Distinct(StringComparer.Ordinal));

        var familyCounts = StaleActionCases
            .GroupBy(static candidate => candidate.Family)
            .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(
            new[] { "before-error", "within-error", "after-error", "wrapper-identity", "unicode-and-lines", "same-length-boundary" },
            familyCounts.Keys.ToArray());
        Assert.IsTrue(familyCounts.Values.All(static count => count == 4));
        Assert.IsTrue(StaleActionCases.All(static candidate => candidate.OriginalCoordinateUnit == Utf16CoordinateUnit));
        Assert.IsTrue(StaleActionCases.All(static candidate => candidate.OriginalQuery.Contains("Naem", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void MatchingDocumentIdentity_ShouldApplyNativeEditAndThenAnalyze()
    {
        var document = DocumentSnapshot.Create("query-1", 7, BaseQuery, Utf16CoordinateUnit);
        var diagnostic = Analyze(BaseQuery).Errors.Single();
        var edit = diagnostic.SuggestedFixes.Single(static action => action.TextEdit != null).TextEdit!;

        var outcome = DocumentRecoveryHarness.ApplyOrReanalyze(
            new CapturedAction(document, edit),
            document);

        Assert.IsTrue(outcome.Applied);
        Assert.IsFalse(outcome.Reanalyzed);
        Assert.IsNull(outcome.Rejection);
        Assert.IsNotNull(outcome.AppliedText);
        Assert.IsTrue(Analyze(outcome.AppliedText!).IsSuccess, FormatDiagnostics(Analyze(outcome.AppliedText!)));
    }

    [TestMethod]
    public void SameLengthChangedText_ShouldNotBeAcceptedByAnOffsetOnlyConsumer()
    {
        var original = DocumentSnapshot.Create("query-1", 7, BaseQuery, Utf16CoordinateUnit);
        var currentText = ReplaceOnce(BaseQuery, "Warsaw", "Berlin");
        var current = DocumentSnapshot.Create("query-1", 7, currentText, Utf16CoordinateUnit);
        var edit = Analyze(BaseQuery).Errors.Single().SuggestedFixes
            .Single(static action => action.TextEdit != null).TextEdit!;

        Assert.AreEqual(original.Text.Length, current.Text.Length);
        Assert.AreNotEqual(original.TextDigest, current.TextDigest);
        var outcome = DocumentRecoveryHarness.ApplyOrReanalyze(new CapturedAction(original, edit), current);

        Assert.IsFalse(outcome.Applied);
        Assert.AreEqual(StaleRejection.TextDigest, outcome.Rejection);
        Assert.IsTrue(outcome.Reanalyzed);
        Assert.IsFalse(outcome.Reanalysis!.IsSuccess);
        Assert.IsTrue(Analyze(ApplyEdit(current.Text, edit)).IsSuccess,
            "The stale edit would silently turn same-length changed text into a valid query.");
    }

    [TestMethod]
    public void SameTextDifferentWrapperVersion_ShouldBeRejectedByTheHarness()
    {
        var original = DocumentSnapshot.Create("query-1", 7, BaseQuery, Utf16CoordinateUnit);
        var current = DocumentSnapshot.Create("query-1", 8, BaseQuery, Utf16CoordinateUnit);
        var edit = Analyze(BaseQuery).Errors.Single().SuggestedFixes
            .Single(static action => action.TextEdit != null).TextEdit!;

        Assert.AreEqual(original.TextDigest, current.TextDigest);
        var outcome = DocumentRecoveryHarness.ApplyOrReanalyze(new CapturedAction(original, edit), current);

        Assert.IsFalse(outcome.Applied);
        Assert.AreEqual(StaleRejection.DocumentVersion, outcome.Rejection);
        Assert.IsTrue(outcome.Reanalyzed);
        Assert.IsFalse(outcome.Reanalysis!.IsSuccess);
    }

    [TestMethod]
    public void NativeActions_ShouldKeepIdentityAndCoordinateMetadataAtTheHarnessBoundary()
    {
        var textEditProperties = typeof(TextEdit)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static property => property.Name)
            .ToArray();
        var actionProperties = typeof(DiagnosticAction)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static property => property.Name)
            .ToArray();

        CollectionAssert.AreEquivalent(new[] { "Span", "NewText" }, textEditProperties);
        CollectionAssert.AreEquivalent(new[] { "Title", "Kind", "TextEdit" }, actionProperties);
        Assert.IsFalse(textEditProperties.Contains("DocumentId", StringComparer.Ordinal));
        Assert.IsFalse(textEditProperties.Contains("DocumentVersion", StringComparer.Ordinal));
        Assert.IsFalse(textEditProperties.Contains("TextDigest", StringComparer.Ordinal));
        Assert.IsFalse(actionProperties.Contains("DocumentVersion", StringComparer.Ordinal));
        Assert.IsFalse(actionProperties.Contains("CoordinateUnit", StringComparer.Ordinal));
    }

    public static IEnumerable<object[]> RecoveryCases()
    {
        foreach (var candidate in StaleActionCases)
            yield return [candidate];
    }

    private static readonly IReadOnlyList<RecoveryCase> StaleActionCases =
    [
        new("REC-094-B01", "before-error", BaseQuery, "  " + BaseQuery,
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-B02", "before-error", BaseQuery, ReplaceOnce(BaseQuery, "select", "SELECT"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-B03", "before-error", BaseQuery, "\r\n" + BaseQuery,
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-B04", "before-error", MarkerQuery, ReplaceOnce(MarkerQuery, "'😀'", "'🦄'"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),

        new("REC-094-W01", "within-error", BaseQuery, ReplaceOnce(BaseQuery, "Naem", "Nime"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-W02", "within-error", BaseQuery, ReplaceOnce(BaseQuery, "Naem", "Name"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, true),
        new("REC-094-W03", "within-error", BaseQuery, ReplaceOnce(BaseQuery, "Naem", "N"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-W04", "within-error", MarkerQuery, ReplaceOnce(MarkerQuery, "Naem", "Naeem"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),

        new("REC-094-A01", "after-error", BaseQuery, BaseQuery + "  ",
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-A02", "after-error", BaseQuery, BaseQuery + "\r\n",
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-A03", "after-error", BaseQuery, ReplaceOnce(BaseQuery, "Warsaw", "Berlin"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-A04", "after-error", CrLfQuery, ReplaceOnce(CrLfQuery, "Warsaw", "Krakow"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),

        new("REC-094-I01", "wrapper-identity", BaseQuery, BaseQuery,
            "query-1", "query-1", 7, 8, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.DocumentVersion, false),
        new("REC-094-I02", "wrapper-identity", BaseQuery, BaseQuery,
            "query-1", "query-2", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.DocumentId, false),
        new("REC-094-I03", "wrapper-identity", BaseQuery, BaseQuery,
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, "utf8-bytes",
            StaleRejection.CoordinateUnit, false),
        new("REC-094-I04", "wrapper-identity", MarkerQuery, MarkerQuery,
            "query-1", "query-2", 7, 8, Utf16CoordinateUnit, "utf8-bytes",
            StaleRejection.DocumentId, false),

        new("REC-094-C01", "unicode-and-lines", MarkerQuery,
            ReplaceOnce(MarkerQuery, "from #A.Entities()", "from  #A.Entities()"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-C02", "unicode-and-lines", MarkerQuery, ReplaceOnce(MarkerQuery, "Naem", "Name"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, true),
        new("REC-094-C03", "unicode-and-lines", LfQuery, ReplaceOnce(LfQuery, "\nwhere", "\n  where"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-C04", "unicode-and-lines", CrLfQuery,
            ReplaceOnce(CrLfQuery, "\r\nwhere", "\r\n  where"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),

        new("REC-094-D01", "same-length-boundary", BaseQuery,
            ReplaceOnce(BaseQuery, "select Name", "select City"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-D02", "same-length-boundary", BaseQuery,
            ReplaceOnce(BaseQuery, "Naem = ", "Naem  = "),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-D03", "same-length-boundary", BaseQuery,
            ReplaceOnce(BaseQuery, "#A.Entities()", "#B.Entities()"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false),
        new("REC-094-D04", "same-length-boundary", BaseQuery,
            ReplaceOnce(BaseQuery, "Warsaw", "London"),
            "query-1", "query-1", 7, 7, Utf16CoordinateUnit, Utf16CoordinateUnit,
            StaleRejection.TextDigest, false)
    ];

    private const string BaseQuery = "select Name from #A.Entities() where Naem = 'Warsaw'";
    private const string MarkerQuery = "select '😀' as Marker from #A.Entities() where Naem = 'Warsaw'";
    private const string LfQuery = "select Name from #A.Entities()\nwhere Naem = 'Warsaw'";
    private const string CrLfQuery = "select Name from #A.Entities()\r\nwhere Naem = 'Warsaw'";

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(CreateBasicSources())).Analyze(query);
    }

    private static string ApplyEdit(string query, TextEdit edit)
    {
        return query[..edit.Span.Start] + edit.NewText + query[edit.Span.End..];
    }

    private static string ReplaceOnce(string text, string oldText, string newText)
    {
        var index = text.IndexOf(oldText, StringComparison.Ordinal);
        if (index < 0)
            throw new InvalidOperationException($"Expected '{oldText}' in frozen recovery case text.");

        return text[..index] + newText + text[(index + oldText.Length)..];
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

    private static string ComputeDigest(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private enum StaleRejection
    {
        None,
        DocumentId,
        DocumentVersion,
        TextDigest,
        CoordinateUnit
    }

    private sealed record RecoveryCase(
        string CaseId,
        string Family,
        string OriginalQuery,
        string CurrentQuery,
        string OriginalDocumentId,
        string CurrentDocumentId,
        int OriginalVersion,
        int CurrentVersion,
        string OriginalCoordinateUnit,
        string CurrentCoordinateUnit,
        StaleRejection ExpectedRejection,
        bool CurrentQueryShouldAnalyze);

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

    private sealed record RecoveryOutcome(
        bool Applied,
        bool Reanalyzed,
        string? AppliedText,
        QueryAnalysisResult? Reanalysis,
        StaleRejection? Rejection);

    private static class DocumentRecoveryHarness
    {
        public static RecoveryOutcome ApplyOrReanalyze(CapturedAction action, DocumentSnapshot current)
        {
            var rejection = GetRejection(action.OriginalDocument, current);
            if (rejection is not null)
            {
                return new RecoveryOutcome(
                    Applied: false,
                    Reanalyzed: true,
                    AppliedText: null,
                    Reanalysis: Analyze(current.Text),
                    Rejection: rejection);
            }

            var appliedText = ApplyEdit(current.Text, action.Edit);
            return new RecoveryOutcome(
                Applied: true,
                Reanalyzed: false,
                AppliedText: appliedText,
                Reanalysis: null,
                Rejection: null);
        }

        private static StaleRejection? GetRejection(DocumentSnapshot original, DocumentSnapshot current)
        {
            if (!string.Equals(original.DocumentId, current.DocumentId, StringComparison.Ordinal))
                return StaleRejection.DocumentId;
            if (original.Version != current.Version)
                return StaleRejection.DocumentVersion;
            if (!string.Equals(original.TextDigest, current.TextDigest, StringComparison.Ordinal))
                return StaleRejection.TextDigest;
            if (!string.Equals(original.CoordinateUnit, current.CoordinateUnit, StringComparison.Ordinal))
                return StaleRejection.CoordinateUnit;

            return null;
        }
    }
}
