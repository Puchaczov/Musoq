using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

/// <summary>
///     Recovery campaign REC-096: correction candidates retain their exact
///     structured values and source coordinates across Unicode, culture, and
///     formatting boundaries.
/// </summary>
[TestClass]
public sealed class DiagnosticREC096UnicodeCultureRenderingTests
{
    [TestMethod]
    [DynamicData(nameof(RecoveryCases))]
    public void CorrectionRendering_ShouldPreserveUnicodeValuesAndCoordinates(object candidateData)
    {
        var candidate = (RecoveryCase)candidateData;
        var cultures = candidate.Family == "culture"
            ? CultureFixtures
            : [CultureInfo.InvariantCulture];

        string? baseline = null;
        foreach (var culture in cultures)
        {
            var priorCulture = CultureInfo.CurrentCulture;
            var priorUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var matches = ErrorCatalog.GetDidYouMeanCandidates(
                    candidate.Input,
                    candidate.Candidates,
                    maxDistance: 3,
                    maxCandidates: 5);
                CollectionAssert.AreEqual(candidate.ExpectedCandidates.ToArray(), matches.ToArray(), candidate.CaseId);

                var query = candidate.SourcePrefix + candidate.Input + candidate.SourceSuffix;
                var span = new TextSpan(candidate.SourcePrefix.Length, candidate.Input.Length);
                var source = new SourceText(query, "REC-096.sql");
                var locations = source.GetLocations(span);

                Assert.AreEqual(candidate.ExpectedLine, locations.Start.Line, candidate.CaseId);
                Assert.AreEqual(candidate.ExpectedSourceColumn, locations.Start.Column, candidate.CaseId);
                Assert.AreEqual(candidate.Input, source.GetText(span), candidate.CaseId);

                var renderedValue = candidate.ExpectedCandidates.Count == 1
                    ? candidate.ExpectedCandidates[0]
                    : null;
                var diagnostic = Diagnostic
                    .Error(DiagnosticCode.MQ3001_UnknownColumn, "REC-096 correction", span)
                    .WithSourceContext(source, span);
                if (renderedValue != null)
                {
                    var action = DiagnosticAction.QuickFix(
                        $"Replace '{candidate.Input}' with '{renderedValue}'",
                        span,
                        renderedValue);
                    diagnostic = diagnostic.WithSuggestedFix(action);
                }

                Assert.AreEqual(span, diagnostic.Span, candidate.CaseId);
                if (renderedValue != null)
                    Assert.AreEqual(renderedValue, diagnostic.SuggestedFixes.Single().TextEdit!.NewText,
                        candidate.CaseId);
                else
                    Assert.IsEmpty(diagnostic.SuggestedFixes, candidate.CaseId);
                Assert.AreEqual(candidate.ExpectedTerminalColumn,
                    GetTerminalColumn(source.GetLineText(locations.Start.Line), locations.Start.Column - 1),
                    candidate.CaseId);

                if (renderedValue != null)
                {
                    var action = diagnostic.SuggestedFixes.Single().TextEdit!;
                    var repaired = ApplyEdit(query, action);
                    Assert.AreEqual(candidate.SourcePrefix + renderedValue + candidate.SourceSuffix,
                        repaired,
                        candidate.CaseId);
                }

                var renderingFingerprint = string.Join(
                    "|",
                    matches,
                    renderedValue == null ? "no-automatic-edit" : span.Start.ToString(CultureInfo.InvariantCulture),
                    renderedValue ?? string.Empty,
                    locations.Start.Line,
                    locations.Start.Column,
                    GetTerminalColumn(source.GetLineText(locations.Start.Line), locations.Start.Column - 1));
                baseline ??= renderingFingerprint;
                if (candidate.Family == "culture")
                    Assert.AreEqual(baseline, renderingFingerprint, candidate.CaseId);
            }
            finally
            {
                CultureInfo.CurrentCulture = priorCulture;
                CultureInfo.CurrentUICulture = priorUiCulture;
            }
        }
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainFourCasesPerUnicodeBoundary()
    {
        Assert.HasCount(24, CandidateCases);
        Assert.HasCount(24, CandidateCases.Select(static candidate => candidate.CaseId).Distinct(StringComparer.Ordinal));

        var familyCounts = CandidateCases
            .GroupBy(static candidate => candidate.Family)
            .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(
            new[] { "supplementary", "combining", "case-and-homograph", "culture", "formatting", "quoted-and-control" },
            familyCounts.Keys.ToArray());
        Assert.IsTrue(familyCounts.Values.All(static count => count == 4));
        Assert.IsTrue(CandidateCases.All(static candidate => candidate.ExpectedCandidates.Count > 0));
    }

