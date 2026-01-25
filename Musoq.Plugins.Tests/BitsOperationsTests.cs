using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class BitsOperationsTests : LibraryBaseBaseTests
{
    #region ShiftLeft Tests

    [TestMethod]
    public void ShiftLeft_Byte_ShouldReturnCorrectResult()
    {
        // Arrange
        byte? value = 4; // 00000100
        var shift = 2;

        // Act
        var result = Library.ShiftLeft(value, shift);

        // Assert
        Assert.AreEqual((byte)16, result); // 00010000
    }

    [TestMethod]
    public void ShiftLeft_Byte_WhenNull_ShouldReturnNull()
    {
        // Arrange
        byte? value = null;
        var shift = 2;

        // Act
        var result = Library.ShiftLeft(value, shift);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ShiftLeft_Short_ShouldReturnCorrectResult()
    {
        // Arrange
        short? value = 4;
        var shift = 2;

        // Act
        var result = Library.ShiftLeft(value, shift);

        // Assert
        Assert.AreEqual((short)16, result);
    }

    [TestMethod]
    public void ShiftLeft_Short_WhenNull_ShouldReturnNull()
    {
        // Arrange
        short? value = null;
        var shift = 2;

        // Act
        var result = Library.ShiftLeft(value, shift);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ShiftLeft_Int_ShouldReturnCorrectResult()
    {
        // Arrange
        int? value = 4;
        var shift = 2;

        // Act
        var result = Library.ShiftLeft(value, shift);

        // Assert
        Assert.AreEqual(16, result);
    }

    [TestMethod]
    public void ShiftLeft_Int_WhenNull_ShouldReturnNull()
    {
        // Arrange
        int? value = null;
        var shift = 2;

        // Act
        var result = Library.ShiftLeft(value, shift);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ShiftLeft_Long_ShouldReturnCorrectResult()
    {
        // Arrange
        long? value = 4L;
        var shift = 2;

        // Act
        var result = Library.ShiftLeft(value, shift);

        // Assert
        Assert.AreEqual(16L, result);
    }

    [TestMethod]
    public void ShiftLeft_Long_WhenNull_ShouldReturnNull()
    {
        // Arrange
        long? value = null;
        var shift = 2;

        // Act
        var result = Library.ShiftLeft(value, shift);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region ShiftRight Tests

    [TestMethod]
    public void ShiftRight_Byte_ShouldReturnCorrectResult()
    {
        // Arrange
        byte? value = 16; // 00010000
        var shift = 2;

        // Act
        var result = Library.ShiftRight(value, shift);

        // Assert
        Assert.AreEqual((byte)4, result); // 00000100
    }

    [TestMethod]
    public void ShiftRight_Byte_WhenNull_ShouldReturnNull()
    {
        // Arrange
        byte? value = null;
        var shift = 2;

        // Act
        var result = Library.ShiftRight(value, shift);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ShiftRight_Int_ShouldReturnCorrectResult()
    {
        // Arrange
        int? value = 16;
        var shift = 2;

        // Act
        var result = Library.ShiftRight(value, shift);

        // Assert
        Assert.AreEqual(4, result);
    }

    [TestMethod]
    public void ShiftRight_Int_WhenNull_ShouldReturnNull()
    {
        // Arrange
        int? value = null;
        var shift = 2;

        // Act
        var result = Library.ShiftRight(value, shift);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region Not Tests

    [TestMethod]
    public void Not_Byte_ShouldReturnCorrectResult()
    {
        // Arrange
        byte? value = 0b00001111; // 15

        // Act
        var result = Library.Not(value);

        // Assert
        Assert.AreEqual((byte)0b11110000, result); // 240
    }

    [TestMethod]
    public void Not_Byte_WhenNull_ShouldReturnNull()
    {
        // Arrange
        byte? value = null;

        // Act
        var result = Library.Not(value);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Not_Int_ShouldReturnCorrectResult()
    {
        // Arrange
        int? value = 0b00001111; // 15

        // Act
        var result = Library.Not(value);

        // Assert
        Assert.AreEqual(~15, result);
    }

    [TestMethod]
    public void Not_Int_WhenNull_ShouldReturnNull()
    {
        // Arrange
        int? value = null;

        // Act
        var result = Library.Not(value);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region And Tests

    [TestMethod]
    public void And_Byte_ShouldReturnCorrectResult()
    {
        // Arrange
        byte? left = 0b11110000; // 240
        byte? right = 0b00001111; // 15

        // Act
        var result = Library.And(left, right);

        // Assert
        Assert.AreEqual((byte)0, result); // No common bits
    }

    [TestMethod]
    public void And_Byte_WhenOneNull_ShouldReturnNull()
    {
        // Arrange
        byte? left = 240;
        byte? right = null;

        // Act
        var result = Library.And(left, right);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Int_ShouldReturnCorrectResult()
    {
        // Arrange
        int? left = 0b11110000; // 240
        int? right = 0b11111111; // 255

        // Act
        var result = Library.And(left, right);

        // Assert
        Assert.AreEqual(240, result);
    }

    [TestMethod]
    public void And_Int_WhenBothNull_ShouldReturnNull()
    {
        // Arrange
        int? left = null;
        int? right = null;

        // Act
        var result = Library.And(left, right);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region Or Tests

    [TestMethod]
    public void Or_Byte_ShouldReturnCorrectResult()
    {
        // Arrange
        byte? left = 0b11110000; // 240
        byte? right = 0b00001111; // 15

        // Act
        var result = Library.Or(left, right);

        // Assert
        Assert.AreEqual((byte)255, result); // All bits set
    }

    [TestMethod]
    public void Or_Byte_WhenOneNull_ShouldReturnNull()
    {
        // Arrange
        byte? left = null;
        byte? right = 15;

        // Act
        var result = Library.Or(left, right);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Int_ShouldReturnCorrectResult()
    {
        // Arrange
        int? left = 0b11110000; // 240
        int? right = 0b00001111; // 15

        // Act
        var result = Library.Or(left, right);

        // Assert
        Assert.AreEqual(255, result);
    }

    #endregion

    #region Xor Tests

    [TestMethod]
    public void Xor_Byte_ShouldReturnCorrectResult()
    {
        // Arrange
        byte? left = 0b11110000; // 240
        byte? right = 0b00001111; // 15

        // Act
        var result = Library.Xor(left, right);

        // Assert
        Assert.AreEqual((byte)255, result); // All bits different
    }

    [TestMethod]
    public void Xor_Byte_WhenSameValues_ShouldReturnZero()
    {
        // Arrange
        byte? left = 0b11110000; // 240
        byte? right = 0b11110000; // 240

        // Act
        var result = Library.Xor(left, right);

        // Assert
        Assert.AreEqual((byte)0, result); // Same bits cancel out
    }

    [TestMethod]
    public void Xor_Byte_WhenOneNull_ShouldReturnNull()
    {
        // Arrange
        byte? left = 240;
        byte? right = null;

        // Act
        var result = Library.Xor(left, right);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Int_ShouldReturnCorrectResult()
    {
        // Arrange
        int? left = 0b11110000; // 240
        int? right = 0b00001111; // 15

        // Act
        var result = Library.Xor(left, right);

        // Assert
        Assert.AreEqual(255, result);
    }

    #endregion
}
