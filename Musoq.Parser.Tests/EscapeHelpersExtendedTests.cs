using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Helpers;

namespace Musoq.Parser.Tests;

/// <summary>
///     Extended tests for EscapeHelpers to cover edge cases and improve branch coverage.
/// </summary>
[TestClass]
public class EscapeHelpersExtendedTests
{
    #region Hex Escape Edge Cases

    [TestMethod]
    public void Unescape_HexNullCharacter_ReturnsOriginal()
    {
        var result = @"\x00".Unescape();
        Assert.AreEqual(@"\x00", result);
    }

    [TestMethod]
    public void Unescape_HexValidCharacter_Converts()
    {
        var result = @"\x41".Unescape();
        Assert.AreEqual("A", result);
    }

    [TestMethod]
    public void Unescape_HexLowercaseValid_Converts()
    {
        var result = @"\x61".Unescape();
        Assert.AreEqual("a", result);
    }

    [TestMethod]
    public void Unescape_HexTooShort_ReturnsOriginal()
    {
        var result = @"\x4".Unescape();
        Assert.AreEqual(@"\x4", result);
    }

    [TestMethod]
    public void Unescape_HexAtEndOfString_ReturnsOriginal()
    {
        var result = @"text\x".Unescape();
        Assert.AreEqual(@"text\x", result);
    }

    [TestMethod]
    public void Unescape_HexInvalidChars_ReturnsOriginal()
    {
        var result = @"\xGG".Unescape();
        Assert.AreEqual(@"\xGG", result);
    }

    [TestMethod]
    public void Unescape_HexMixed_ConvertsValid()
    {
        var result = @"\x41\x00\x42".Unescape();

        Assert.AreEqual(@"A\x00B", result);
    }

    #endregion

    #region Unicode Escape Edge Cases

    [TestMethod]
    public void Unescape_UnicodeNullCharacter_Converts()
    {
        var result = @"\u0000".Unescape();
        Assert.AreEqual("\0", result);
    }

    [TestMethod]
    public void Unescape_UnicodeTooShort_ReturnsOriginal()
    {
        var result = @"\u123".Unescape();
        Assert.AreEqual(@"\u123", result);
    }

    [TestMethod]
    public void Unescape_UnicodeAtEndOfString_ReturnsOriginal()
    {
        var result = @"text\u".Unescape();
        Assert.AreEqual(@"text\u", result);
    }

    [TestMethod]
    public void Unescape_UnicodeOnlyTwoCharsAvailable_ReturnsOriginal()
    {
        var result = @"\u12".Unescape();
        Assert.AreEqual(@"\u12", result);
    }

    [TestMethod]
    public void Unescape_UnicodeOnlyThreeCharsAvailable_ReturnsOriginal()
    {
        var result = @"\u123".Unescape();
        Assert.AreEqual(@"\u123", result);
    }

    [TestMethod]
    public void Unescape_UnicodeInvalidHex_ReturnsOriginal()
    {
        var result = @"\uGGGG".Unescape();
        Assert.AreEqual(@"\uGGGG", result);
    }

    [TestMethod]
    public void Unescape_UnicodeMaxValue_Converts()
    {
        var result = @"\uFFFF".Unescape();
        Assert.AreEqual("\uFFFF", result);
    }

    #endregion

    #region Escape Edge Cases

