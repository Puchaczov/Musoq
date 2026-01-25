using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for bit operations covering all numeric type overloads
///     to improve branch coverage.
/// </summary>
[TestClass]
public class BitsOperationsExtendedTests : LibraryBaseBaseTests
{
    #region ShiftLeft Extended Tests

    [TestMethod]
    public void ShiftLeft_SByte_ShouldReturnCorrectResult()
    {
        sbyte? value = 4;
        var result = Library.ShiftLeft(value, 2);
        Assert.AreEqual((sbyte)16, result);
    }

    [TestMethod]
    public void ShiftLeft_SByte_WhenNull_ShouldReturnNull()
    {
        sbyte? value = null;
        var result = Library.ShiftLeft(value, 2);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ShiftLeft_UShort_ShouldReturnCorrectResult()
    {
        ushort? value = 4;
        var result = Library.ShiftLeft(value, 2);
        Assert.AreEqual((ushort)16, result);
    }

    [TestMethod]
    public void ShiftLeft_UShort_WhenNull_ShouldReturnNull()
    {
        ushort? value = null;
        var result = Library.ShiftLeft(value, 2);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ShiftLeft_UInt_ShouldReturnCorrectResult()
    {
        uint? value = 4u;
        var result = Library.ShiftLeft(value, 2);
        Assert.AreEqual(16u, result);
    }

    [TestMethod]
    public void ShiftLeft_UInt_WhenNull_ShouldReturnNull()
    {
        uint? value = null;
        var result = Library.ShiftLeft(value, 2);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ShiftLeft_ULong_ShouldReturnCorrectResult()
    {
        ulong? value = 4ul;
        var result = Library.ShiftLeft(value, 2);
        Assert.AreEqual(16ul, result);
    }

    [TestMethod]
    public void ShiftLeft_ULong_WhenNull_ShouldReturnNull()
    {
        ulong? value = null;
        var result = Library.ShiftLeft(value, 2);
        Assert.IsNull(result);
    }

    #endregion

    #region ShiftRight Extended Tests

    [TestMethod]
    public void ShiftRight_Short_ShouldReturnCorrectResult()
    {
        short? value = 16;
        var result = Library.ShiftRight(value, 2);
        Assert.AreEqual((short)4, result);
    }

    [TestMethod]
    public void ShiftRight_Short_WhenNull_ShouldReturnNull()
    {
        short? value = null;
        var result = Library.ShiftRight(value, 2);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ShiftRight_Long_ShouldReturnCorrectResult()
    {
        long? value = 16L;
        var result = Library.ShiftRight(value, 2);
        Assert.AreEqual(4L, result);
    }

    [TestMethod]
    public void ShiftRight_Long_WhenNull_ShouldReturnNull()
    {
        long? value = null;
        var result = Library.ShiftRight(value, 2);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ShiftRight_SByte_ShouldReturnCorrectResult()
    {
        sbyte? value = 16;
        var result = Library.ShiftRight(value, 2);
        Assert.AreEqual((sbyte)4, result);
    }

    [TestMethod]
    public void ShiftRight_SByte_WhenNull_ShouldReturnNull()
    {
        sbyte? value = null;
        var result = Library.ShiftRight(value, 2);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ShiftRight_UShort_ShouldReturnCorrectResult()
    {
        ushort? value = 16;
        var result = Library.ShiftRight(value, 2);
        Assert.AreEqual((ushort)4, result);
    }

    [TestMethod]
    public void ShiftRight_UShort_WhenNull_ShouldReturnNull()
    {
        ushort? value = null;
        var result = Library.ShiftRight(value, 2);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ShiftRight_UInt_ShouldReturnCorrectResult()
    {
        uint? value = 16u;
        var result = Library.ShiftRight(value, 2);
        Assert.AreEqual(4u, result);
    }

    [TestMethod]
    public void ShiftRight_UInt_WhenNull_ShouldReturnNull()
    {
        uint? value = null;
        var result = Library.ShiftRight(value, 2);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ShiftRight_ULong_ShouldReturnCorrectResult()
    {
        ulong? value = 16ul;
        var result = Library.ShiftRight(value, 2);
        Assert.AreEqual(4ul, result);
    }

    [TestMethod]
    public void ShiftRight_ULong_WhenNull_ShouldReturnNull()
    {
        ulong? value = null;
        var result = Library.ShiftRight(value, 2);
        Assert.IsNull(result);
    }

    #endregion

    #region Not Extended Tests

    [TestMethod]
    public void Not_Short_ShouldReturnCorrectResult()
    {
        short? value = 15;
        var result = Library.Not(value);
        Assert.AreEqual((short)~15, result);
    }

    [TestMethod]
    public void Not_Short_WhenNull_ShouldReturnNull()
    {
        short? value = null;
        var result = Library.Not(value);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Not_Long_ShouldReturnCorrectResult()
    {
        long? value = 15L;
        var result = Library.Not(value);
        Assert.AreEqual(~15L, result);
    }

    [TestMethod]
    public void Not_Long_WhenNull_ShouldReturnNull()
    {
        long? value = null;
        var result = Library.Not(value);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Not_SByte_ShouldReturnCorrectResult()
    {
        sbyte? value = 15;
        var result = Library.Not(value);
        Assert.AreEqual((sbyte)~15, result);
    }

    [TestMethod]
    public void Not_SByte_WhenNull_ShouldReturnNull()
    {
        sbyte? value = null;
        var result = Library.Not(value);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Not_UShort_ShouldReturnCorrectResult()
    {
        ushort? value = 15;
        var result = Library.Not(value);
        Assert.AreEqual(unchecked((ushort)~15), result);
    }

    [TestMethod]
    public void Not_UShort_WhenNull_ShouldReturnNull()
    {
        ushort? value = null;
        var result = Library.Not(value);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Not_UInt_ShouldReturnCorrectResult()
    {
        uint? value = 15u;
        var result = Library.Not(value);
        Assert.AreEqual(~15u, result);
    }

    [TestMethod]
    public void Not_UInt_WhenNull_ShouldReturnNull()
    {
        uint? value = null;
        var result = Library.Not(value);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Not_ULong_ShouldReturnCorrectResult()
    {
        ulong? value = 15ul;
        var result = Library.Not(value);
        Assert.AreEqual(~15ul, result);
    }

    [TestMethod]
    public void Not_ULong_WhenNull_ShouldReturnNull()
    {
        ulong? value = null;
        var result = Library.Not(value);
        Assert.IsNull(result);
    }

    #endregion

    #region And Extended Tests - Same Type Overloads

    [TestMethod]
    public void And_Short_Short_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        short? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual((short)0b11110000, result);
    }

    [TestMethod]
    public void And_Short_Short_WhenLeftNull_ShouldReturnNull()
    {
        short? left = null;
        short? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Short_Short_WhenRightNull_ShouldReturnNull()
    {
        short? left = 0b11110000;
        short? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Long_Long_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        long? right = 0b11111111L;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000L, result);
    }

    [TestMethod]
    public void And_Long_Long_WhenLeftNull_ShouldReturnNull()
    {
        long? left = null;
        long? right = 0b11111111L;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Long_Long_WhenRightNull_ShouldReturnNull()
    {
        long? left = 0b11110000L;
        long? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_SByte_SByte_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        sbyte? right = 0b01111111;
        var result = Library.And(left, right);
        Assert.AreEqual((sbyte)0b01110000, result);
    }

    [TestMethod]
    public void And_SByte_SByte_WhenNull_ShouldReturnNull()
    {
        sbyte? left = null;
        sbyte? right = 0b01111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UShort_UShort_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        ushort? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual((ushort)0b11110000, result);
    }

    [TestMethod]
    public void And_UShort_UShort_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        ushort? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UInt_UInt_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000u, result);
    }

    [TestMethod]
    public void And_UInt_UInt_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_ULong_ULong_ShouldReturnCorrectResult()
    {
        ulong? left = 0b11110000ul;
        ulong? right = 0b11111111ul;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000ul, result);
    }

    [TestMethod]
    public void And_ULong_ULong_WhenNull_ShouldReturnNull()
    {
        ulong? left = null;
        ulong? right = 0b11111111ul;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    #endregion

    #region And Extended Tests - Mixed Type Overloads

    [TestMethod]
    public void And_Byte_SByte_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        sbyte? right = 0b01111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b01110000, result);
    }

    [TestMethod]
    public void And_Byte_SByte_WhenNull_ShouldReturnNull()
    {
        byte? left = null;
        sbyte? right = 0b01111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Byte_Short_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        short? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000, result);
    }

    [TestMethod]
    public void And_Byte_Short_WhenNull_ShouldReturnNull()
    {
        byte? left = 0b11110000;
        short? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Byte_UShort_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        ushort? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000, result);
    }

    [TestMethod]
    public void And_Byte_UShort_WhenNull_ShouldReturnNull()
    {
        byte? left = null;
        ushort? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Byte_Int_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        int? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000, result);
    }

    [TestMethod]
    public void And_Byte_Int_WhenNull_ShouldReturnNull()
    {
        byte? left = 0b11110000;
        int? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Byte_UInt_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000u, result);
    }

    [TestMethod]
    public void And_Byte_UInt_WhenNull_ShouldReturnNull()
    {
        byte? left = null;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Byte_Long_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        long? right = 0b11111111L;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000L, result);
    }

    [TestMethod]
    public void And_Byte_Long_WhenNull_ShouldReturnNull()
    {
        byte? left = 0b11110000;
        long? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Byte_ULong_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        ulong? right = 0b11111111ul;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000ul, result);
    }

    [TestMethod]
    public void And_Byte_ULong_WhenNull_ShouldReturnNull()
    {
        byte? left = null;
        ulong? right = 0b11111111ul;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_SByte_Byte_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b01110000, result);
    }

    [TestMethod]
    public void And_SByte_Byte_WhenNull_ShouldReturnNull()
    {
        sbyte? left = null;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_SByte_Short_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        short? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b01110000, result);
    }

    [TestMethod]
    public void And_SByte_Short_WhenNull_ShouldReturnNull()
    {
        sbyte? left = 0b01110000;
        short? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_SByte_UShort_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        ushort? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b01110000, result);
    }

