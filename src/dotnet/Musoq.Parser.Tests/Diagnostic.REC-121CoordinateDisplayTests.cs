using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

/// <summary>
///     Recovery campaign REC-121: source locations are UTF-16 coordinates,
///     while terminal presentation has its own display-cell and escaping rules.
/// </summary>
[TestClass]
public sealed class DiagnosticREC121CoordinateDisplayTests
{
    [TestMethod]
    [DynamicData(nameof(SourceCoordinateData))]
    public void SourceTextCoordinateMatrix_ShouldSeparateUtf16OffsetsFromLogicalLines(object data)
    {
        var candidate = (SourceCoordinateCase)data;
        var source = new SourceText(candidate.Source, "REC-121.sql");
        var start = source.GetLocation(candidate.Offset);
        var span = new TextSpan(candidate.Offset, candidate.Length);
        var locations = source.GetLocations(span);

        Assert.AreEqual(candidate.ExpectedLength, source.Length, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedLineCount, source.LineCount, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedLine, start.Line, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedColumn, start.Column, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedLine, locations.Start.Line, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedColumn, locations.Start.Column, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedEndLine, locations.End.Line, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedEndColumn, locations.End.Column, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedLineText, source.GetLineText(candidate.ExpectedLine), candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedText, source.GetText(span), candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedText, source.Text.Substring(candidate.Offset, candidate.Length), candidate.CaseId);
    }

