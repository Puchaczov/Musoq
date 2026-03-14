using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class BranchCoverageTests
{
    private static RootNode ParseQuery(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        return parser.ComposeAll();
    }

    #region TextSpan Branch Coverage

    [TestMethod]
    public void TextSpan_Through_WhenThisIsEmpty_ShouldReturnOther()
    {
        var empty = TextSpan.Empty;
        var other = new TextSpan(5, 10);

        var result = empty.Through(other);

        Assert.AreEqual(5, result.Start);
        Assert.AreEqual(10, result.Length);
    }

    [TestMethod]
    public void TextSpan_Through_WhenOtherIsEmpty_ShouldReturnThis()
    {
        var span = new TextSpan(5, 10);
        var empty = TextSpan.Empty;

        var result = span.Through(empty);

        Assert.AreEqual(5, result.Start);
        Assert.AreEqual(10, result.Length);
    }

    [TestMethod]
    public void TextSpan_Through_WhenBothNonEmpty_ShouldReturnCombined()
    {
        var span1 = new TextSpan(5, 10);
        var span2 = new TextSpan(20, 5);

        var result = span1.Through(span2);

        Assert.AreEqual(5, result.Start);
        Assert.AreEqual(25, result.End);
    }

    [TestMethod]
    public void TextSpan_Intersection_WhenOverlapping_ShouldReturnIntersection()
    {
        var span1 = new TextSpan(5, 10);
        var span2 = new TextSpan(10, 10);

        var result = span1.Intersection(span2);

        Assert.IsNotNull(result);
        Assert.AreEqual(10, result.Value.Start);
        Assert.AreEqual(5, result.Value.Length);
    }

    [TestMethod]
    public void TextSpan_Intersection_WhenNoOverlap_ShouldReturnNull()
    {
        var span1 = new TextSpan(5, 5);
        var span2 = new TextSpan(15, 5);

        var result = span1.Intersection(span2);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TextSpan_Intersection_WhenTouching_ShouldReturnNull()
    {
        var span1 = new TextSpan(5, 5);
        var span2 = new TextSpan(10, 5);

        var result = span1.Intersection(span2);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TextSpan_Contains_Position_InsideSpan_ShouldReturnTrue()
    {
        var span = new TextSpan(5, 10);

        Assert.IsTrue(span.Contains(5));
        Assert.IsTrue(span.Contains(14));
    }

    [TestMethod]
    public void TextSpan_Contains_Position_OutsideSpan_ShouldReturnFalse()
    {
        var span = new TextSpan(5, 10);

        Assert.IsFalse(span.Contains(4));
        Assert.IsFalse(span.Contains(15));
    }

    [TestMethod]
    public void TextSpan_Contains_OtherSpan_FullyContained_ShouldReturnTrue()
    {
        var outer = new TextSpan(5, 20);
        var inner = new TextSpan(10, 5);

        Assert.IsTrue(outer.Contains(inner));
    }

    [TestMethod]
    public void TextSpan_Contains_OtherSpan_NotContained_ShouldReturnFalse()
    {
        var span1 = new TextSpan(5, 5);
        var span2 = new TextSpan(8, 10);

        Assert.IsFalse(span1.Contains(span2));
    }

    [TestMethod]
    public void TextSpan_Overlaps_WhenOverlapping_ShouldReturnTrue()
    {
        var span1 = new TextSpan(5, 10);
        var span2 = new TextSpan(10, 10);

        Assert.IsTrue(span1.Overlaps(span2));
    }

    [TestMethod]
    public void TextSpan_Overlaps_WhenNotOverlapping_ShouldReturnFalse()
    {
        var span1 = new TextSpan(5, 5);
        var span2 = new TextSpan(10, 5);

        Assert.IsFalse(span1.Overlaps(span2));
    }

    [TestMethod]
    public void TextSpan_IsEmpty_ShouldReturnTrueForZeroLength()
    {
        var span = new TextSpan(5, 0);

        Assert.IsTrue(span.IsEmpty);
    }

    [TestMethod]
    public void TextSpan_IsEmpty_ShouldReturnFalseForNonZeroLength()
    {
        var span = new TextSpan(5, 1);

        Assert.IsFalse(span.IsEmpty);
    }

    [TestMethod]
    public void TextSpan_WithLength_ShouldReturnNewSpanWithNewLength()
    {
        var span = new TextSpan(5, 10);

        var result = span.WithLength(20);

        Assert.AreEqual(5, result.Start);
        Assert.AreEqual(20, result.Length);
    }

    [TestMethod]
    public void TextSpan_WithStart_ShouldReturnNewSpanWithNewStart()
    {
        var span = new TextSpan(5, 10);

        var result = span.WithStart(15);

        Assert.AreEqual(15, result.Start);
        Assert.AreEqual(10, result.Length);
    }

    [TestMethod]
    public void TextSpan_FromBounds_ShouldCreateCorrectSpan()
    {
        var span = TextSpan.FromBounds(5, 15);

        Assert.AreEqual(5, span.Start);
        Assert.AreEqual(10, span.Length);
        Assert.AreEqual(15, span.End);
    }

    [TestMethod]
    public void TextSpan_CompareTo_ShouldCompareByStartThenLength()
    {
        var span1 = new TextSpan(5, 10);
        var span2 = new TextSpan(5, 20);
        var span3 = new TextSpan(10, 5);

        Assert.IsLessThan(0, span1.CompareTo(span2));
        Assert.IsLessThan(0, span1.CompareTo(span3));
        Assert.AreEqual(0, span1.CompareTo(new TextSpan(5, 10)));
    }

    [TestMethod]
    public void TextSpan_GetStartLocation_ShouldReturnLocationAtStart()
    {
        var sourceText = new SourceText("SELECT\nFROM");
        var span = new TextSpan(7, 4);

        var location = span.GetStartLocation(sourceText);

        Assert.AreEqual(7, location.Offset);
        Assert.AreEqual(2, location.Line);
    }

    [TestMethod]
    public void TextSpan_GetEndLocation_ShouldReturnLocationAtEnd()
    {
        var sourceText = new SourceText("SELECT\nFROM");
        var span = new TextSpan(7, 4);

        var location = span.GetEndLocation(sourceText);

        Assert.AreEqual(11, location.Offset);
    }

    [TestMethod]
    public void TextSpan_Equals_WithNull_ShouldReturnFalse()
    {
        var span = new TextSpan(5, 10);

        Assert.IsFalse(span.Equals(null));
    }

    [TestMethod]
    public void TextSpan_Equals_WithNonTextSpanObject_ShouldReturnFalse()
    {
        var span = new TextSpan(5, 10);

        Assert.IsFalse(span.Equals("not a span"));
    }

    [TestMethod]
    public void TextSpan_Equals_WithMatchingTextSpanObject_ShouldReturnTrue()
    {
        var span1 = new TextSpan(5, 10);
        object span2 = new TextSpan(5, 10);

        Assert.IsTrue(span1.Equals(span2));
    }

    [TestMethod]
    public void TextSpan_Equals_WithDifferentTextSpanObject_ShouldReturnFalse()
    {
        var span1 = new TextSpan(5, 10);
        object span2 = new TextSpan(5, 20);

        Assert.IsFalse(span1.Equals(span2));
    }

    [TestMethod]
    public void TextSpan_GetHashCode_ShouldBeConsistentForEqualSpans()
    {
        var span1 = new TextSpan(5, 10);
        var span2 = new TextSpan(5, 10);

        Assert.AreEqual(span1.GetHashCode(), span2.GetHashCode());
    }

    [TestMethod]
    public void TextSpan_Operators_ShouldWorkCorrectly()
    {
        var span1 = new TextSpan(5, 10);
        var span2 = new TextSpan(5, 10);
        var span3 = new TextSpan(5, 20);

        Assert.IsTrue(span1 == span2);
        Assert.IsFalse(span1 != span2);
        Assert.IsTrue(span1 != span3);
        Assert.IsFalse(span1 == span3);
    }

    [TestMethod]
    public void TextSpan_ToString_ShouldReturnIntervalNotation()
    {
        var span = new TextSpan(5, 10);

        Assert.AreEqual("[5..15)", span.ToString());
    }

    #endregion

    #region SourceLocation Branch Coverage

    [TestMethod]
    public void SourceLocation_None_ShouldBeInvalid()
    {
        var none = SourceLocation.None;

        Assert.IsFalse(none.IsValid);
        Assert.AreEqual(-1, none.Offset);
        Assert.AreEqual(-1, none.Line);
        Assert.AreEqual(-1, none.Column);
    }

    [TestMethod]
    public void SourceLocation_WithFilePath_ShouldCreateNewLocationWithFilePath()
    {
        var loc = new SourceLocation(10, 2, 5);

        var withFile = loc.WithFilePath("test.sql");

        Assert.AreEqual(10, withFile.Offset);
        Assert.AreEqual(2, withFile.Line);
        Assert.AreEqual(5, withFile.Column);
        Assert.AreEqual("test.sql", withFile.FilePath);
    }

    [TestMethod]
    public void SourceLocation_ToString_WhenValid_WithoutFilePath_ShouldReturnLineColumn()
    {
        var loc = new SourceLocation(10, 2, 5);

        Assert.AreEqual("(2,5)", loc.ToString());
    }

    [TestMethod]
    public void SourceLocation_ToString_WhenValid_WithFilePath_ShouldIncludeFilePath()
    {
        var loc = new SourceLocation(10, 2, 5, "test.sql");

        Assert.AreEqual("test.sql(2,5)", loc.ToString());
    }

    [TestMethod]
    public void SourceLocation_ToString_WhenInvalid_ShouldReturnUnknown()
    {
        var loc = SourceLocation.None;

        Assert.AreEqual("(unknown)", loc.ToString());
    }

    [TestMethod]
    public void SourceLocation_ToLspString_WhenValid_ShouldReturnZeroBased()
    {
        var loc = new SourceLocation(10, 2, 5);

        Assert.AreEqual("(1,4)", loc.ToLspString());
    }

    [TestMethod]
    public void SourceLocation_ToLspString_WhenInvalid_ShouldReturnUnknown()
    {
        var loc = SourceLocation.None;

        Assert.AreEqual("(unknown)", loc.ToLspString());
    }

    [TestMethod]
    public void SourceLocation_Equality_ShouldCompareAllFields()
    {
        var loc1 = new SourceLocation(10, 2, 5, "a.sql");
        var loc2 = new SourceLocation(10, 2, 5, "a.sql");
        var loc3 = new SourceLocation(10, 2, 5, "b.sql");
        var loc4 = new SourceLocation(10, 2, 6, "a.sql");

        Assert.AreEqual(loc1, loc2);
        Assert.AreNotEqual(loc1, loc3);
        Assert.AreNotEqual(loc1, loc4);
    }

    [TestMethod]
    public void SourceLocation_Equality_ObjectOverload_ShouldWork()
    {
        var loc1 = new SourceLocation(10, 2, 5);
        object loc2 = new SourceLocation(10, 2, 5);
        object notLocation = "not a location";

        Assert.IsTrue(loc1.Equals(loc2));
        Assert.IsFalse(loc1.Equals(notLocation));
        Assert.IsFalse(loc1.Equals(null));
    }

    [TestMethod]
    public void SourceLocation_GetHashCode_ShouldBeConsistent()
    {
        var loc1 = new SourceLocation(10, 2, 5);
        var loc2 = new SourceLocation(10, 2, 5);

        Assert.AreEqual(loc1.GetHashCode(), loc2.GetHashCode());
    }

    [TestMethod]
    public void SourceLocation_CompareTo_ShouldCompareByOffsetThenLineThenColumn()
    {
        var loc1 = new SourceLocation(10, 2, 5);
        var loc2 = new SourceLocation(20, 3, 1);
        var loc3 = new SourceLocation(10, 3, 1);
        var loc4 = new SourceLocation(10, 2, 8);

        Assert.IsLessThan(0, loc1.CompareTo(loc2));
        Assert.IsLessThan(0, loc1.CompareTo(loc3));
        Assert.IsLessThan(0, loc1.CompareTo(loc4));
        Assert.IsGreaterThan(0, loc2.CompareTo(loc1));
    }

    [TestMethod]
    public void SourceLocation_ComparisonOperators_ShouldWorkCorrectly()
    {
        var loc1 = new SourceLocation(10, 2, 5);
        var loc2 = new SourceLocation(20, 3, 1);
        var loc3 = new SourceLocation(10, 2, 5);

        Assert.IsTrue(loc1 < loc2);
        Assert.IsTrue(loc1 <= loc2);
        Assert.IsTrue(loc1 <= loc3);
        Assert.IsTrue(loc2 > loc1);
        Assert.IsTrue(loc2 >= loc1);
        Assert.IsTrue(loc3 >= loc1);
        Assert.IsTrue(loc1 == loc3);
        Assert.IsTrue(loc1 != loc2);
    }

    [TestMethod]
    public void SourceLocation_Line0AndColumn0_ShouldReturnZeroBased()
    {
        var loc = new SourceLocation(10, 3, 7);

        Assert.AreEqual(2, loc.Line0);
        Assert.AreEqual(6, loc.Column0);
    }

    #endregion

    #region TextEdit Branch Coverage

    [TestMethod]
    public void TextEdit_Constructor_ShouldSetProperties()
    {
        var span = new TextSpan(10, 5);
        var edit = new TextEdit(span, "replacement");

        Assert.AreEqual(span, edit.Span);
        Assert.AreEqual("replacement", edit.NewText);
    }

    [TestMethod]
    public void TextEdit_Insert_ShouldCreateInsertionEdit()
    {
        var edit = TextEdit.Insert(10, "inserted text");

        Assert.AreEqual(10, edit.Span.Start);
        Assert.AreEqual(0, edit.Span.Length);
        Assert.AreEqual("inserted text", edit.NewText);
    }

    [TestMethod]
    public void TextEdit_Delete_ShouldCreateDeletionEdit()
    {
        var span = new TextSpan(10, 5);
        var edit = TextEdit.Delete(span);

        Assert.AreEqual(span, edit.Span);
        Assert.AreEqual(string.Empty, edit.NewText);
    }

    [TestMethod]
    public void TextEdit_Replace_ShouldCreateReplacementEdit()
    {
        var span = new TextSpan(10, 5);
        var edit = TextEdit.Replace(span, "new text");

        Assert.AreEqual(span, edit.Span);
        Assert.AreEqual("new text", edit.NewText);
    }

    #endregion

    #region ParseResult Branch Coverage

    [TestMethod]
    public void ParseResult_WithNoErrors_ShouldBeSuccessful()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var root = ParseQuery(sourceText.Text);
        var parseResult = new ParseResult(root, sourceText);

        Assert.IsTrue(parseResult.Success);
        Assert.IsFalse(parseResult.HasErrors);
        Assert.IsFalse(parseResult.HasWarnings);
        Assert.IsTrue(parseResult.HasAst);
        Assert.AreEqual(0, parseResult.ErrorCount);
        Assert.AreEqual(0, parseResult.WarningCount);
    }

    [TestMethod]
    public void ParseResult_WithErrors_ShouldNotBeSuccessful()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var root = ParseQuery(sourceText.Text);
        var errors = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test error", new TextSpan(0, 5))
        };
        var parseResult = new ParseResult(root, sourceText, errors);

        Assert.IsFalse(parseResult.Success);
        Assert.IsTrue(parseResult.HasErrors);
        Assert.AreEqual(1, parseResult.ErrorCount);
    }

    [TestMethod]
    public void ParseResult_WithWarnings_ShouldHaveWarnings()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var root = ParseQuery(sourceText.Text);
        var warnings = new[]
        {
            Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "test warning", new TextSpan(0, 5))
        };
        var parseResult = new ParseResult(root, sourceText, warnings);

        Assert.IsTrue(parseResult.Success);
        Assert.IsTrue(parseResult.HasWarnings);
        Assert.AreEqual(1, parseResult.WarningCount);
        Assert.AreEqual(0, parseResult.ErrorCount);
    }

    [TestMethod]
    public void ParseResult_Errors_ShouldFilterOnlyErrors()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var root = ParseQuery(sourceText.Text);
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 5)),
            Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(5, 3))
        };
        var parseResult = new ParseResult(root, sourceText, diagnostics);

        var errors = parseResult.Errors.ToArray();

        Assert.HasCount(1, errors);
        Assert.AreEqual(DiagnosticSeverity.Error, errors[0].Severity);
    }

    [TestMethod]
    public void ParseResult_Warnings_ShouldFilterOnlyWarnings()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var root = ParseQuery(sourceText.Text);
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 5)),
            Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(5, 3))
        };
        var parseResult = new ParseResult(root, sourceText, diagnostics);

        var warnings = parseResult.Warnings.ToArray();

        Assert.HasCount(1, warnings);
        Assert.AreEqual(DiagnosticSeverity.Warning, warnings[0].Severity);
    }

    [TestMethod]
    public void ParseResult_Failed_ShouldHaveNoAst()
    {
        var sourceText = new SourceText("invalid");
        var errors = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "parse failed", new TextSpan(0, 7))
        };

        var result = ParseResult.Failed(sourceText, errors);

        Assert.IsFalse(result.HasAst);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.HasErrors);
    }

    [TestMethod]
    public void ParseResult_GetSortedDiagnostics_ShouldSortByOffset()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "second", new TextSpan(10, 5)),
            Diagnostic.Error(DiagnosticCode.MQ2002_MissingToken, "first", new TextSpan(0, 5))
        };
        var parseResult = new ParseResult(null, sourceText, diagnostics);

        var sorted = parseResult.GetSortedDiagnostics().ToArray();

        Assert.AreEqual("first", sorted[0].Message);
        Assert.AreEqual("second", sorted[1].Message);
    }

    [TestMethod]
    public void ParseResult_GetDiagnosticsAt_ShouldFindDiagnosticsAtPosition()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "at position", new TextSpan(5, 3))
        };
        var parseResult = new ParseResult(null, sourceText, diagnostics);

        var found = parseResult.GetDiagnosticsAt(6).ToArray();

        Assert.HasCount(1, found);
    }

    [TestMethod]
    public void ParseResult_GetDiagnosticsAt_WithTolerance_ShouldFindNearbyDiagnostics()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "at position", new TextSpan(5, 3))
        };
        var parseResult = new ParseResult(null, sourceText, diagnostics);

        var found = parseResult.GetDiagnosticsAt(10, tolerance: 3).ToArray();

        Assert.HasCount(1, found);
    }

    [TestMethod]
    public void ParseResult_GetDiagnosticsOnLine_ShouldFindDiagnosticsOnSpecificLine()
    {
        var sourceText = new SourceText("SELECT\nFROM table", "test.sql");
        var diagnostics = new[]
        {
            Diagnostic.Error(
                DiagnosticCode.MQ2001_UnexpectedToken,
                "error on line 2",
                new SourceLocation(7, 2, 1),
                new SourceLocation(11, 2, 5))
        };
        var parseResult = new ParseResult(null, sourceText, diagnostics);

        var line2 = parseResult.GetDiagnosticsOnLine(2).ToArray();

        Assert.HasCount(1, line2);
    }

    [TestMethod]
    public void ParseResult_FormatDiagnostics_WhenEmpty_ShouldReturnNoDiagnostics()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var parseResult = new ParseResult(null, sourceText, Array.Empty<Diagnostic>());

        var formatted = parseResult.FormatDiagnostics();

        Assert.AreEqual("No diagnostics.", formatted);
    }

    [TestMethod]
    public void ParseResult_FormatDiagnostics_WhenHasErrors_ShouldFormat()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "some error", new TextSpan(0, 5))
        };
        var parseResult = new ParseResult(null, sourceText, diagnostics);

        var formatted = parseResult.FormatDiagnostics();

        Assert.Contains("some error", formatted);
    }

    [TestMethod]
    public void ParseResult_ThrowIfErrors_WhenNoErrors_ShouldNotThrow()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var parseResult = new ParseResult(null, sourceText, Array.Empty<Diagnostic>());

        parseResult.ThrowIfErrors();
    }

    [TestMethod]
    public void ParseResult_ThrowIfErrors_WhenHasErrors_ShouldThrow()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "fatal error", new TextSpan(0, 5))
        };
        var parseResult = new ParseResult(null, sourceText, diagnostics);

        Assert.Throws<ParseException>(() => parseResult.ThrowIfErrors());
    }

    #endregion

    #region SourceText Branch Coverage

    [TestMethod]
    public void SourceText_GetLocation_NegativeOffset_ShouldReturnNone()
    {
        var sourceText = new SourceText("SELECT 1");

        var loc = sourceText.GetLocation(-1);

        Assert.IsFalse(loc.IsValid);
    }

    [TestMethod]
    public void SourceText_GetLocation_BeyondEnd_ShouldClampToEnd()
    {
        var sourceText = new SourceText("SELECT 1");

        var loc = sourceText.GetLocation(100);

        Assert.AreEqual(8, loc.Offset);
    }

    [TestMethod]
    public void SourceText_GetLineText_OutOfRange_ShouldReturnEmpty()
    {
        var sourceText = new SourceText("line1\nline2");

        Assert.AreEqual(string.Empty, sourceText.GetLineText(0));
        Assert.AreEqual(string.Empty, sourceText.GetLineText(10));
    }

    [TestMethod]
    public void SourceText_GetLineSpan_OutOfRange_ShouldReturnEmpty()
    {
        var sourceText = new SourceText("line1\nline2");

        var span = sourceText.GetLineSpan(0);

        Assert.AreEqual(TextSpan.Empty, span);
    }

    [TestMethod]
    public void SourceText_GetLineSpan_ValidLine_ShouldReturnCorrectSpan()
    {
        var sourceText = new SourceText("line1\nline2\nline3");

        var span = sourceText.GetLineSpan(2);

        Assert.AreEqual(6, span.Start);
    }

    [TestMethod]
    public void SourceText_GetText_BySpan_ShouldReturnCorrectText()
    {
        var sourceText = new SourceText("Hello World");

        var text = sourceText.GetText(new TextSpan(6, 5));

        Assert.AreEqual("World", text);
    }

    [TestMethod]
    public void SourceText_GetText_ByStartAndLength_ShouldReturnCorrectText()
    {
        var sourceText = new SourceText("Hello World");

        var text = sourceText.GetText(6, 5);

        Assert.AreEqual("World", text);
    }

    [TestMethod]
    public void SourceText_GetText_WhenStartBeyondLength_ShouldReturnEmpty()
    {
        var sourceText = new SourceText("Hello");

        var text = sourceText.GetText(new TextSpan(100, 5));

        Assert.AreEqual(string.Empty, text);
    }

    [TestMethod]
    public void SourceText_GetText_WhenSpanExceedsEnd_ShouldClamp()
    {
        var sourceText = new SourceText("Hello");

        var text = sourceText.GetText(new TextSpan(3, 100));

        Assert.AreEqual("lo", text);
    }

    [TestMethod]
    public void SourceText_GetLocations_ShouldReturnStartAndEnd()
    {
        var sourceText = new SourceText("SELECT\nFROM table");
        var span = new TextSpan(7, 4);

        var (start, end) = sourceText.GetLocations(span);

        Assert.AreEqual(7, start.Offset);
        Assert.AreEqual(2, start.Line);
        Assert.AreEqual(11, end.Offset);
    }

    [TestMethod]
    public void SourceText_WithCarriageReturn_ShouldCalculateCorrectLines()
    {
        var sourceText = new SourceText("line1\r\nline2\rline3");

        Assert.AreEqual(3, sourceText.LineCount);
        Assert.AreEqual("line1", sourceText.GetLineText(1));
        Assert.AreEqual("line2", sourceText.GetLineText(2));
        Assert.AreEqual("line3", sourceText.GetLineText(3));
    }

    [TestMethod]
    public void SourceText_Indexer_ShouldReturnCharacterAtIndex()
    {
        var sourceText = new SourceText("ABC");

        Assert.AreEqual('A', sourceText[0]);
        Assert.AreEqual('B', sourceText[1]);
        Assert.AreEqual('C', sourceText[2]);
    }

    [TestMethod]
    public void SourceText_FilePath_ShouldBeAccessible()
    {
        var sourceText = new SourceText("test", "test.sql");

        Assert.AreEqual("test.sql", sourceText.FilePath);
    }

    [TestMethod]
    public void SourceText_Length_ShouldMatch()
    {
        var sourceText = new SourceText("Hello");

        Assert.AreEqual(5, sourceText.Length);
    }

    [TestMethod]
    public void SourceText_GetLineSpan_LastLine_ShouldReturnToEnd()
    {
        var sourceText = new SourceText("line1\nline2");

        var span = sourceText.GetLineSpan(2);

        Assert.AreEqual(6, span.Start);
        Assert.AreEqual(5, span.Length);
    }

    #endregion

    #region Diagnostic Branch Coverage

    [TestMethod]
    public void Diagnostic_WithRelatedInfo_ShouldAddInfo()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", new TextSpan(0, 5));

        var withInfo = diag.WithRelatedInfo("extra info");

        Assert.HasCount(1, withInfo.RelatedInfo);
        Assert.AreEqual("extra info", withInfo.RelatedInfo[0]);
    }

    [TestMethod]
    public void Diagnostic_WithSuggestedFix_ShouldAddFix()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", new TextSpan(0, 5));
        var fix = DiagnosticAction.QuickFix("Fix it", new TextSpan(0, 5), "fixed");

        var withFix = diag.WithSuggestedFix(fix);

        Assert.HasCount(1, withFix.SuggestedFixes);
        Assert.AreEqual("Fix it", withFix.SuggestedFixes[0].Title);
    }

    [TestMethod]
    public void Diagnostic_WithExplanation_ShouldAddExplanation()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", new TextSpan(0, 5));

        var withExplanation = diag.WithExplanation("This happened because...");

        Assert.AreEqual("This happened because...", withExplanation.Explanation);
    }

    [TestMethod]
    public void Diagnostic_WithDocsReference_ShouldAddReference()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", new TextSpan(0, 5));

        var withDocs = diag.WithDocsReference("docs/errors.md#MQ2001");

        Assert.AreEqual("docs/errors.md#MQ2001", withDocs.DocsReference);
    }

    [TestMethod]
    public void Diagnostic_Info_ShouldHaveInfoSeverity()
    {
        var diag = Diagnostic.Info(DiagnosticCode.MQ5001_UnusedAlias, "info msg", new TextSpan(0, 5));

        Assert.AreEqual(DiagnosticSeverity.Info, diag.Severity);
    }

    [TestMethod]
    public void Diagnostic_Hint_ShouldHaveHintSeverity()
    {
        var diag = Diagnostic.Hint(DiagnosticCode.MQ5001_UnusedAlias, "hint msg", new TextSpan(0, 5));

        Assert.AreEqual(DiagnosticSeverity.Hint, diag.Severity);
    }

    [TestMethod]
    public void Diagnostic_Phase_ShouldMapFromCode()
    {
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", new TextSpan(0, 5));

        Assert.AreEqual(DiagnosticPhase.Parse, diag.Phase);
    }

    [TestMethod]
    public void Diagnostic_Span_ShouldBeDerivedFromLocations()
    {
        var start = new SourceLocation(5, 1, 6);
        var end = new SourceLocation(10, 1, 11);
        var diag = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test", start, end);

        var span = diag.Span;

        Assert.AreEqual(5, span.Start);
        Assert.AreEqual(5, span.Length);
    }

    #endregion

    #region DiagnosticAction Branch Coverage

    [TestMethod]
    public void DiagnosticAction_QuickFix_ShouldCreateQuickFixAction()
    {
        var action = DiagnosticAction.QuickFix("Fix it", new TextSpan(0, 5), "fixed");

        Assert.AreEqual("Fix it", action.Title);
        Assert.AreEqual(DiagnosticActionKind.QuickFix, action.Kind);
        Assert.IsNotNull(action.TextEdit);
        Assert.AreEqual("fixed", action.TextEdit.NewText);
    }

    [TestMethod]
    public void DiagnosticAction_Refactor_ShouldCreateRefactorAction()
    {
        var action = DiagnosticAction.Refactor("Refactor it", new TextSpan(0, 5), "refactored");

        Assert.AreEqual("Refactor it", action.Title);
        Assert.AreEqual(DiagnosticActionKind.Refactor, action.Kind);
        Assert.IsNotNull(action.TextEdit);
    }

    [TestMethod]
    public void DiagnosticAction_Suggestion_ShouldCreateSuggestionAction()
    {
        var action = DiagnosticAction.Suggestion("Consider this");

        Assert.AreEqual("Consider this", action.Title);
        Assert.AreEqual(DiagnosticActionKind.Suggestion, action.Kind);
        Assert.IsNull(action.TextEdit);
    }

    #endregion

    #region ParseException Branch Coverage

    [TestMethod]
    public void ParseException_ShouldCarryDiagnostics()
    {
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error 1", new TextSpan(0, 5)),
            Diagnostic.Error(DiagnosticCode.MQ2002_MissingToken, "error 2", new TextSpan(5, 3))
        };

        var exception = new ParseException("Parse failed", diagnostics);

        Assert.AreEqual("Parse failed", exception.Message);
        Assert.HasCount(2, exception.Diagnostics);
    }

    #endregion

    #region DiagnosticFormatter Branch Coverage

    [TestMethod]
    public void DiagnosticFormatter_Format_WithFilePath_ShouldIncludeFilePath()
    {
        var formatter = new DiagnosticFormatter();
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "test error",
            new SourceLocation(5, 2, 3, "test.sql"),
            new SourceLocation(10, 2, 8));

        var result = formatter.Format(diagnostic);

        Assert.Contains("test.sql", result);
        Assert.Contains("test error", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WithoutFilePath_ShouldUseParentheses()
    {
        var formatter = new DiagnosticFormatter();
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "test error",
            new TextSpan(0, 5));

        var result = formatter.Format(diagnostic);

        Assert.StartsWith("(", result);
        Assert.Contains("error", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WithColor_ShouldIncludeAnsiCodes()
    {
        var formatter = new DiagnosticFormatter { UseColor = true };
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "test error",
            new TextSpan(0, 5));

        var result = formatter.Format(diagnostic);

        Assert.Contains("\u001b[31m", result);
        Assert.Contains("\u001b[0m", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WithoutContextSnippet_ShouldOmitSnippet()
    {
        var formatter = new DiagnosticFormatter { IncludeContextSnippet = false };
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "test error",
            new TextSpan(0, 5));

        var result = formatter.Format(diagnostic);

        Assert.DoesNotContain("-->", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WithContextSnippet_ShouldShowSnippet()
    {
        var formatter = new DiagnosticFormatter { IncludeContextSnippet = true };
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticSeverity.Error,
            "test error",
            new SourceLocation(0, 1, 1),
            new SourceLocation(5, 1, 6),
            "SELECT * FROM table");

        var result = formatter.Format(diagnostic);

        Assert.Contains("SELECT * FROM table", result);
        Assert.Contains("^", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WithSuggestedFixes_ShouldShowFixes()
    {
        var formatter = new DiagnosticFormatter();
        var fix = DiagnosticAction.QuickFix("Add missing semicolon", new TextSpan(5, 1), ";");
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "missing semicolon",
            new TextSpan(0, 5))
            .WithSuggestedFix(fix);

        var result = formatter.Format(diagnostic);

        Assert.Contains("Suggested fixes:", result);
        Assert.Contains("Add missing semicolon", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WarningSeverity_ShouldShowWarning()
    {
        var formatter = new DiagnosticFormatter();
        var diagnostic = Diagnostic.Warning(
            DiagnosticCode.MQ5001_UnusedAlias,
            "unused alias",
            new TextSpan(0, 5));

        var result = formatter.Format(diagnostic);

        Assert.Contains("warning", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_FormatAsJson_ShouldReturnValidStructure()
    {
        var formatter = new DiagnosticFormatter();
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "test error",
            new SourceLocation(0, 1, 1),
            new SourceLocation(5, 1, 6));

        var result = formatter.FormatAsJson(diagnostic);

        Assert.Contains("\"range\"", result);
        Assert.Contains("\"severity\"", result);
        Assert.Contains("\"code\"", result);
        Assert.Contains("\"message\"", result);
        Assert.Contains("test error", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_FormatAsJson_WithSpecialChars_ShouldEscape()
    {
        var formatter = new DiagnosticFormatter();
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "error with \"quotes\" and\nnewline",
            new TextSpan(0, 5));

        var result = formatter.FormatAsJson(diagnostic);

        Assert.Contains("\\\"quotes\\\"", result);
        Assert.Contains("\\n", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WithColorSnippet_ShouldColorErrorLine()
    {
        var formatter = new DiagnosticFormatter { UseColor = true, IncludeContextSnippet = true };
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticSeverity.Warning,
            "test warning",
            new SourceLocation(0, 1, 1),
            new SourceLocation(5, 1, 6),
            "SELECT bad syntax");

        var result = formatter.Format(diagnostic);

        Assert.Contains("\u001b[33m", result);
    }

    #endregion

    #region DiagnosticBag Branch Coverage

    [TestMethod]
    public void DiagnosticBag_Add_NullDiagnostic_ShouldThrow()
    {
        var bag = new DiagnosticBag();

        Assert.Throws<ArgumentNullException>(() => bag.Add(null));
    }

    [TestMethod]
    public void DiagnosticBag_Add_WhenMaxErrorsReached_ShouldRejectErrors()
    {
        var bag = new DiagnosticBag { MaxErrors = 2 };

        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error 1", new TextSpan(0, 1));
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error 2", new TextSpan(1, 1));
        var added = bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error 3", new TextSpan(2, 1));

        Assert.IsFalse(added);
        Assert.IsTrue(bag.HasTooManyErrors);
        Assert.AreEqual(2, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_Add_WarningsNotLimited_ShouldAlwaysAdd()
    {
        var bag = new DiagnosticBag { MaxErrors = 1 };

        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 1));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(1, 1));

        Assert.AreEqual(1, bag.ErrorCount);
        Assert.AreEqual(1, bag.WarningCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddWithSourceText_ShouldIncludeContextSnippet()
    {
        var bag = new DiagnosticBag
        {
            SourceText = new SourceText("SELECT * FROM table")
        };

        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 6));

        var diagnostic = bag.First();
        Assert.IsNotNull(diagnostic.ContextSnippet);
    }

    [TestMethod]
    public void DiagnosticBag_AddWithoutSourceText_ShouldUseFallbackLocations()
    {
        var bag = new DiagnosticBag();

        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(5, 3));

        var diagnostic = bag.First();
        Assert.AreEqual(5, diagnostic.Location.Offset);
    }

    [TestMethod]
    public void DiagnosticBag_AddInfo_ShouldAddInfoDiagnostic()
    {
        var bag = new DiagnosticBag();

        bag.AddInfo(DiagnosticCode.MQ2001_UnexpectedToken, "info message", new TextSpan(0, 1));

        Assert.AreEqual(1, bag.Count);
        Assert.AreEqual(0, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddHint_ShouldAddHintDiagnostic()
    {
        var bag = new DiagnosticBag();

        bag.AddHint(DiagnosticCode.MQ2001_UnexpectedToken, "hint message", new TextSpan(0, 1));

        Assert.AreEqual(1, bag.Count);
        Assert.AreEqual(0, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddErrorWithFormattedArgs_ShouldFormatMessage()
    {
        var bag = new DiagnosticBag();

        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, new TextSpan(0, 1), "SELECT");

        Assert.IsTrue(bag.HasErrors);
        Assert.AreEqual(1, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddWarningWithFormattedArgs_ShouldFormatMessage()
    {
        var bag = new DiagnosticBag();

        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, new TextSpan(0, 1), "myAlias");

        Assert.AreEqual(1, bag.WarningCount);
    }

    [TestMethod]
    public void DiagnosticBag_GetErrors_ShouldReturnOnlyErrors()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 1));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(1, 1));

        var errors = bag.GetErrors().ToArray();

        Assert.HasCount(1, errors);
        Assert.IsTrue(errors[0].IsError);
    }

    [TestMethod]
    public void DiagnosticBag_GetWarnings_ShouldReturnOnlyWarnings()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 1));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(1, 1));

        var warnings = bag.GetWarnings().ToArray();

        Assert.HasCount(1, warnings);
        Assert.IsTrue(warnings[0].IsWarning);
    }

    [TestMethod]
    public void DiagnosticBag_ToSortedList_ShouldSortByLocationThenSeverity()
    {
        var bag = new DiagnosticBag();
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warning at 10", new TextSpan(10, 1));
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error at 0", new TextSpan(0, 1));

        var sorted = bag.ToSortedList();

        Assert.AreEqual(0, sorted[0].Location.Offset);
    }

    [TestMethod]
    public void DiagnosticBag_Clear_ShouldRemoveAllDiagnostics()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 1));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(1, 1));

        bag.Clear();

        Assert.AreEqual(0, bag.Count);
        Assert.AreEqual(0, bag.ErrorCount);
        Assert.AreEqual(0, bag.WarningCount);
        Assert.IsFalse(bag.HasErrors);
    }

    [TestMethod]
    public void DiagnosticBag_AddRange_ShouldStopWhenMaxErrorsReached()
    {
        var bag = new DiagnosticBag { MaxErrors = 1 };
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error 1", new TextSpan(0, 1)),
            Diagnostic.Error(DiagnosticCode.MQ2002_MissingToken, "error 2", new TextSpan(1, 1)),
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error 3", new TextSpan(2, 1))
        };

        bag.AddRange(diagnostics);

        Assert.AreEqual(1, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddRange_FromOtherBag_ShouldMerge()
    {
        var bag1 = new DiagnosticBag();
        bag1.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error from bag1", new TextSpan(0, 1));

        var bag2 = new DiagnosticBag();
        bag2.AddRange(bag1);

        Assert.AreEqual(1, bag2.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_Enumerate_ShouldReturnAllDiagnostics()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 1));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(1, 1));

        var count = 0;
        foreach (var _ in bag)
            count++;

        Assert.AreEqual(2, count);
    }

    #endregion

    #region Diagnostic Additional Branch Coverage

    [TestMethod]
    public void Diagnostic_IsError_ShouldReturnTrueForErrors()
    {
        var error = Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 5));

        Assert.IsTrue(error.IsError);
        Assert.IsFalse(error.IsWarning);
    }

    [TestMethod]
    public void Diagnostic_IsWarning_ShouldReturnTrueForWarnings()
    {
        var warning = Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(0, 5));

        Assert.IsFalse(warning.IsError);
        Assert.IsTrue(warning.IsWarning);
    }

    [TestMethod]
    public void Diagnostic_ToDetailedString_Basic_ShouldFormatCorrectly()
    {
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "unexpected token",
            new TextSpan(0, 5));

        var result = diagnostic.ToDetailedString();

        Assert.Contains("error", result);
        Assert.Contains("unexpected token", result);
        Assert.Contains("-->", result);
    }

    [TestMethod]
    public void Diagnostic_ToDetailedString_WithContextSnippet_ShouldIncludeSnippet()
    {
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticSeverity.Error,
            "unexpected token",
            new SourceLocation(0, 1, 1),
            new SourceLocation(5, 1, 6),
            "SELECT bad");

        var result = diagnostic.ToDetailedString();

        Assert.Contains("SELECT bad", result);
    }

    [TestMethod]
    public void Diagnostic_ToDetailedString_WithRelatedInfo_ShouldIncludeNotes()
    {
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "error",
            new TextSpan(0, 5))
            .WithRelatedInfo("see also: something");

        var result = diagnostic.ToDetailedString();

        Assert.Contains("note: see also: something", result);
    }

    [TestMethod]
    public void Diagnostic_ToDetailedString_WithSuggestedFix_ShouldIncludeHelp()
    {
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "error",
            new TextSpan(0, 5))
            .WithSuggestedFix(DiagnosticAction.QuickFix("try this", new TextSpan(0, 5), "fix"));

        var result = diagnostic.ToDetailedString();

        Assert.Contains("help: try this", result);
    }

    [TestMethod]
    public void Diagnostic_ToString_ShouldFormatCorrectly()
    {
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "test message",
            new TextSpan(0, 5));

        var result = diagnostic.ToString();

        Assert.Contains("error", result);
        Assert.Contains("test message", result);
    }

    [TestMethod]
    public void Diagnostic_Info_FactoryMethod_ShouldCreateInfoDiagnostic()
    {
        var diagnostic = Diagnostic.Info(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "info message",
            new TextSpan(0, 5));

        Assert.AreEqual(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.IsFalse(diagnostic.IsError);
        Assert.IsFalse(diagnostic.IsWarning);
    }

    [TestMethod]
    public void Diagnostic_Error_WithSourceLocation_ShouldUseLocations()
    {
        var location = new SourceLocation(10, 3, 5);
        var endLocation = new SourceLocation(15, 3, 10);
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "error",
            location,
            endLocation);

        Assert.AreEqual(10, diagnostic.Location.Offset);
        Assert.AreEqual(15, diagnostic.EndLocation.Offset);
    }

    [TestMethod]
    public void Diagnostic_Warning_WithSourceLocation_ShouldUseLocations()
    {
        var location = new SourceLocation(10, 3, 5);
        var diagnostic = Diagnostic.Warning(
            DiagnosticCode.MQ5001_UnusedAlias,
            "warning",
            location);

        Assert.AreEqual(10, diagnostic.Location.Offset);
        Assert.AreEqual(10, diagnostic.EndLocation.Offset);
    }

    [TestMethod]
    public void Diagnostic_Info_WithSourceLocation_ShouldUseLocations()
    {
        var location = new SourceLocation(10, 3, 5);
        var diagnostic = Diagnostic.Info(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "info",
            location);

        Assert.AreEqual(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.AreEqual(10, diagnostic.Location.Offset);
    }

    #endregion
}
