using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public partial class LibraryBaseUtilsTests
{
    private readonly LibraryBase _library = new();

    #region Unicode Escape Tests

    [TestMethod]
    public void ToUnicodeEscape_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ToUnicodeEscape(null));
    }

    [TestMethod]
    public void ToUnicodeEscape_WhenHi_ReturnsCorrectEscape()
    {
        Assert.AreEqual("\\u0048\\u0069", _library.ToUnicodeEscape("Hi"));
    }

    [TestMethod]
    public void FromUnicodeEscape_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.FromUnicodeEscape(null));
    }

    [TestMethod]
    public void FromUnicodeEscape_WhenValidEscape_ReturnsOriginal()
    {
        Assert.AreEqual("Hi", _library.FromUnicodeEscape("\\u0048\\u0069"));
    }

    #endregion

    #region ROT13/ROT47 Tests

    [TestMethod]
    public void Rot13_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.Rot13(null));
    }

    [TestMethod]
    public void Rot13_WhenHello_ReturnsUryyb()
    {
        Assert.AreEqual("Uryyb", _library.Rot13("Hello"));
    }

    [TestMethod]
    public void Rot13_WhenAppliedTwice_ReturnsOriginal()
    {
        var original = "Hello World!";
        Assert.AreEqual(original, _library.Rot13(_library.Rot13(original)));
    }

    [TestMethod]
    public void Rot47_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.Rot47(null));
    }

    [TestMethod]
    public void Rot47_WhenAppliedTwice_ReturnsOriginal()
    {
        var original = "Hello World! 123";
        Assert.AreEqual(original, _library.Rot47(_library.Rot47(original)));
    }

    #endregion

    #region Morse Code Tests

    [TestMethod]
    public void ToMorse_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ToMorse(null));
    }

    [TestMethod]
    public void ToMorse_WhenSOS_ReturnsCorrectCode()
    {
        Assert.AreEqual("... --- ...", _library.ToMorse("SOS"));
    }

    [TestMethod]
    public void FromMorse_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.FromMorse(null));
    }

    [TestMethod]
    public void FromMorse_WhenValidCode_ReturnsText()
    {
        Assert.AreEqual("SOS", _library.FromMorse("... --- ..."));
    }

    #endregion

    #region Binary String Tests

    [TestMethod]
    public void ToBinaryString_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ToBinaryString(null));
    }

    [TestMethod]
    public void ToBinaryString_WhenHi_ReturnsCorrectBinary()
    {
        Assert.AreEqual("01001000 01101001", _library.ToBinaryString("Hi"));
    }

    [TestMethod]
    public void FromBinaryString_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.FromBinaryString(null));
    }

    [TestMethod]
    public void FromBinaryString_WhenValidBinary_ReturnsText()
    {
        Assert.AreEqual("Hi", _library.FromBinaryString("01001000 01101001"));
    }

    #endregion

    #region String Manipulation Tests

    [TestMethod]
    public void ReverseString_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ReverseString(null));
    }

    [TestMethod]
    public void ReverseString_WhenHello_ReturnsOlleh()
    {
        Assert.AreEqual("olleH", _library.ReverseString("Hello"));
    }

    [TestMethod]
    public void SplitAndTake_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.SplitAndTake(null, ",", 0));
    }

    [TestMethod]
    public void SplitAndTake_WhenValidIndex_ReturnsPart()
    {
        Assert.AreEqual("two", _library.SplitAndTake("one,two,three", ",", 1));
    }

    [TestMethod]
    public void SplitAndTake_WhenInvalidIndex_ReturnsNull()
    {
        Assert.IsNull(_library.SplitAndTake("one,two,three", ",", 10));
    }

    [TestMethod]
    public void PadLeft_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.PadLeft(null, 10));
    }

    [TestMethod]
    public void PadLeft_WithSpaces_PadsCorrectly()
    {
        Assert.AreEqual("     Hello", _library.PadLeft("Hello", 10));
    }

    [TestMethod]
    public void PadLeft_WithChar_PadsCorrectly()
    {
        Assert.AreEqual("00042", _library.PadLeft("42", 5, '0'));
    }

    [TestMethod]
    public void PadRight_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.PadRight(null, 10));
    }

    [TestMethod]
    public void PadRight_WithSpaces_PadsCorrectly()
    {
        Assert.AreEqual("Hello     ", _library.PadRight("Hello", 10));
    }

    [TestMethod]
    public void RemoveDiacritics_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.RemoveDiacritics(null));
    }

    [TestMethod]
    public void RemoveDiacritics_WhenCafe_ReturnsCafe()
    {
        Assert.AreEqual("cafe", _library.RemoveDiacritics("café"));
    }

    #endregion

    #region Hash Tests

    [TestMethod]
    public void Sha384_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.Sha384((string?)null));
    }

    [TestMethod]
    public void Sha384_WhenHello_ReturnsCorrectHash()
    {
        var hash = _library.Sha384("hello");
        Assert.IsNotNull(hash);
        Assert.AreEqual(96, hash.Length);
    }

    [TestMethod]
    public void Crc32_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.Crc32((string?)null));
    }

    [TestMethod]
    public void Crc32_WhenHello_ReturnsCorrectChecksum()
    {
        var crc = _library.Crc32("hello");
        Assert.IsNotNull(crc);
        Assert.AreEqual(8, crc.Length);
    }

    [TestMethod]
    public void HmacSha256_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.HmacSha256(null, "key"));
        Assert.IsNull(_library.HmacSha256("message", null));
    }

    [TestMethod]
    public void HmacSha256_WhenValid_ReturnsHash()
    {
        var hash = _library.HmacSha256("message", "secret");
        Assert.IsNotNull(hash);
        Assert.AreEqual(64, hash.Length);
    }

    [TestMethod]
    public void HmacSha512_WhenValid_ReturnsHash()
    {
        var hash = _library.HmacSha512("message", "secret");
        Assert.IsNotNull(hash);
        Assert.AreEqual(128, hash.Length);
    }

    [TestMethod]
    public void Sha384_ByteArray_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.Sha384((byte[]?)null));
    }

    [TestMethod]
    public void Sha384_ByteArray_WhenValid_ReturnsCorrectHash()
    {
        var hash = _library.Sha384("Hello"u8.ToArray());
        Assert.IsNotNull(hash);
        Assert.AreEqual(96, hash.Length);
    }

    [TestMethod]
    public void Crc32_ByteArray_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.Crc32((byte[]?)null));
    }

    [TestMethod]
    public void Crc32_ByteArray_WhenValid_ReturnsChecksum()
    {
        var crc = _library.Crc32("Hello"u8.ToArray());
        Assert.IsNotNull(crc);
        Assert.AreEqual(8, crc.Length);
    }

    #endregion

}
