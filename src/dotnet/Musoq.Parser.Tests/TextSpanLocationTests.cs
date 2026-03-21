using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

/// <summary>
///     Branch coverage tests for TextSpan, SourceLocation, TextEdit, and SourceText.
/// </summary>
[TestClass]
public class TextSpanLocationTests
{
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
}