    [TestMethod]
    public void CultureFixtures_ShouldBeInvariantPolishAndTurkishAndRestoreCallerCulture()
    {
        var priorCulture = CultureInfo.CurrentCulture;
        var priorUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CollectionAssert.AreEqual(
                new[] { "", "pl-PL", "tr-TR" },
                CultureFixtures.Select(static culture => culture.Name).ToArray());

            foreach (var culture in CultureFixtures)
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                Assert.AreEqual(culture.Name, CultureInfo.CurrentCulture.Name);
                Assert.AreEqual(culture.Name, CultureInfo.CurrentUICulture.Name);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUiCulture;
        }

        Assert.AreEqual(priorCulture.Name, CultureInfo.CurrentCulture.Name);
        Assert.AreEqual(priorUiCulture.Name, CultureInfo.CurrentUICulture.Name);
    }

    [TestMethod]
    public void SourceText_ShouldSeparateUtf16ColumnsFromExpandedTerminalColumns()
    {
        var source = new SourceText("a\tselect 1\r\n\tNaem\r\nnext", "REC-096.sql");
        var offset = source.Text.IndexOf("Naem", StringComparison.Ordinal);
        var locations = source.GetLocations(new TextSpan(offset, 4));

        Assert.AreEqual(2, locations.Start.Line);
        Assert.AreEqual(2, locations.Start.Column);
        Assert.AreEqual(5, GetTerminalColumn(source.GetLineText(2), 1));
        Assert.AreEqual("Naem", source.GetText(new TextSpan(offset, 4)));
    }

    [TestMethod]
    public void StructuredCorrectionAction_ShouldKeepNativePayloadSmallAndExact()
    {
        var actionProperties = typeof(DiagnosticAction)
            .GetProperties()
            .Select(static property => property.Name)
            .ToArray();
        var textEditProperties = typeof(TextEdit)
            .GetProperties()
            .Select(static property => property.Name)
            .ToArray();

        CollectionAssert.AreEquivalent(new[] { "Title", "Kind", "TextEdit" }, actionProperties);
        CollectionAssert.AreEquivalent(new[] { "Span", "NewText" }, textEditProperties);

        var value = "line\n\tquote'Name";
        var edit = new TextEdit(new TextSpan(3, 2), value);
        var action = DiagnosticAction.QuickFix("preserve", edit.Span, edit.NewText);

        Assert.AreEqual(value, action.TextEdit!.NewText);
        Assert.AreEqual(edit.Span, action.TextEdit.Span);
        Assert.IsFalse(actionProperties.Contains("NormalizedText", StringComparer.Ordinal));
        Assert.IsFalse(textEditProperties.Contains("DisplayText", StringComparer.Ordinal));
    }

    [TestMethod]
    public void CaseSensitiveAndHomographCandidates_ShouldRemainDistinctAndOrdered()
    {
        var candidates = new[] { "Name", "name", "Nаme" };
        var matches = ErrorCatalog.GetDidYouMeanCandidates("Nme", candidates);

        CollectionAssert.AreEqual(new[] { "Name", "Nаme", "name" }, matches.ToArray());
        Assert.AreNotEqual(candidates[0], candidates[1]);
        Assert.AreNotEqual(candidates[0], candidates[2]);
        Assert.AreNotEqual(candidates[1], candidates[2]);
    }

    public static IEnumerable<object[]> RecoveryCases()
    {
        foreach (var candidate in CandidateCases)
            yield return [candidate];
    }

    private static readonly IReadOnlyList<CultureInfo> CultureFixtures =
    [
        CultureInfo.InvariantCulture,
        CultureInfo.GetCultureInfo("pl-PL"),
        CultureInfo.GetCultureInfo("tr-TR")
    ];

    private static readonly IReadOnlyList<RecoveryCase> CandidateCases =
    [
        new("REC-096-S01", "supplementary", "𐐀Nmae", ["𐐀Name"], ["𐐀Name"], "select ", " from #source.Rows()", 1, 8, 8),
        new("REC-096-S02", "supplementary", "😀Valu", ["😀Value"], ["😀Value"], "value=", ";", 1, 7, 7),
        new("REC-096-S03", "supplementary", "𝄞Scor", ["𝄞Score"], ["𝄞Score"], "marker: ", "", 1, 9, 9),
        new("REC-096-S04", "supplementary", "𐑂Ctiy", ["𐑂City"], ["𐑂City"], "select ", "", 1, 8, 8),

        new("REC-096-C01", "combining", "Caf\u0301", ["Cafe\u0301"], ["Cafe\u0301"], "value=", "", 1, 7, 7),
        new("REC-096-C02", "combining", "n\u0301me", ["na\u0301me"], ["na\u0301me"], "value=", "", 1, 7, 7),
        new("REC-096-C03", "combining", "re\u0301sume", ["re\u0301sume\u0301"], ["re\u0301sume\u0301"], "value=", "", 1, 7, 7),
        new("REC-096-C04", "combining", "A\u0308ngstr\u0308m", ["A\u0308ngstro\u0308m"], ["A\u0308ngstro\u0308m"], "value=", "", 1, 7, 7),

        new("REC-096-H01", "case-and-homograph", "Nme", ["Name", "name", "Nаme"], ["Name", "Nаme", "name"], "select ", "", 1, 8, 8),
        new("REC-096-H02", "case-and-homograph", "Cde", ["Code", "Cоde"], ["Code", "Cоde"], "select ", "", 1, 8, 8),
        new("REC-096-H03", "case-and-homograph", "Q", ["O", "0", "О"], ["0", "O", "О"], "value=", "", 1, 7, 7),
        new("REC-096-H04", "case-and-homograph", "B", ["A", "А", "4"], ["4", "A", "А"], "value=", "", 1, 7, 7),

        new("REC-096-T01", "culture", "Istanbu", ["Istanbul"], ["Istanbul"], "value=", "", 1, 7, 7),
        new("REC-096-T02", "culture", "Izmir", ["İzmir"], ["İzmir"], "value=", "", 1, 7, 7),
        new("REC-096-T03", "culture", "ışığ", ["ışık"], ["ışık"], "value=", "", 1, 7, 7),
        new("REC-096-T04", "culture", "TURKIYE", ["TÜRKİYE"], ["TÜRKİYE"], "value=", "", 1, 7, 7),

        new("REC-096-F01", "formatting", "Naem", ["Name"], ["Name"], "select ", "\nfrom #source.Rows()", 1, 8, 8),
        new("REC-096-F02", "formatting", "Ctiy", ["City"], ["City"], "select 1\r\nwhere ", "", 2, 7, 7),
        new("REC-096-F03", "formatting", "Countr", ["Country"], ["Country"], "select 1\r\n\t", "", 2, 2, 5),
        new("REC-096-F04", "formatting", "Populaton", ["Population"], ["Population"], "a\tselect ", "", 1, 10, 12),

        new("REC-096-Q01", "quoted-and-control", "Column With Spacs", ["Column With Spaces"], ["Column With Spaces"], "value=", "", 1, 7, 7),
        new("REC-096-Q02", "quoted-and-control", "quote'Nam", ["quote'Name"], ["quote'Name"], "value=", "", 1, 7, 7),
        new("REC-096-Q03", "quoted-and-control", @"escaped\Nam", [@"escaped\Name"], [@"escaped\Name"], "value=", "", 1, 7, 7),
        new("REC-096-Q04", "quoted-and-control", "line\n\tNam", ["line\n\tName"], ["line\n\tName"], "value=", "", 1, 7, 7)
    ];

    private static string ApplyEdit(string text, TextEdit edit)
    {
        return text[..edit.Span.Start] + edit.NewText + text[edit.Span.End..];
    }

    private static int GetTerminalColumn(string line, int sourceColumnZeroBased, int tabWidth = 4)
    {
        var terminalColumn = 1;
        for (var index = 0; index < sourceColumnZeroBased && index < line.Length; index++)
        {
            if (line[index] == '\t')
                terminalColumn += tabWidth - ((terminalColumn - 1) % tabWidth);
            else
                terminalColumn++;
        }

        return terminalColumn;
    }

    private sealed record RecoveryCase(
        string CaseId,
        string Family,
        string Input,
        IReadOnlyList<string> Candidates,
        IReadOnlyList<string> ExpectedCandidates,
        string SourcePrefix,
        string SourceSuffix,
        int ExpectedLine,
        int ExpectedSourceColumn,
        int ExpectedTerminalColumn);
}
