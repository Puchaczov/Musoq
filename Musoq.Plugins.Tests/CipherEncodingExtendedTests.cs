using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for cipher and encoding methods to improve branch coverage.
///     Tests Rot13, Rot47, ToMorse, FromMorse, ToUnicodeEscape, FromUnicodeEscape, ToBinaryString, FromBinaryString.
/// </summary>
[TestClass]
public class CipherEncodingExtendedTests : LibraryBaseBaseTests
{
    #region Rot13 Tests

    [TestMethod]
    public void Rot13_Null_ReturnsNull()
    {
        Assert.IsNull(Library.Rot13(null));
    }

    [TestMethod]
    public void Rot13_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.Rot13(string.Empty));
    }

    [TestMethod]
    public void Rot13_Lowercase_RotatesCorrectly()
    {
        Assert.AreEqual("uryyb", Library.Rot13("hello"));
    }

    [TestMethod]
    public void Rot13_Uppercase_RotatesCorrectly()
    {
        Assert.AreEqual("URYYB", Library.Rot13("HELLO"));
    }

    [TestMethod]
    public void Rot13_Mixed_RotatesLettersOnly()
    {
        Assert.AreEqual("Uryyb Jbeyq 123!", Library.Rot13("Hello World 123!"));
    }

    [TestMethod]
    public void Rot13_DoubleApplication_ReturnsOriginal()
    {
        var original = "Hello World";
        var encoded = Library.Rot13(original);
        var decoded = Library.Rot13(encoded);
        Assert.AreEqual(original, decoded);
    }

    [TestMethod]
    public void Rot13_NonLetters_Unchanged()
    {
        Assert.AreEqual("12345 !@#$%", Library.Rot13("12345 !@#$%"));
    }

    #endregion

    #region Rot47 Tests

    [TestMethod]
    public void Rot47_Null_ReturnsNull()
    {
        Assert.IsNull(Library.Rot47(null));
    }

    [TestMethod]
    public void Rot47_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.Rot47(string.Empty));
    }

    [TestMethod]
    public void Rot47_PrintableAscii_RotatesCorrectly()
    {
        var result = Library.Rot47("Hello");
        Assert.IsNotNull(result);
        Assert.AreNotEqual("Hello", result);
    }

    [TestMethod]
    public void Rot47_DoubleApplication_ReturnsOriginal()
    {
        var original = "Hello World 123!";
        var encoded = Library.Rot47(original);
        var decoded = Library.Rot47(encoded);
        Assert.AreEqual(original, decoded);
    }

    [TestMethod]
    public void Rot47_ControlChars_Unchanged()
    {
        var input = "\t\n";
        Assert.AreEqual(input, Library.Rot47(input));
    }

    [TestMethod]
    public void Rot47_SpaceUnchanged()
    {
        Assert.AreEqual(" ", Library.Rot47(" "));
    }

    #endregion

    #region ToMorse Tests

    [TestMethod]
    public void ToMorse_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToMorse(null));
    }

    [TestMethod]
    public void ToMorse_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToMorse(string.Empty));
    }

    [TestMethod]
    public void ToMorse_SingleLetter_ReturnsMorse()
    {
        Assert.AreEqual(".-", Library.ToMorse("A"));
    }

    [TestMethod]
    public void ToMorse_HelloWorld_ReturnsMorse()
    {
        var result = Library.ToMorse("HELLO");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("...."));
    }

    [TestMethod]
    public void ToMorse_Lowercase_ConvertedToUpper()
    {
        var upper = Library.ToMorse("A");
        var lower = Library.ToMorse("a");
        Assert.AreEqual(upper, lower);
    }

    [TestMethod]
    public void ToMorse_Numbers_ConvertsCorrectly()
    {
        Assert.AreEqual(".---- ..--- ...--", Library.ToMorse("123"));
    }

    [TestMethod]
    public void ToMorse_Space_UsesSlash()
    {
        Assert.AreEqual(".- / -...", Library.ToMorse("A B"));
    }

    [TestMethod]
    public void ToMorse_UnknownChar_Skipped()
    {
        var result = Library.ToMorse("A@B");
        Assert.IsNotNull(result);

        Assert.AreEqual(".- -...", result);
    }

    #endregion

    #region FromMorse Tests

    [TestMethod]
    public void FromMorse_Null_ReturnsNull()
    {
        Assert.IsNull(Library.FromMorse(null));
    }

    [TestMethod]
    public void FromMorse_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.FromMorse(string.Empty));
    }

    [TestMethod]
    public void FromMorse_SingleCode_ReturnsLetter()
    {
        Assert.AreEqual("A", Library.FromMorse(".-"));
    }

    [TestMethod]
    public void FromMorse_MultipleCodes_ReturnsWord()
    {
        Assert.AreEqual("HI", Library.FromMorse(".... .."));
    }

    [TestMethod]
    public void FromMorse_WithSlash_ReturnsSpace()
    {
        Assert.AreEqual("A B", Library.FromMorse(".- / -..."));
    }

    [TestMethod]
    public void FromMorse_UnknownCode_Skipped()
    {
        var result = Library.FromMorse(".- ........ -...");
        Assert.IsNotNull(result);
    }

    #endregion

    #region ToUnicodeEscape Tests

    [TestMethod]
    public void ToUnicodeEscape_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToUnicodeEscape(null));
    }

    [TestMethod]
    public void ToUnicodeEscape_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToUnicodeEscape(string.Empty));
    }

    [TestMethod]
    public void ToUnicodeEscape_Ascii_ReturnsEscaped()
    {
        Assert.AreEqual("\\u0048\\u0069", Library.ToUnicodeEscape("Hi"));
    }

    [TestMethod]
    public void ToUnicodeEscape_SingleChar_ReturnsEscaped()
    {
        Assert.AreEqual("\\u0041", Library.ToUnicodeEscape("A"));
    }

    #endregion

    #region FromUnicodeEscape Tests

    [TestMethod]
    public void FromUnicodeEscape_Null_ReturnsNull()
    {
        Assert.IsNull(Library.FromUnicodeEscape(null));
    }

    [TestMethod]
    public void FromUnicodeEscape_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.FromUnicodeEscape(string.Empty));
    }

    [TestMethod]
    public void FromUnicodeEscape_ValidEscape_ReturnsDecoded()
    {
        Assert.AreEqual("Hi", Library.FromUnicodeEscape("\\u0048\\u0069"));
    }

    [TestMethod]
    public void FromUnicodeEscape_NoEscapes_ReturnsOriginal()
    {
        Assert.AreEqual("Hello", Library.FromUnicodeEscape("Hello"));
    }

    [TestMethod]
    public void FromUnicodeEscape_InvalidEscape_ReturnsOriginal()
    {
        Assert.AreEqual("\\uXXXX", Library.FromUnicodeEscape("\\uXXXX"));
    }

    [TestMethod]
    public void FromUnicodeEscape_MixedContent_DecodesProperly()
    {
        Assert.AreEqual("Hello A World", Library.FromUnicodeEscape("Hello \\u0041 World"));
    }

    #endregion

    #region ToBinaryString Tests

    [TestMethod]
    public void ToBinaryString_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToBinaryString(null));
    }

    [TestMethod]
    public void ToBinaryString_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToBinaryString(string.Empty));
    }

    [TestMethod]
    public void ToBinaryString_SingleChar_ReturnsBinary()
    {
        Assert.AreEqual("01000001", Library.ToBinaryString("A"));
    }

    [TestMethod]
    public void ToBinaryString_MultipleChars_SpaceSeparated()
    {
        var result = Library.ToBinaryString("AB");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains(" "));
    }

    #endregion

    #region FromBinaryString Tests

    [TestMethod]
    public void FromBinaryString_Null_ReturnsNull()
    {
        Assert.IsNull(Library.FromBinaryString(null));
    }

    [TestMethod]
    public void FromBinaryString_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.FromBinaryString(string.Empty));
    }

    [TestMethod]
    public void FromBinaryString_ValidBinary_ReturnsDecoded()
    {
        Assert.AreEqual("A", Library.FromBinaryString("01000001"));
    }

    [TestMethod]
    public void FromBinaryString_SpaceSeparated_ReturnsDecoded()
    {
        Assert.AreEqual("AB", Library.FromBinaryString("01000001 01000010"));
    }

    [TestMethod]
    public void FromBinaryString_InvalidBinary_ReturnsNull()
    {
        Assert.IsNull(Library.FromBinaryString("not binary"));
    }

    #endregion
}