    [TestMethod]
    [DynamicData(nameof(ParseDiagnosticData))]
    public void ParseDiagnosticCoordinateMatrix_ShouldResolveTheOriginalQueryRegion(object data)
    {
        var candidate = (ParseDiagnosticCase)data;
        var lexer = new Lexer(candidate.Query, true, recoverOnError: true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();

        Assert.IsFalse(result.Success, candidate.CaseId);
        Assert.HasCount(1, result.Diagnostics, candidate.CaseId);

        var diagnostic = result.Diagnostics.Single();
        var expectedSpan = new TextSpan(candidate.Offset, 1);
        Assert.AreEqual(DiagnosticCode.MQ1001_UnknownToken, diagnostic.Code, candidate.CaseId);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity, candidate.CaseId);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase, candidate.CaseId);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind, candidate.CaseId);
        Assert.AreEqual(expectedSpan, diagnostic.Span, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedLine, diagnostic.Location.Line, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedColumn, diagnostic.Location.Column, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedLine, diagnostic.EndLocation.Line, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedColumn + 1, diagnostic.EndLocation.Column, candidate.CaseId);
        Assert.AreEqual("@", result.SourceText.GetText(diagnostic.Span), candidate.CaseId);
        Assert.HasCount(1, result.GetDiagnosticsAt(candidate.Offset), candidate.CaseId);
        Assert.HasCount(1, result.GetDiagnosticsOnLine(candidate.ExpectedLine), candidate.CaseId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet), candidate.CaseId);
    }

    [TestMethod]
    [DynamicData(nameof(DisplayCoordinateData))]
    public void DisplayCoordinateMatrix_ShouldKeepControlSafePresentationSeparate(object data)
    {
        var candidate = (DisplayCoordinateCase)data;
        var source = new SourceText(candidate.Source, "REC-121.sql");
        var span = new TextSpan(candidate.Offset, 1);
        var diagnostic = Diagnostic
            .Error(DiagnosticCode.MQ2001_UnexpectedToken, "coordinate display", span)
            .WithSourceContext(source, span);
        var formatter = new DiagnosticFormatter { UseColor = false };

        Assert.AreEqual(candidate.ExpectedLine, diagnostic.Location.Line, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedColumn, diagnostic.Location.Column, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedLine, diagnostic.EndLocation.Line, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedColumn + 1, diagnostic.EndLocation.Column, candidate.CaseId);
        StringAssert.Contains(diagnostic.ContextSnippet!, candidate.RawLine, candidate.CaseId);

        var json = formatter.FormatAsJson(diagnostic);
        using var document = JsonDocument.Parse(json);
        var range = document.RootElement.GetProperty("range");
        var start = range.GetProperty("start");
        var end = range.GetProperty("end");
        Assert.AreEqual(candidate.ExpectedLine - 1, start.GetProperty("line").GetInt32(), candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedColumn - 1, start.GetProperty("character").GetInt32(), candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedLine - 1, end.GetProperty("line").GetInt32(), candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedColumn, end.GetProperty("character").GetInt32(), candidate.CaseId);

        var formatted = formatter.Format(diagnostic);
        Assert.IsFalse(formatted.Contains('\t', StringComparison.Ordinal), candidate.CaseId);
        Assert.IsFalse(formatted.Contains('\0', StringComparison.Ordinal), candidate.CaseId);
        Assert.IsFalse(formatted.Contains('\b', StringComparison.Ordinal), candidate.CaseId);
        Assert.IsFalse(formatted.Contains('\f', StringComparison.Ordinal), candidate.CaseId);

        var lines = formatted.Split(Environment.NewLine, StringSplitOptions.None);
        var displayLineIndex = Array.FindIndex(
            lines,
            line => line.Contains(" | " + candidate.ExpectedDisplayLine, StringComparison.Ordinal));
        Assert.IsGreaterThanOrEqualTo(0, displayLineIndex, candidate.CaseId);
        var caretLine = lines[displayLineIndex + 1];
        var separator = caretLine.IndexOf("| ", StringComparison.Ordinal);
        var caret = caretLine.IndexOf('^', separator + 2);
        Assert.IsGreaterThanOrEqualTo(0, separator, candidate.CaseId);
        Assert.AreEqual(separator + 2 + candidate.ExpectedDisplayColumn - 1, caret, candidate.CaseId);
    }

    [TestMethod]
    [DynamicData(nameof(SchemaDiagnosticData))]
    public void SchemaDiagnosticCoordinateMatrix_ShouldUseCharacterOffsetsNotByteOffsets(object data)
    {
        var candidate = (SchemaDiagnosticCase)data;
        var lexer = new Lexer(candidate.Schema, true, recoverOnError: true);
        var exception = Assert.ThrowsExactly<SyntaxException>(
            () => new SchemaParser(lexer).ParseSchema(),
            candidate.CaseId);
        var expectedSpan = new TextSpan(candidate.Offset, "middle".Length);

        Assert.AreEqual(DiagnosticCode.MQ4005_InvalidEndianness, exception.Code, candidate.CaseId);
        Assert.AreEqual(expectedSpan, exception.Span, candidate.CaseId);

        var diagnostic = exception.ToDiagnostic(new SourceText(candidate.Schema));
        Assert.AreEqual(DiagnosticCode.MQ4005_InvalidEndianness, diagnostic.Code, candidate.CaseId);
        Assert.AreEqual(DiagnosticPhase.Schema, diagnostic.Phase, candidate.CaseId);
        Assert.AreEqual(DiagnosticSourceKind.Schema, diagnostic.SourceKind, candidate.CaseId);
        Assert.AreEqual(expectedSpan, diagnostic.Span, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedLine, diagnostic.Location.Line, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedColumn, diagnostic.Location.Column, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedLine, diagnostic.EndLocation.Line, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedColumn + "middle".Length, diagnostic.EndLocation.Column, candidate.CaseId);
        Assert.AreEqual("middle", new SourceText(candidate.Schema).GetText(diagnostic.Span), candidate.CaseId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet), candidate.CaseId);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, candidate.Schema);
        Assert.AreEqual(candidate.Offset, envelope.Offset, candidate.CaseId);
        Assert.AreEqual("middle".Length, envelope.Length, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedLine, envelope.Line, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedColumn, envelope.Column, candidate.CaseId);
    }

    [TestMethod]
    public void CoordinateMatrix_ShouldContainTwelveCasesPerPublicSurface()
    {
        Assert.HasCount(12, SourceCases);
        Assert.HasCount(12, ParseCases);
        Assert.HasCount(12, DisplayCases);
        Assert.HasCount(12, SchemaCases);

        var ids = SourceCases.Select(static item => item.CaseId)
            .Concat(ParseCases.Select(static item => item.CaseId))
            .Concat(DisplayCases.Select(static item => item.CaseId))
            .Concat(SchemaCases.Select(static item => item.CaseId))
            .ToArray();
        Assert.HasCount(48, ids.Distinct(StringComparer.Ordinal));
    }

    public static IEnumerable<object[]> SourceCoordinateData() =>
        SourceCases.Select(static item => new object[] { item });

    public static IEnumerable<object[]> ParseDiagnosticData() =>
        ParseCases.Select(static item => new object[] { item });

    public static IEnumerable<object[]> DisplayCoordinateData() =>
        DisplayCases.Select(static item => new object[] { item });

    public static IEnumerable<object[]> SchemaDiagnosticData() =>
        SchemaCases.Select(static item => new object[] { item });

    private static readonly SourceCoordinateCase[] SourceCases =
    [
        new("S01", "head\n@", 6, 5, 1, 2, 1, 2, 2, 2, "@", "@"),
        new("S02", "head\r\n@", 7, 6, 1, 2, 1, 2, 2, 2, "@", "@"),
        new("S03", "a\r\nb\nc@", 7, 6, 1, 3, 2, 3, 3, 3, "c@", "@"),
        new("S04", "a\r\nb\rc@", 7, 6, 1, 3, 2, 3, 3, 3, "c@", "@"),
        new("S05", "line\nend", 8, 8, 0, 2, 4, 2, 4, 2, "end", ""),
        new("S06", "line\r\nend", 9, 9, 0, 2, 4, 2, 4, 2, "end", ""),
        new("S07", "x", 1, 1, 0, 1, 2, 1, 2, 1, "x", ""),
        new("S08", "😀\t@", 4, 3, 1, 1, 4, 1, 5, 1, "😀\t@", "@"),
        new("S09", "e\u0301@", 3, 2, 1, 1, 3, 1, 4, 1, "e\u0301@", "@"),
        new("S10", "界@", 2, 1, 1, 1, 2, 1, 3, 1, "界@", "@"),
        new("S11", "\t😀\t@", 5, 4, 1, 1, 5, 1, 6, 1, "\t😀\t@", "@"),
        new("S12", "pre\r\n\t界@", 8, 7, 1, 2, 3, 2, 4, 2, "\t界@", "@")
    ];

    private static readonly ParseDiagnosticCase[] ParseCases =
    [
        new("P01", "select @", 7, 1, 8),
        new("P02", "select\t@", 7, 1, 8),
        new("P03", "-- 😀\t界\nselect @", 15, 2, 8),
        new("P04", "-- e\u0301\rselect @", 13, 2, 8),
        new("P05", "-- 界\r\nselect @", 13, 2, 8),
        new("P06", "--\r\n\tselect @", 12, 2, 9),
        new("P07", "-- 😀\n\tselect @", 14, 2, 9),
        new("P08", "-- e\u0301\nselect @", 13, 2, 8),
        new("P09", "-- \t界\nselect @", 13, 2, 8),
        new("P10", "select 1\r\n\t@", 11, 2, 2),
        new("P11", "select 1\n\t@", 10, 2, 2),
        new("P12", "-- wide 界\r\n@", 11, 2, 1)
    ];

    private static readonly DisplayCoordinateCase[] DisplayCases =
    [
        new("D01", "a\t@", 2, 1, 3, "a\t@", "a   @", 5),
        new("D02", "\t@", 1, 1, 2, "\t@", "    @", 5),
        new("D03", "😀@", 2, 1, 3, "😀@", "😀@", 3),
        new("D04", "界@", 1, 1, 2, "界@", "界@", 3),
        new("D05", "e\u0301@", 2, 1, 3, "e\u0301@", "e\u0301@", 2),
        new("D06", "😀\t@", 3, 1, 4, "😀\t@", "😀  @", 5),
        new("D07", "\t界@", 2, 1, 3, "\t界@", "    界@", 7),
        new("D08", "\t e\u0301@", 4, 1, 5, "\t e\u0301@", "     e\u0301@", 7),
        new("D09", "\0@", 1, 1, 2, "\0@", "\\u0000@", 7),
        new("D10", "a\b@", 2, 1, 3, "a\b@", "a\\b@", 4),
        new("D11", "a\f@", 2, 1, 3, "a\f@", "a\\f@", 4),
        new("D12", "x\t😀\te\u0301@", 7, 1, 8, "x\t😀\te\u0301@", "x   😀  e\u0301@", 10)
    ];

    private static readonly SchemaDiagnosticCase[] SchemaCases =
    [
        new("B01", "binary Packet { Value: int middle }", 27, 1, 28),
        new("B02", "binary Packet {\n Value: int middle\n}", 28, 2, 13),
        new("B03", "binary Packet {\r\n Value: int middle\r\n}", 29, 2, 13),
        new("B04", "binary Packet {\n\tValue: int middle\n}", 28, 2, 13),
        new("B05", "binary Packet {\r\n\tValue: int middle\r\n}", 29, 2, 13),
        new("B06", "-- 😀\r\nbinary Packet {\n Value: int middle\n}", 35, 3, 13),
        new("B07", "binary Packet {\n -- e\u0301\r\n Value: int middle\n}", 36, 3, 13),
        new("B08", "binary 界 {\n Value: int middle\n}", 23, 2, 13),
        new("B09", "-- e\u0301\nbinary Packet {\n Value: int middle\n}", 34, 3, 13),
        new("B10", "-- 界\nbinary Packet {\n\tValue: int middle\n}", 33, 3, 13),
        new("B11", "binary Packet {\r\n\t-- 😀\r\n\tValue: int middle\r\n}", 37, 3, 13),
        new("B12", "binary Packet {\n\tValue: int middle}", 28, 2, 13)
    ];

    private sealed record SourceCoordinateCase(
        string CaseId,
        string Source,
        int ExpectedLength,
        int Offset,
        int Length,
        int ExpectedLine,
        int ExpectedColumn,
        int ExpectedEndLine,
        int ExpectedEndColumn,
        int ExpectedLineCount,
        string ExpectedLineText,
        string ExpectedText);

    private sealed record ParseDiagnosticCase(
        string CaseId,
        string Query,
        int Offset,
        int ExpectedLine,
        int ExpectedColumn);

    private sealed record DisplayCoordinateCase(
        string CaseId,
        string Source,
        int Offset,
        int ExpectedLine,
        int ExpectedColumn,
        string RawLine,
        string ExpectedDisplayLine,
        int ExpectedDisplayColumn);

    private sealed record SchemaDiagnosticCase(
        string CaseId,
        string Schema,
        int Offset,
        int ExpectedLine,
        int ExpectedColumn);
}
