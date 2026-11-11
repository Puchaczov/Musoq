using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class FromBytesMethodsTests
{
    #region FromBytesToChar Tests

    [TestMethod]
    public void FromBytesToChar_ShouldReturnChar()
    {
        var bytes = BitConverter.GetBytes('A');
        var result = Library.FromBytesToChar(bytes);
        Assert.AreEqual('A', result);
    }

    [TestMethod]
    public void FromBytesToChar_WithUnicodeChar_ShouldReturnCorrectChar()
    {
        var bytes = BitConverter.GetBytes('世');
        var result = Library.FromBytesToChar(bytes);
        Assert.AreEqual('世', result);
    }

    [TestMethod]
    public void FromBytesToChar_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToChar(null!);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToChar_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToChar(new byte[1]);
        Assert.IsFalse(result.HasValue);
    }

    #endregion

    #region FromBytesToHalf Tests

    [TestMethod]
    public void FromBytesToHalf_ShouldReturnHalf()
    {
        var value = (Half)123.5;
        var bytes = BitConverter.GetBytes(value);
        var result = Library.FromBytesToHalf(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(value, result.GetValueOrDefault());
    }

    [TestMethod]
    public void FromBytesToHalf_WithNegativeValue_ShouldReturnNegativeHalf()
    {
        var value = (Half)(-456.75);
        var bytes = BitConverter.GetBytes(value);
        var result = Library.FromBytesToHalf(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(value, result.GetValueOrDefault());
    }

    [TestMethod]
    public void FromBytesToHalf_WithZero_ShouldReturnZero()
    {
        var value = (Half)0.0;
        var bytes = BitConverter.GetBytes(value);
        var result = Library.FromBytesToHalf(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(value, result.GetValueOrDefault());
    }

    [TestMethod]
    public void FromBytesToHalf_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToHalf(null!);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToHalf_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToHalf(new byte[1]);
        Assert.IsFalse(result.HasValue);
    }

    #endregion

    #region Padding Tests

    [TestMethod]
    public void FromBytesToInt32_WithPadding_SingleByte_ShouldPadAndConvert()
    {
        // Arrange - single byte [5] should become [5, 0, 0, 0] in little-endian
        var bytes = new byte[] { 5 };

        // Act
        var result = Library.FromBytesToInt32(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(5, result.GetValueOrDefault()); // 0x00000005
    }

    [TestMethod]
    public void FromBytesToInt32_WithPadding_TwoBytes_ShouldPadAndConvert()
    {
        // Arrange - [1, 2] should become [1, 2, 0, 0] in little-endian
        var bytes = new byte[] { 1, 2 };

        // Act
        var result = Library.FromBytesToInt32(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(513, result.GetValueOrDefault()); // 0x00000201 = 513
    }

    [TestMethod]
    public void FromBytesToInt32_WithoutPadding_InsufficientBytes_ShouldReturnNull()
    {
        // Arrange
        var bytes = new byte[] { 1, 2 };

        // Act
        var result = Library.FromBytesToInt32(bytes, false);

        // Assert
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt16_WithPadding_SingleByte_ShouldPadAndConvert()
    {
        // Arrange - [10] should become [10, 0]
        var bytes = new byte[] { 10 };

        // Act
        var result = Library.FromBytesToInt16(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual<short>(10, result.GetValueOrDefault());
    }

    [TestMethod]
    public void FromBytesToUInt32_WithPadding_ThreeBytes_ShouldPadAndConvert()
    {
        // Arrange - [0xFF, 0xFF, 0xFF] should become [0xFF, 0xFF, 0xFF, 0x00]
        var bytes = new byte[] { 0xFF, 0xFF, 0xFF };

        // Act
        var result = Library.FromBytesToUInt32(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(16777215U, result.GetValueOrDefault()); // 0x00FFFFFF
    }

    [TestMethod]
    public void FromBytesToInt64_WithPadding_FourBytes_ShouldPadAndConvert()
    {
        // Arrange - [1, 2, 3, 4] should become [1, 2, 3, 4, 0, 0, 0, 0]
        var bytes = new byte[] { 1, 2, 3, 4 };

        // Act
        var result = Library.FromBytesToInt64(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(67305985L, result.GetValueOrDefault()); // 0x0000000004030201
    }

    [TestMethod]
    public void FromBytesToBool_WithPadding_EmptyArray_ShouldReturnFalse()
    {
        // Arrange - empty array should become [0]
        var bytes = Array.Empty<byte>();

        // Act
        var result = Library.FromBytesToBool(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.IsFalse(result.GetValueOrDefault());
    }

    [TestMethod]
    public void FromBytesToChar_WithPadding_SingleByte_ShouldPadAndConvert()
    {
        // Arrange - [65] should become [65, 0] which is 'A' in UTF-16
        var bytes = new byte[] { 65 };

        // Act
        var result = Library.FromBytesToChar(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual('A', result.GetValueOrDefault());
    }

    [TestMethod]
    public void FromBytesToFloat_WithPadding_TwoBytes_ShouldPadAndConvert()
    {
        // Arrange - [0, 0] should become [0, 0, 0, 0] which is 0.0f
        var bytes = "\u0000\u0000"u8.ToArray();

        // Act
        var result = Library.FromBytesToFloat(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(0.0f, result.GetValueOrDefault(), 0.001f);
    }

    [TestMethod]
    public void FromBytesToDouble_WithPadding_FourBytes_ShouldPadAndConvert()
    {
        // Arrange - [0, 0, 0, 0] should become [0, 0, 0, 0, 0, 0, 0, 0] which is 0.0
        var bytes = "\u0000\u0000\u0000\u0000"u8.ToArray();

        // Act
        var result = Library.FromBytesToDouble(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(0.0, result.GetValueOrDefault(), 0.000001);
    }

    [TestMethod]
    public void FromBytesToHalf_WithPadding_SingleByte_ShouldPadAndConvert()
    {
        // Arrange - [0] should become [0, 0] which is 0.0 as Half
        var bytes = new byte[] { 0 };

        // Act
        var result = Library.FromBytesToHalf(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual((Half)0.0, result.GetValueOrDefault());
    }

    [TestMethod]
    public void FromBytesToUInt16_WithPadding_SingleByte_ShouldPadAndConvert()
    {
        // Arrange - [255] should become [255, 0]
        var bytes = new byte[] { 255 };

        // Act
        var result = Library.FromBytesToUInt16(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual<ushort>(255, result.GetValueOrDefault());
    }

    [TestMethod]
    public void FromBytesToUInt64_WithPadding_SingleByte_ShouldPadAndConvert()
    {
        // Arrange - [42] should become [42, 0, 0, 0, 0, 0, 0, 0]
        var bytes = new byte[] { 42 };

        // Act
        var result = Library.FromBytesToUInt64(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(42UL, result.GetValueOrDefault());
    }

    [TestMethod]
    public void FromBytesToInt32_WithPadding_NullArray_ShouldReturnNull()
    {
        // Arrange
        // Act
        var result = Library.FromBytesToInt32(null!, true);

        // Assert
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt32_WithPadding_ExactSize_ShouldNotPad()
    {
        // Arrange - exact size should work without padding
        var bytes = BitConverter.GetBytes(12345);

        // Act
        var result = Library.FromBytesToInt32(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(12345, result.GetValueOrDefault());
    }

    [TestMethod]
    public void FromBytesToInt32_WithPadding_MoreThanNeeded_ShouldUseOriginal()
    {
        // Arrange - more bytes than needed should use first 4 bytes
        var bytes = new byte[] { 1, 0, 0, 0, 99, 99 };

        // Act
        var result = Library.FromBytesToInt32(bytes, true);

        // Assert
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(1, result.GetValueOrDefault()); // Only uses first 4 bytes
    }

    #endregion
}
