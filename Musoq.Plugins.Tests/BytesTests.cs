using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Musoq.Plugins.Tests;

[TestClass]
public class BytesTests : LibraryBaseBaseTests
{
    #region Existing Tests

    [TestMethod]
    public void GetBytesForString()
    {
        AssertLoop("abc"u8.ToArray(), Library.GetBytes("abc")!);
        AssertLoop(BitConverter.GetBytes('a'), Library.GetBytes('a')!);
        AssertLoop(BitConverter.GetBytes(5L), Library.GetBytes(5L)!);
        AssertLoop(BitConverter.GetBytes(true), Library.GetBytes(true)!);

        AssertLoop(decimal.GetBits(5m).SelectMany(f => BitConverter.GetBytes(f)).ToArray(), Library.GetBytes(5m)!);
    }

    #endregion

    #region String Tests

    [TestMethod]
    public void GetBytes_String_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((string?)null));
    }

    [TestMethod]
    public void GetBytes_String_Empty_ReturnsEmptyArray()
    {
        var result = Library.GetBytes(string.Empty);
        Assert.IsNotNull(result);
        Assert.HasCount(0, result);
    }

    [TestMethod]
    public void GetBytes_String_WithOffsetAndLength()
    {
        var result = Library.GetBytes("Hello World", 5, 0);
        Assert.IsNotNull(result);
        AssertLoop("Hello"u8.ToArray(), result);
    }

    [TestMethod]
    public void GetBytes_String_WithOffsetAndLength_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((string?)null, 5, 0));
    }

    #endregion

    #region Char Tests

    [TestMethod]
    public void GetBytes_Char_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((char?)null));
    }

    [TestMethod]
    public void GetBytes_Char_ValidValue()
    {
        var result = Library.GetBytes('Z');
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes('Z'), result);
    }

    #endregion

    #region Bool Tests

    [TestMethod]
    public void GetBytes_Bool_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((bool?)null));
    }

    [TestMethod]
    public void GetBytes_Bool_True()
    {
        var result = Library.GetBytes(true);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes(true), result);
    }

    [TestMethod]
    public void GetBytes_Bool_False()
    {
        var result = Library.GetBytes(false);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes(false), result);
    }

    #endregion

    #region Long Tests

    [TestMethod]
    public void GetBytes_Long_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((long?)null));
    }

    [TestMethod]
    public void GetBytes_Long_ValidValue()
    {
        var result = Library.GetBytes(123456789L);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes(123456789L), result);
    }

    [TestMethod]
    public void GetBytes_Long_Negative()
    {
        var result = Library.GetBytes(-123456789L);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes(-123456789L), result);
    }

    #endregion

    #region Int Tests

    [TestMethod]
    public void GetBytes_Int_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((int?)null));
    }

    [TestMethod]
    public void GetBytes_Int_ValidValue()
    {
        int? value = 12345;
        var result = Library.GetBytes(value);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes(value.Value), result);
    }

    [TestMethod]
    public void GetBytes_Int_Negative()
    {
        int? value = -12345;
        var result = Library.GetBytes(value);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes(value.Value), result);
    }

    #endregion

    #region Short Tests

    [TestMethod]
    public void GetBytes_Short_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((short?)null));
    }

    [TestMethod]
    public void GetBytes_Short_ValidValue()
    {
        var result = Library.GetBytes((short)1234);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes((short)1234), result);
    }

    [TestMethod]
    public void GetBytes_Short_Negative()
    {
        var result = Library.GetBytes((short)-1234);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes((short)-1234), result);
    }

    #endregion

    #region ULong Tests

    [TestMethod]
    public void GetBytes_ULong_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((ulong?)null));
    }

    [TestMethod]
    public void GetBytes_ULong_ValidValue()
    {
        var result = Library.GetBytes(123456789UL);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes(123456789UL), result);
    }

    #endregion

    #region UShort Tests

    [TestMethod]
    public void GetBytes_UShort_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((ushort?)null));
    }

    [TestMethod]
    public void GetBytes_UShort_ValidValue()
    {
        var result = Library.GetBytes((ushort)1234);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes((ushort)1234), result);
    }

    #endregion

    #region UInt Tests

    [TestMethod]
    public void GetBytes_UInt_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((uint?)null));
    }

    [TestMethod]
    public void GetBytes_UInt_ValidValue()
    {
        var result = Library.GetBytes(12345u);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes(12345u), result);
    }

    #endregion

    #region Float Tests

    [TestMethod]
    public void GetBytes_Float_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((float?)null));
    }

    [TestMethod]
    public void GetBytes_Float_ValidValue()
    {
        var result = Library.GetBytes(3.14f);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes(3.14f), result);
    }

    #endregion

    #region Double Tests

    [TestMethod]
    public void GetBytes_Double_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((double?)null));
    }

    [TestMethod]
    public void GetBytes_Double_ValidValue()
    {
        var result = Library.GetBytes(3.14159);
        Assert.IsNotNull(result);
        AssertLoop(BitConverter.GetBytes(3.14159), result);
    }

    #endregion

    #region Decimal Tests

    [TestMethod]
    public void GetBytes_Decimal_Null_ReturnsNull()
    {
        Assert.IsNull(Library.GetBytes((decimal?)null));
    }

    [TestMethod]
    public void GetBytes_Decimal_ValidValue()
    {
        var result = Library.GetBytes(123.456m);
        Assert.IsNotNull(result);
        var expected = decimal.GetBits(123.456m).SelectMany(f => BitConverter.GetBytes(f)).ToArray();
        AssertLoop(expected, result);
    }

    [TestMethod]
    public void GetBytes_Decimal_Zero()
    {
        var result = Library.GetBytes(0m);
        Assert.IsNotNull(result);
        var expected = decimal.GetBits(0m).SelectMany(f => BitConverter.GetBytes(f)).ToArray();
        AssertLoop(expected, result);
    }

    [TestMethod]
    public void GetBytes_Decimal_Negative()
    {
        var result = Library.GetBytes(-123.456m);
        Assert.IsNotNull(result);
        var expected = decimal.GetBits(-123.456m).SelectMany(f => BitConverter.GetBytes(f)).ToArray();
        AssertLoop(expected, result);
    }

    #endregion

    #region Helper Methods

    private void AssertLoop(byte[] byte1, byte[] byte2)
    {
        Assert.HasCount(byte1.Length, byte2);

        for(var i = 0; i < byte1.Length; ++i)
        {
            Assert.AreEqual(byte1[i], byte2[i]);
        }
    }

    #endregion
}