    [TestMethod]
    public void Escape_EmptyString_ReturnsEmpty()
    {
        var result = string.Empty.Escape();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Escape_NullString_ReturnsNull()
    {
        string? input = null;
        var result = input.Escape();
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Escape_NoSpecialChars_ReturnsSame()
    {
        const string input = "simple text 12345";
        var result = input.Escape();
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void Escape_ControlCharacter_NotInEscapeCharsValues_ReturnsSame()
    {
        var input = "\u0001";
        var result = input.Escape();
        Assert.AreEqual("\u0001", result);
    }

    [TestMethod]
    public void Escape_ControlCharacter0x02_NotInEscapeCharsValues_ReturnsSame()
    {
        var input = "\u0002";
        var result = input.Escape();
        Assert.AreEqual("\u0002", result);
    }

    [TestMethod]
    public void Escape_ControlCharacter0x03_NotInEscapeCharsValues_ReturnsSame()
    {
        var input = "\u0003";
        var result = input.Escape();
        Assert.AreEqual("\u0003", result);
    }

    [TestMethod]
    public void Escape_ControlCharacter0x1F_NotInEscapeCharsValues_ReturnsSame()
    {
        var input = "\u001F";
        var result = input.Escape();
        Assert.AreEqual("\u001F", result);
    }

    [TestMethod]
    public void Escape_ControlCharacter0x7F_NotInEscapeCharsValues_ReturnsSame()
    {
        var input = "\u007F";
        var result = input.Escape();
        Assert.AreEqual("\u007F", result);
    }

    [TestMethod]
    public void Escape_MixedControlAndText_OnlyEscapesKnownChars()
    {
        var input = "a\u0001b\u0002c";
        var result = input.Escape();
        Assert.AreEqual("a\u0001b\u0002c", result);
    }

    [TestMethod]
    public void Escape_MixedControlAndEscapeChars_EscapesKnownCharsOnly()
    {
        var input = "a\u0001b\nc";
        var result = input.Escape();
        Assert.AreEqual("a\\u0001b\\nc", result);
    }

    [TestMethod]
    public void Escape_Backslash_Escapes()
    {
        var result = "\\".Escape();
        Assert.AreEqual(@"\\", result);
    }

    [TestMethod]
    public void Escape_SingleQuote_Escapes()
    {
        var result = "'".Escape();
        Assert.AreEqual(@"\'", result);
    }

    [TestMethod]
    public void Escape_DoubleQuote_Escapes()
    {
        var result = "\"".Escape();
        Assert.AreEqual(@"\""", result);
    }

    [TestMethod]
    public void Escape_Newline_Escapes()
    {
        var result = "\n".Escape();
        Assert.AreEqual(@"\n", result);
    }

    [TestMethod]
    public void Escape_CarriageReturn_Escapes()
    {
        var result = "\r".Escape();
        Assert.AreEqual(@"\r", result);
    }

    [TestMethod]
    public void Escape_Tab_Escapes()
    {
        var result = "\t".Escape();
        Assert.AreEqual(@"\t", result);
    }

    [TestMethod]
    public void Escape_Backspace_Escapes()
    {
        var result = "\b".Escape();
        Assert.AreEqual(@"\b", result);
    }

    [TestMethod]
    public void Escape_FormFeed_Escapes()
    {
        var result = "\f".Escape();
        Assert.AreEqual(@"\f", result);
    }

    [TestMethod]
    public void Escape_EscapeChar_Escapes()
    {
        var result = "\u001B".Escape();
        Assert.AreEqual(@"\e", result);
    }

    [TestMethod]
    public void Escape_NullChar_Escapes()
    {
        var result = "\0".Escape();
        Assert.AreEqual(@"\0", result);
    }

    #endregion

    #region Round-trip Tests

    [TestMethod]
    public void RoundTrip_AllEscapeSequences_PreservesData()
    {
        var original = "Line1\nLine2\tTabbed\rReturned\\Backslash\"Quote'Single";
        var escaped = original.Escape();
        var unescaped = escaped.Unescape();
        Assert.AreEqual(original, unescaped);
    }

    [TestMethod]
    public void RoundTrip_KnownEscapeCharacters_PreservesData()
    {
        var original = "\\\'\"\n\r\t\b\f\u001B\0";
        var escaped = original.Escape();
        var unescaped = escaped.Unescape();
        Assert.AreEqual(original, unescaped);
    }

    #endregion

    #region Boundary Conditions

    [TestMethod]
    public void Unescape_BackslashAtEnd_PreservesBackslash()
    {
        var result = @"text\".Unescape();
        Assert.AreEqual(@"text\", result);
    }

    [TestMethod]
    public void Unescape_OnlyBackslash_PreservesBackslash()
    {
        var result = @"\".Unescape();
        Assert.AreEqual(@"\", result);
    }

    [TestMethod]
    public void Unescape_UnknownEscapeSequence_PreservesBackslashAndChar()
    {
        var result = @"\q".Unescape();
        Assert.AreEqual(@"\q", result);
    }

    [TestMethod]
    public void Unescape_MultipleUnknownEscapes_PreservesAll()
    {
        var result = @"\q\w\y\z".Unescape();
        Assert.AreEqual(@"\q\w\y\z", result);
    }

    #endregion
}
