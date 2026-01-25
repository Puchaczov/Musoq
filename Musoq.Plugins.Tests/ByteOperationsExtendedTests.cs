using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for byte conversion methods in LibraryBaseBytes.cs to improve branch coverage
/// </summary>
[TestClass]
public class ByteOperationsExtendedTests : LibraryBaseBaseTests
{
    #region GetBytes (string) Tests

    [TestMethod]
    public void GetBytes_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((string?)null));
    }

    [TestMethod]
    public void GetBytes_ValidString_ReturnsBytes()
    {
        var result = Library.GetBytes("Hello");
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("Hello"), result);
    }

    [TestMethod]
    public void GetBytes_EmptyString_ReturnsEmptyBytes()
    {
        var result = Library.GetBytes("");
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Length);
    }

    #endregion

    #region GetBytes (string, length, offset) Tests

    [TestMethod]
    public void GetBytes_StringWithOffset_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes(null, 5, 0));
    }

    [TestMethod]
    public void GetBytes_StringWithOffset_ValidString_ReturnsBytes()
    {
        var result = Library.GetBytes("Hello World", 5, 6);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("World"), result);
    }

    #endregion

    #region GetBytes (char) Tests

    [TestMethod]
    public void GetBytes_NullChar_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((char?)null));
    }

    [TestMethod]
    public void GetBytes_ValidChar_ReturnsBytes()
    {
        var result = Library.GetBytes('A');
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(BitConverter.GetBytes('A'), result);
    }

    #endregion

    #region GetBytes (bool) Tests

    [TestMethod]
    public void GetBytes_NullBool_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((bool?)null));
    }

    [TestMethod]
    public void GetBytes_TrueBool_ReturnsBytes()
    {
        var result = Library.GetBytes(true);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(BitConverter.GetBytes(true), result);
    }

    [TestMethod]
    public void GetBytes_FalseBool_ReturnsBytes()
    {
        var result = Library.GetBytes(false);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(BitConverter.GetBytes(false), result);
    }

    #endregion

    #region GetBytes (long) Tests

    [TestMethod]
    public void GetBytes_NullLong_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((long?)null));
    }

    [TestMethod]
    public void GetBytes_ValidLong_ReturnsBytes()
    {
        var result = Library.GetBytes(123456789L);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(BitConverter.GetBytes(123456789L), result);
    }

    [TestMethod]
    public void GetBytes_NegativeLong_ReturnsBytes()
    {
        var result = Library.GetBytes(-123456789L);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(BitConverter.GetBytes(-123456789L), result);
    }

    #endregion

    #region GetBytes (int) Tests

    [TestMethod]
    public void GetBytes_NullInt_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((int?)null));
    }

    [TestMethod]
    public void GetBytes_ValidInt_ReturnsBytes()
    {
        var value = 12345;
        var result = Library.GetBytes(value);
        Assert.IsNotNull(result);

        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void GetBytes_NegativeInt_ReturnsBytes()
    {
        var value = -12345;
        var result = Library.GetBytes(value);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    #endregion

    #region GetBytes (short) Tests

    [TestMethod]
    public void GetBytes_NullShort_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((short?)null));
    }

    [TestMethod]
    public void GetBytes_ValidShort_ReturnsBytes()
    {
        var result = Library.GetBytes(1234);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(BitConverter.GetBytes((short)1234), result);
    }

    #endregion

    #region GetBytes (ulong) Tests

    [TestMethod]
    public void GetBytes_NullULong_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((ulong?)null));
    }

    [TestMethod]
    public void GetBytes_ValidULong_ReturnsBytes()
    {
        var result = Library.GetBytes((ulong)123456789);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(BitConverter.GetBytes((ulong)123456789), result);
    }

    #endregion

    #region GetBytes (ushort) Tests

    [TestMethod]
    public void GetBytes_NullUShort_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((ushort?)null));
    }

    [TestMethod]
    public void GetBytes_ValidUShort_ReturnsBytes()
    {
        var result = Library.GetBytes((ushort)1234);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(BitConverter.GetBytes((ushort)1234), result);
    }

    #endregion

    #region GetBytes (uint) Tests

    [TestMethod]
    public void GetBytes_NullUInt_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((uint?)null));
    }

    [TestMethod]
    public void GetBytes_ValidUInt_ReturnsBytes()
    {
        var result = Library.GetBytes((uint)12345);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(BitConverter.GetBytes((uint)12345), result);
    }

    #endregion

    #region GetBytes (decimal) Tests

    [TestMethod]
    public void GetBytes_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((decimal?)null));
    }

    [TestMethod]
    public void GetBytes_ValidDecimal_ReturnsBytes()
    {
        var result = Library.GetBytes(12345.67m);
        Assert.IsNotNull(result);
        Assert.AreEqual(16, result.Length);
    }

    [TestMethod]
    public void GetBytes_NegativeDecimal_ReturnsBytes()
    {
        var result = Library.GetBytes(-12345.67m);
        Assert.IsNotNull(result);
        Assert.AreEqual(16, result.Length);
    }

    #endregion

    #region GetBytes (double) Tests

    [TestMethod]
    public void GetBytes_NullDouble_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((double?)null));
    }

    [TestMethod]
    public void GetBytes_ValidDouble_ReturnsBytes()
    {
        var result = Library.GetBytes(123.456);
        Assert.IsNotNull(result);
        Assert.AreEqual(8, result.Length);
    }

    [TestMethod]
    public void GetBytes_NegativeDouble_ReturnsBytes()
    {
        var result = Library.GetBytes(-123.456);
        Assert.IsNotNull(result);
        Assert.AreEqual(8, result.Length);
    }

    #endregion

    #region GetBytes (float) Tests

    [TestMethod]
    public void GetBytes_NullFloat_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((float?)null));
    }

    [TestMethod]
    public void GetBytes_ValidFloat_ReturnsBytes()
    {
        var result = Library.GetBytes(123.456f);
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(BitConverter.GetBytes(123.456f), result);
    }

    #endregion

    #region ToHex Tests for Bytes

    [TestMethod]
    public void ToHex_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.ToHex(null));
    }

    [TestMethod]
    public void ToHex_EmptyBytes_ReturnsEmptyString()
    {
        Assert.AreEqual("", Library.ToHex(Array.Empty<byte>()));
    }

    [TestMethod]
    public void ToHex_ValidBytes_ReturnsHex()
    {
        var result = Library.ToHex(new byte[] { 255, 0, 128 });
        Assert.AreEqual("FF0080", result);
    }

    [TestMethod]
    public void ToHex_WithDelimiter_ReturnsHexWithDelimiter()
    {
        var result = Library.ToHex(new byte[] { 255, 0, 128 }, ":");

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains(":"));
    }

    #endregion

    #region ToHex for Various Types

    [TestMethod]
    public void ToHex_Int_ReturnsHex()
    {
        var result = Library.ToHex(255);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith("FF"));
    }

    [TestMethod]
    public void ToHex_Byte_ReturnsHex()
    {
        var result = Library.ToHex((byte)255);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("FF"));
    }

    [TestMethod]
    public void ToHex_Long_ReturnsHex()
    {
        var result = Library.ToHex(-1L);
        Assert.IsNotNull(result);
        Assert.AreEqual(16, result.Length);
    }

    [TestMethod]
    public void ToHex_Short_ReturnsHex()
    {
        var result = Library.ToHex((short)255);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("FF"));
    }

    #endregion
}