    [TestMethod]
    public void And_SByte_UShort_WhenNull_ShouldReturnNull()
    {
        sbyte? left = null;
        ushort? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_SByte_Int_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        int? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b01110000, result);
    }

    [TestMethod]
    public void And_SByte_Int_WhenNull_ShouldReturnNull()
    {
        sbyte? left = 0b01110000;
        int? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_SByte_UInt_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.AreEqual(0b01110000u, result);
    }

    [TestMethod]
    public void And_SByte_UInt_WhenNull_ShouldReturnNull()
    {
        sbyte? left = null;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_SByte_Long_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        long? right = 0b11111111L;
        var result = Library.And(left, right);
        Assert.AreEqual(0b01110000L, result);
    }

    [TestMethod]
    public void And_SByte_Long_WhenNull_ShouldReturnNull()
    {
        sbyte? left = 0b01110000;
        long? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Short_Byte_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000, result);
    }

    [TestMethod]
    public void And_Short_Byte_WhenNull_ShouldReturnNull()
    {
        short? left = null;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Short_SByte_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        sbyte? right = 0b01111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b01110000, result);
    }

    [TestMethod]
    public void And_Short_SByte_WhenNull_ShouldReturnNull()
    {
        short? left = 0b11110000;
        sbyte? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Short_UShort_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        ushort? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000, result);
    }

    [TestMethod]
    public void And_Short_UShort_WhenNull_ShouldReturnNull()
    {
        short? left = null;
        ushort? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Short_Int_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        int? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000, result);
    }

    [TestMethod]
    public void And_Short_Int_WhenNull_ShouldReturnNull()
    {
        short? left = 0b11110000;
        int? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Short_UInt_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000u, result);
    }

    [TestMethod]
    public void And_Short_UInt_WhenNull_ShouldReturnNull()
    {
        short? left = null;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Short_Long_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        long? right = 0b11111111L;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000L, result);
    }

    [TestMethod]
    public void And_Short_Long_WhenNull_ShouldReturnNull()
    {
        short? left = 0b11110000;
        long? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UShort_Byte_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000, result);
    }

    [TestMethod]
    public void And_UShort_Byte_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UShort_SByte_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        sbyte? right = 0b01111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b01110000, result);
    }

    [TestMethod]
    public void And_UShort_SByte_WhenNull_ShouldReturnNull()
    {
        ushort? left = 0b11110000;
        sbyte? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UShort_Short_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        short? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000, result);
    }

    [TestMethod]
    public void And_UShort_Short_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        short? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UShort_Int_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        int? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000, result);
    }

    [TestMethod]
    public void And_UShort_Int_WhenNull_ShouldReturnNull()
    {
        ushort? left = 0b11110000;
        int? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UShort_UInt_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000u, result);
    }

    [TestMethod]
    public void And_UShort_UInt_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UShort_Long_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        long? right = 0b11111111L;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000L, result);
    }

    [TestMethod]
    public void And_UShort_Long_WhenNull_ShouldReturnNull()
    {
        ushort? left = 0b11110000;
        long? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UShort_ULong_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        ulong? right = 0b11111111ul;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000ul, result);
    }

    [TestMethod]
    public void And_UShort_ULong_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        ulong? right = 0b11111111ul;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Int_Byte_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000, result);
    }

    [TestMethod]
    public void And_Int_Byte_WhenNull_ShouldReturnNull()
    {
        int? left = null;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Int_SByte_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        sbyte? right = 0b01111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b01110000, result);
    }

    [TestMethod]
    public void And_Int_SByte_WhenNull_ShouldReturnNull()
    {
        int? left = 0b11110000;
        sbyte? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Int_Short_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        short? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000, result);
    }

    [TestMethod]
    public void And_Int_Short_WhenNull_ShouldReturnNull()
    {
        int? left = null;
        short? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Int_UShort_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        ushort? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000, result);
    }

    [TestMethod]
    public void And_Int_UShort_WhenNull_ShouldReturnNull()
    {
        int? left = 0b11110000;
        ushort? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Int_UInt_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000u, result);
    }

    [TestMethod]
    public void And_Int_UInt_WhenNull_ShouldReturnNull()
    {
        int? left = null;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Int_Long_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        long? right = 0b11111111L;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000L, result);
    }

    [TestMethod]
    public void And_Int_Long_WhenNull_ShouldReturnNull()
    {
        int? left = 0b11110000;
        long? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UInt_Byte_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000u, result);
    }

    [TestMethod]
    public void And_UInt_Byte_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UInt_SByte_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        sbyte? right = 0b01111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b01110000u, result);
    }

    [TestMethod]
    public void And_UInt_SByte_WhenNull_ShouldReturnNull()
    {
        uint? left = 0b11110000u;
        sbyte? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UInt_Short_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        short? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000u, result);
    }

    [TestMethod]
    public void And_UInt_Short_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        short? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UInt_UShort_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        ushort? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000u, result);
    }

    [TestMethod]
    public void And_UInt_UShort_WhenNull_ShouldReturnNull()
    {
        uint? left = 0b11110000u;
        ushort? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UInt_Int_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        int? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000u, result);
    }

    [TestMethod]
    public void And_UInt_Int_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        int? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UInt_Long_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        long? right = 0b11111111L;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000ul, result);
    }

    [TestMethod]
    public void And_UInt_Long_WhenNull_ShouldReturnNull()
    {
        uint? left = 0b11110000u;
        long? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_UInt_ULong_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        ulong? right = 0b11111111ul;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000ul, result);
    }

    [TestMethod]
    public void And_UInt_ULong_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        ulong? right = 0b11111111ul;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Long_Byte_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000L, result);
    }

    [TestMethod]
    public void And_Long_Byte_WhenNull_ShouldReturnNull()
    {
        long? left = null;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Long_SByte_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        sbyte? right = 0b01111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b01110000L, result);
    }

    [TestMethod]
    public void And_Long_SByte_WhenNull_ShouldReturnNull()
    {
        long? left = 0b11110000L;
        sbyte? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Long_Short_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        short? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000L, result);
    }

    [TestMethod]
    public void And_Long_Short_WhenNull_ShouldReturnNull()
    {
        long? left = null;
        short? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Long_UShort_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        ushort? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000L, result);
    }

    [TestMethod]
    public void And_Long_UShort_WhenNull_ShouldReturnNull()
    {
        long? left = 0b11110000L;
        ushort? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Long_Int_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        int? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000L, result);
    }

    [TestMethod]
    public void And_Long_Int_WhenNull_ShouldReturnNull()
    {
        long? left = null;
        int? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_Long_UInt_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000L, result);
    }

    [TestMethod]
    public void And_Long_UInt_WhenNull_ShouldReturnNull()
    {
        long? left = 0b11110000L;
        uint? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_ULong_Byte_ShouldReturnCorrectResult()
    {
        ulong? left = 0b11110000ul;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000ul, result);
    }

    [TestMethod]
    public void And_ULong_Byte_WhenNull_ShouldReturnNull()
    {
        ulong? left = null;
        byte? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_ULong_UShort_ShouldReturnCorrectResult()
    {
        ulong? left = 0b11110000ul;
        ushort? right = 0b11111111;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000ul, result);
    }

    [TestMethod]
    public void And_ULong_UShort_WhenNull_ShouldReturnNull()
    {
        ulong? left = 0b11110000ul;
        ushort? right = null;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void And_ULong_UInt_ShouldReturnCorrectResult()
    {
        ulong? left = 0b11110000ul;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.AreEqual(0b11110000ul, result);
    }

    [TestMethod]
    public void And_ULong_UInt_WhenNull_ShouldReturnNull()
    {
        ulong? left = null;
        uint? right = 0b11111111u;
        var result = Library.And(left, right);
        Assert.IsNull(result);
    }

    #endregion

    #region Or Extended Tests - Same Type Overloads

    [TestMethod]
    public void Or_Short_Short_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        short? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual((short)0b11111111, result);
    }

    [TestMethod]
    public void Or_Short_Short_WhenNull_ShouldReturnNull()
    {
        short? left = null;
        short? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Long_Long_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        long? right = 0b00001111L;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111L, result);
    }

    [TestMethod]
    public void Or_Long_Long_WhenNull_ShouldReturnNull()
    {
        long? left = null;
        long? right = 0b00001111L;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_SByte_SByte_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        sbyte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual((sbyte)0b01111111, result);
    }

    [TestMethod]
    public void Or_SByte_SByte_WhenNull_ShouldReturnNull()
    {
        sbyte? left = null;
        sbyte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UShort_UShort_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        ushort? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual((ushort)0b11111111, result);
    }

    [TestMethod]
    public void Or_UShort_UShort_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        ushort? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UInt_UInt_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111u, result);
    }

    [TestMethod]
    public void Or_UInt_UInt_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_ULong_ULong_ShouldReturnCorrectResult()
    {
        ulong? left = 0b11110000ul;
        ulong? right = 0b00001111ul;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111ul, result);
    }

    [TestMethod]
    public void Or_ULong_ULong_WhenNull_ShouldReturnNull()
    {
        ulong? left = null;
        ulong? right = 0b00001111ul;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    #endregion

    #region Or Extended Tests - Mixed Type Overloads

    [TestMethod]
    public void Or_Byte_SByte_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        sbyte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_Byte_SByte_WhenNull_ShouldReturnNull()
    {
        byte? left = null;
        sbyte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Byte_Short_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        short? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_Byte_Short_WhenNull_ShouldReturnNull()
    {
        byte? left = 0b11110000;
        short? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Byte_UShort_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        ushort? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_Byte_UShort_WhenNull_ShouldReturnNull()
    {
        byte? left = null;
        ushort? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Byte_Int_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        int? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_Byte_Int_WhenNull_ShouldReturnNull()
    {
        byte? left = 0b11110000;
        int? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Byte_UInt_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111u, result);
    }

    [TestMethod]
    public void Or_Byte_UInt_WhenNull_ShouldReturnNull()
    {
        byte? left = null;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Byte_Long_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        long? right = 0b00001111L;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111L, result);
    }

    [TestMethod]
    public void Or_Byte_Long_WhenNull_ShouldReturnNull()
    {
        byte? left = 0b11110000;
        long? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Byte_ULong_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        ulong? right = 0b00001111ul;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111ul, result);
    }

    [TestMethod]
    public void Or_Byte_ULong_WhenNull_ShouldReturnNull()
    {
        byte? left = null;
        ulong? right = 0b00001111ul;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_SByte_Byte_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b01111111, result);
    }

    [TestMethod]
    public void Or_SByte_Byte_WhenNull_ShouldReturnNull()
    {
        sbyte? left = null;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_SByte_Short_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        short? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b01111111, result);
    }

    [TestMethod]
    public void Or_SByte_Short_WhenNull_ShouldReturnNull()
    {
        sbyte? left = 0b01110000;
        short? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_SByte_UShort_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        ushort? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b01111111, result);
    }

    [TestMethod]
    public void Or_SByte_UShort_WhenNull_ShouldReturnNull()
    {
        sbyte? left = null;
        ushort? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_SByte_Int_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        int? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b01111111, result);
    }

    [TestMethod]
    public void Or_SByte_Int_WhenNull_ShouldReturnNull()
    {
        sbyte? left = 0b01110000;
        int? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_SByte_UInt_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b01111111u, result);
    }

    [TestMethod]
    public void Or_SByte_UInt_WhenNull_ShouldReturnNull()
    {
        sbyte? left = null;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_SByte_Long_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        long? right = 0b00001111L;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b01111111L, result);
    }

    [TestMethod]
    public void Or_SByte_Long_WhenNull_ShouldReturnNull()
    {
        sbyte? left = 0b01110000;
        long? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Short_Byte_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_Short_Byte_WhenNull_ShouldReturnNull()
    {
        short? left = null;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Short_SByte_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        sbyte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_Short_SByte_WhenNull_ShouldReturnNull()
    {
        short? left = 0b11110000;
        sbyte? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Short_UShort_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        ushort? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_Short_UShort_WhenNull_ShouldReturnNull()
    {
        short? left = null;
        ushort? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Short_Int_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        int? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_Short_Int_WhenNull_ShouldReturnNull()
    {
        short? left = 0b11110000;
        int? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Short_UInt_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111u, result);
    }

    [TestMethod]
    public void Or_Short_UInt_WhenNull_ShouldReturnNull()
    {
        short? left = null;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UShort_Byte_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_UShort_Byte_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UShort_SByte_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        sbyte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_UShort_SByte_WhenNull_ShouldReturnNull()
    {
        ushort? left = 0b11110000;
        sbyte? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UShort_Short_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        short? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_UShort_Short_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        short? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UShort_Int_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        int? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_UShort_Int_WhenNull_ShouldReturnNull()
    {
        ushort? left = 0b11110000;
        int? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UShort_UInt_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111u, result);
    }

    [TestMethod]
    public void Or_UShort_UInt_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UShort_Long_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        long? right = 0b00001111L;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111L, result);
    }

    [TestMethod]
    public void Or_UShort_Long_WhenNull_ShouldReturnNull()
    {
        ushort? left = 0b11110000;
        long? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UShort_ULong_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        ulong? right = 0b00001111ul;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111ul, result);
    }

    [TestMethod]
    public void Or_UShort_ULong_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        ulong? right = 0b00001111ul;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Int_Byte_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_Int_Byte_WhenNull_ShouldReturnNull()
    {
        int? left = null;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Int_SByte_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        sbyte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_Int_SByte_WhenNull_ShouldReturnNull()
    {
        int? left = 0b11110000;
        sbyte? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Int_Short_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        short? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_Int_Short_WhenNull_ShouldReturnNull()
    {
        int? left = null;
        short? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Int_UShort_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        ushort? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111, result);
    }

    [TestMethod]
    public void Or_Int_UShort_WhenNull_ShouldReturnNull()
    {
        int? left = 0b11110000;
        ushort? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Int_UInt_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111u, result);
    }

    [TestMethod]
    public void Or_Int_UInt_WhenNull_ShouldReturnNull()
    {
        int? left = null;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Int_Long_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        long? right = 0b00001111L;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111L, result);
    }

    [TestMethod]
    public void Or_Int_Long_WhenNull_ShouldReturnNull()
    {
        int? left = 0b11110000;
        long? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UInt_Byte_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111u, result);
    }

    [TestMethod]
    public void Or_UInt_Byte_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UInt_SByte_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        sbyte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111u, result);
    }

    [TestMethod]
    public void Or_UInt_SByte_WhenNull_ShouldReturnNull()
    {
        uint? left = 0b11110000u;
        sbyte? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UInt_Short_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        short? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111u, result);
    }

    [TestMethod]
    public void Or_UInt_Short_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        short? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UInt_UShort_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        ushort? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111u, result);
    }

    [TestMethod]
    public void Or_UInt_UShort_WhenNull_ShouldReturnNull()
    {
        uint? left = 0b11110000u;
        ushort? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UInt_Int_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        int? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111u, result);
    }

    [TestMethod]
    public void Or_UInt_Int_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        int? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UInt_Long_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        long? right = 0b00001111L;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111ul, result);
    }

    [TestMethod]
    public void Or_UInt_Long_WhenNull_ShouldReturnNull()
    {
        uint? left = 0b11110000u;
        long? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_UInt_ULong_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        ulong? right = 0b00001111ul;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111ul, result);
    }

    [TestMethod]
    public void Or_UInt_ULong_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        ulong? right = 0b00001111ul;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Long_Byte_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111L, result);
    }

    [TestMethod]
    public void Or_Long_Byte_WhenNull_ShouldReturnNull()
    {
        long? left = null;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Long_SByte_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        sbyte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111L, result);
    }

    [TestMethod]
    public void Or_Long_SByte_WhenNull_ShouldReturnNull()
    {
        long? left = 0b11110000L;
        sbyte? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Long_Short_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        short? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111L, result);
    }

    [TestMethod]
    public void Or_Long_Short_WhenNull_ShouldReturnNull()
    {
        long? left = null;
        short? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Long_UShort_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        ushort? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111L, result);
    }

    [TestMethod]
    public void Or_Long_UShort_WhenNull_ShouldReturnNull()
    {
        long? left = 0b11110000L;
        ushort? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Long_Int_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        int? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111L, result);
    }

    [TestMethod]
    public void Or_Long_Int_WhenNull_ShouldReturnNull()
    {
        long? left = null;
        int? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_Long_UInt_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111L, result);
    }

    [TestMethod]
    public void Or_Long_UInt_WhenNull_ShouldReturnNull()
    {
        long? left = 0b11110000L;
        uint? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_ULong_Byte_ShouldReturnCorrectResult()
    {
        ulong? left = 0b11110000ul;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111ul, result);
    }

    [TestMethod]
    public void Or_ULong_Byte_WhenNull_ShouldReturnNull()
    {
        ulong? left = null;
        byte? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_ULong_UShort_ShouldReturnCorrectResult()
    {
        ulong? left = 0b11110000ul;
        ushort? right = 0b00001111;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111ul, result);
    }

    [TestMethod]
    public void Or_ULong_UShort_WhenNull_ShouldReturnNull()
    {
        ulong? left = 0b11110000ul;
        ushort? right = null;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Or_ULong_UInt_ShouldReturnCorrectResult()
    {
        ulong? left = 0b11110000ul;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.AreEqual(0b11111111ul, result);
    }

    [TestMethod]
    public void Or_ULong_UInt_WhenNull_ShouldReturnNull()
    {
        ulong? left = null;
        uint? right = 0b00001111u;
        var result = Library.Or(left, right);
        Assert.IsNull(result);
    }

    #endregion

    #region Xor Extended Tests - Same Type Overloads

    [TestMethod]
    public void Xor_Short_Short_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        short? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual((short)0b00001111, result);
    }

    [TestMethod]
    public void Xor_Short_Short_WhenNull_ShouldReturnNull()
    {
        short? left = null;
        short? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Long_Long_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        long? right = 0b11111111L;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111L, result);
    }

    [TestMethod]
    public void Xor_Long_Long_WhenNull_ShouldReturnNull()
    {
        long? left = null;
        long? right = 0b11111111L;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_SByte_SByte_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        sbyte? right = 0b01111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual((sbyte)0b00001111, result);
    }

    [TestMethod]
    public void Xor_SByte_SByte_WhenNull_ShouldReturnNull()
    {
        sbyte? left = null;
        sbyte? right = 0b01111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UShort_UShort_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        ushort? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual((ushort)0b00001111, result);
    }

    [TestMethod]
    public void Xor_UShort_UShort_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        ushort? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UInt_UInt_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111u, result);
    }

    [TestMethod]
    public void Xor_UInt_UInt_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_ULong_ULong_ShouldReturnCorrectResult()
    {
        ulong? left = 0b11110000ul;
        ulong? right = 0b11111111ul;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111ul, result);
    }

    [TestMethod]
    public void Xor_ULong_ULong_WhenNull_ShouldReturnNull()
    {
        ulong? left = null;
        ulong? right = 0b11111111ul;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    #endregion

    #region Xor Extended Tests - Mixed Type Overloads

    [TestMethod]
    public void Xor_Byte_SByte_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        sbyte? right = 0b01111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b10001111, result);
    }

    [TestMethod]
    public void Xor_Byte_SByte_WhenNull_ShouldReturnNull()
    {
        byte? left = null;
        sbyte? right = 0b01111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Byte_Short_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        short? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111, result);
    }

    [TestMethod]
    public void Xor_Byte_Short_WhenNull_ShouldReturnNull()
    {
        byte? left = 0b11110000;
        short? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Byte_UShort_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        ushort? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111, result);
    }

    [TestMethod]
    public void Xor_Byte_UShort_WhenNull_ShouldReturnNull()
    {
        byte? left = null;
        ushort? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Byte_Int_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        int? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111, result);
    }

    [TestMethod]
    public void Xor_Byte_Int_WhenNull_ShouldReturnNull()
    {
        byte? left = 0b11110000;
        int? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Byte_UInt_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111u, result);
    }

    [TestMethod]
    public void Xor_Byte_UInt_WhenNull_ShouldReturnNull()
    {
        byte? left = null;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Byte_Long_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        long? right = 0b11111111L;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111L, result);
    }

    [TestMethod]
    public void Xor_Byte_Long_WhenNull_ShouldReturnNull()
    {
        byte? left = 0b11110000;
        long? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Byte_ULong_ShouldReturnCorrectResult()
    {
        byte? left = 0b11110000;
        ulong? right = 0b11111111ul;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111ul, result);
    }

    [TestMethod]
    public void Xor_Byte_ULong_WhenNull_ShouldReturnNull()
    {
        byte? left = null;
        ulong? right = 0b11111111ul;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_SByte_Byte_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b10001111, result);
    }

    [TestMethod]
    public void Xor_SByte_Byte_WhenNull_ShouldReturnNull()
    {
        sbyte? left = null;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_SByte_Short_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        short? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b10001111, result);
    }

    [TestMethod]
    public void Xor_SByte_Short_WhenNull_ShouldReturnNull()
    {
        sbyte? left = 0b01110000;
        short? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_SByte_UShort_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        ushort? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b10001111, result);
    }

    [TestMethod]
    public void Xor_SByte_UShort_WhenNull_ShouldReturnNull()
    {
        sbyte? left = null;
        ushort? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_SByte_Int_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        int? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b10001111, result);
    }

    [TestMethod]
    public void Xor_SByte_Int_WhenNull_ShouldReturnNull()
    {
        sbyte? left = 0b01110000;
        int? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_SByte_UInt_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b10001111u, result);
    }

    [TestMethod]
    public void Xor_SByte_UInt_WhenNull_ShouldReturnNull()
    {
        sbyte? left = null;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_SByte_Long_ShouldReturnCorrectResult()
    {
        sbyte? left = 0b01110000;
        long? right = 0b11111111L;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b10001111L, result);
    }

    [TestMethod]
    public void Xor_SByte_Long_WhenNull_ShouldReturnNull()
    {
        sbyte? left = 0b01110000;
        long? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Short_Byte_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111, result);
    }

    [TestMethod]
    public void Xor_Short_Byte_WhenNull_ShouldReturnNull()
    {
        short? left = null;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Short_SByte_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        sbyte? right = 0b01111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b10001111, result);
    }

    [TestMethod]
    public void Xor_Short_SByte_WhenNull_ShouldReturnNull()
    {
        short? left = 0b11110000;
        sbyte? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Short_UShort_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        ushort? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111, result);
    }

    [TestMethod]
    public void Xor_Short_UShort_WhenNull_ShouldReturnNull()
    {
        short? left = null;
        ushort? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Short_Int_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        int? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111, result);
    }

    [TestMethod]
    public void Xor_Short_Int_WhenNull_ShouldReturnNull()
    {
        short? left = 0b11110000;
        int? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Short_UInt_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111u, result);
    }

    [TestMethod]
    public void Xor_Short_UInt_WhenNull_ShouldReturnNull()
    {
        short? left = null;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Short_Long_ShouldReturnCorrectResult()
    {
        short? left = 0b11110000;
        long? right = 0b11111111L;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111L, result);
    }

    [TestMethod]
    public void Xor_Short_Long_WhenNull_ShouldReturnNull()
    {
        short? left = 0b11110000;
        long? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UShort_Byte_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111, result);
    }

    [TestMethod]
    public void Xor_UShort_Byte_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UShort_SByte_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        sbyte? right = 0b01111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b10001111, result);
    }

    [TestMethod]
    public void Xor_UShort_SByte_WhenNull_ShouldReturnNull()
    {
        ushort? left = 0b11110000;
        sbyte? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UShort_Short_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        short? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111, result);
    }

    [TestMethod]
    public void Xor_UShort_Short_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        short? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UShort_Int_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        int? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111, result);
    }

    [TestMethod]
    public void Xor_UShort_Int_WhenNull_ShouldReturnNull()
    {
        ushort? left = 0b11110000;
        int? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UShort_UInt_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111u, result);
    }

    [TestMethod]
    public void Xor_UShort_UInt_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UShort_Long_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        long? right = 0b11111111L;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111L, result);
    }

    [TestMethod]
    public void Xor_UShort_Long_WhenNull_ShouldReturnNull()
    {
        ushort? left = 0b11110000;
        long? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UShort_ULong_ShouldReturnCorrectResult()
    {
        ushort? left = 0b11110000;
        ulong? right = 0b11111111ul;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111ul, result);
    }

    [TestMethod]
    public void Xor_UShort_ULong_WhenNull_ShouldReturnNull()
    {
        ushort? left = null;
        ulong? right = 0b11111111ul;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Int_Byte_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111, result);
    }

    [TestMethod]
    public void Xor_Int_Byte_WhenNull_ShouldReturnNull()
    {
        int? left = null;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Int_SByte_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        sbyte? right = 0b01111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b10001111, result);
    }

    [TestMethod]
    public void Xor_Int_SByte_WhenNull_ShouldReturnNull()
    {
        int? left = 0b11110000;
        sbyte? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Int_Short_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        short? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111, result);
    }

    [TestMethod]
    public void Xor_Int_Short_WhenNull_ShouldReturnNull()
    {
        int? left = null;
        short? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Int_UShort_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        ushort? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111, result);
    }

    [TestMethod]
    public void Xor_Int_UShort_WhenNull_ShouldReturnNull()
    {
        int? left = 0b11110000;
        ushort? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Int_UInt_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111u, result);
    }

    [TestMethod]
    public void Xor_Int_UInt_WhenNull_ShouldReturnNull()
    {
        int? left = null;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Int_Long_ShouldReturnCorrectResult()
    {
        int? left = 0b11110000;
        long? right = 0b11111111L;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111L, result);
    }

    [TestMethod]
    public void Xor_Int_Long_WhenNull_ShouldReturnNull()
    {
        int? left = 0b11110000;
        long? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UInt_Byte_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111u, result);
    }

    [TestMethod]
    public void Xor_UInt_Byte_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UInt_SByte_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        sbyte? right = 0b01111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b10001111u, result);
    }

    [TestMethod]
    public void Xor_UInt_SByte_WhenNull_ShouldReturnNull()
    {
        uint? left = 0b11110000u;
        sbyte? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UInt_Short_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        short? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111u, result);
    }

    [TestMethod]
    public void Xor_UInt_Short_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        short? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UInt_UShort_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        ushort? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111u, result);
    }

    [TestMethod]
    public void Xor_UInt_UShort_WhenNull_ShouldReturnNull()
    {
        uint? left = 0b11110000u;
        ushort? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UInt_Int_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        int? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111u, result);
    }

    [TestMethod]
    public void Xor_UInt_Int_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        int? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UInt_Long_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        long? right = 0b11111111L;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111ul, result);
    }

    [TestMethod]
    public void Xor_UInt_Long_WhenNull_ShouldReturnNull()
    {
        uint? left = 0b11110000u;
        long? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_UInt_ULong_ShouldReturnCorrectResult()
    {
        uint? left = 0b11110000u;
        ulong? right = 0b11111111ul;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111ul, result);
    }

    [TestMethod]
    public void Xor_UInt_ULong_WhenNull_ShouldReturnNull()
    {
        uint? left = null;
        ulong? right = 0b11111111ul;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Long_Byte_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111L, result);
    }

    [TestMethod]
    public void Xor_Long_Byte_WhenNull_ShouldReturnNull()
    {
        long? left = null;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Long_SByte_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        sbyte? right = 0b01111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b10001111L, result);
    }

    [TestMethod]
    public void Xor_Long_SByte_WhenNull_ShouldReturnNull()
    {
        long? left = 0b11110000L;
        sbyte? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Long_Short_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        short? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111L, result);
    }

    [TestMethod]
    public void Xor_Long_Short_WhenNull_ShouldReturnNull()
    {
        long? left = null;
        short? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Long_UShort_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        ushort? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111L, result);
    }

    [TestMethod]
    public void Xor_Long_UShort_WhenNull_ShouldReturnNull()
    {
        long? left = 0b11110000L;
        ushort? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Long_Int_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        int? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111L, result);
    }

    [TestMethod]
    public void Xor_Long_Int_WhenNull_ShouldReturnNull()
    {
        long? left = null;
        int? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_Long_UInt_ShouldReturnCorrectResult()
    {
        long? left = 0b11110000L;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111L, result);
    }

    [TestMethod]
    public void Xor_Long_UInt_WhenNull_ShouldReturnNull()
    {
        long? left = 0b11110000L;
        uint? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_ULong_Byte_ShouldReturnCorrectResult()
    {
        ulong? left = 0b11110000ul;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111ul, result);
    }

    [TestMethod]
    public void Xor_ULong_Byte_WhenNull_ShouldReturnNull()
    {
        ulong? left = null;
        byte? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_ULong_UShort_ShouldReturnCorrectResult()
    {
        ulong? left = 0b11110000ul;
        ushort? right = 0b11111111;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111ul, result);
    }

    [TestMethod]
    public void Xor_ULong_UShort_WhenNull_ShouldReturnNull()
    {
        ulong? left = 0b11110000ul;
        ushort? right = null;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Xor_ULong_UInt_ShouldReturnCorrectResult()
    {
        ulong? left = 0b11110000ul;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.AreEqual(0b00001111ul, result);
    }

    [TestMethod]
    public void Xor_ULong_UInt_WhenNull_ShouldReturnNull()
    {
        ulong? left = null;
        uint? right = 0b11111111u;
        var result = Library.Xor(left, right);
        Assert.IsNull(result);
    }

    #endregion
}
