using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for bit operations in LibraryBaseBitsOperations.cs to improve branch coverage
/// </summary>
[TestClass]
public class BitOperationsExtendedTests : LibraryBaseBaseTests
{
    #region ShiftLeft Tests

    [TestMethod]
    public void ShiftLeft_ByteNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftLeft((byte?)null, 1));
    }

    [TestMethod]
    public void ShiftLeft_ByteValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftLeft((byte?)1, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual((byte)2, result.Value);
    }

    [TestMethod]
    public void ShiftLeft_ShortNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftLeft((short?)null, 1));
    }

    [TestMethod]
    public void ShiftLeft_ShortValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftLeft((short?)1, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual((short)2, result.Value);
    }

    [TestMethod]
    public void ShiftLeft_IntNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftLeft((int?)null, 1));
    }

    [TestMethod]
    public void ShiftLeft_IntValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftLeft((int?)1, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Value);
    }

    [TestMethod]
    public void ShiftLeft_LongNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftLeft((long?)null, 1));
    }

    [TestMethod]
    public void ShiftLeft_LongValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftLeft(1L, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual(2L, result.Value);
    }

    [TestMethod]
    public void ShiftLeft_SByteNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftLeft(null, 1));
    }

    [TestMethod]
    public void ShiftLeft_SByteValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftLeft((sbyte?)1, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual((sbyte)2, result.Value);
    }

    [TestMethod]
    public void ShiftLeft_UShortNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftLeft((ushort?)null, 1));
    }

    [TestMethod]
    public void ShiftLeft_UShortValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftLeft((ushort?)1, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual((ushort)2, result.Value);
    }

    [TestMethod]
    public void ShiftLeft_UIntNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftLeft((uint?)null, 1));
    }

    [TestMethod]
    public void ShiftLeft_UIntValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftLeft((uint?)1, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual((uint)2, result.Value);
    }

    [TestMethod]
    public void ShiftLeft_ULongNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftLeft((ulong?)null, 1));
    }

    [TestMethod]
    public void ShiftLeft_ULongValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftLeft((ulong?)1, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual((ulong)2, result.Value);
    }

    #endregion

    #region ShiftRight Tests

    [TestMethod]
    public void ShiftRight_ByteNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftRight((byte?)null, 1));
    }

    [TestMethod]
    public void ShiftRight_ByteValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftRight((byte?)2, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual((byte)1, result.Value);
    }

    [TestMethod]
    public void ShiftRight_ShortNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftRight((short?)null, 1));
    }

    [TestMethod]
    public void ShiftRight_ShortValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftRight((short?)2, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual((short)1, result.Value);
    }

    [TestMethod]
    public void ShiftRight_IntNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftRight((int?)null, 1));
    }

    [TestMethod]
    public void ShiftRight_IntValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftRight((int?)2, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Value);
    }

    [TestMethod]
    public void ShiftRight_LongNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftRight((long?)null, 1));
    }

    [TestMethod]
    public void ShiftRight_LongValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftRight(2L, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual(1L, result.Value);
    }

    [TestMethod]
    public void ShiftRight_SByteNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftRight(null, 1));
    }

    [TestMethod]
    public void ShiftRight_SByteValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftRight((sbyte?)2, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual((sbyte)1, result.Value);
    }

    [TestMethod]
    public void ShiftRight_UShortNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftRight((ushort?)null, 1));
    }

    [TestMethod]
    public void ShiftRight_UShortValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftRight((ushort?)2, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual((ushort)1, result.Value);
    }

    [TestMethod]
    public void ShiftRight_UIntNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftRight((uint?)null, 1));
    }

    [TestMethod]
    public void ShiftRight_UIntValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftRight((uint?)2, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual((uint)1, result.Value);
    }

    [TestMethod]
    public void ShiftRight_ULongNull_ReturnsNull()
    {
        Assert.IsNull(Library.ShiftRight((ulong?)null, 1));
    }

    [TestMethod]
    public void ShiftRight_ULongValid_ReturnsShiftedValue()
    {
        var result = Library.ShiftRight((ulong?)2, 1);
        Assert.IsNotNull(result);
        Assert.AreEqual((ulong)1, result.Value);
    }

    #endregion

    #region Not Tests

    [TestMethod]
    public void Not_ByteNull_ReturnsNull()
    {
        Assert.IsNull(Library.Not((byte?)null));
    }

    [TestMethod]
    public void Not_ByteValid_ReturnsNegatedValue()
    {
        var result = Library.Not((byte?)5);
        Assert.IsNotNull(result);
        Assert.AreEqual((byte)250, result.Value);
    }

    [TestMethod]
    public void Not_ShortNull_ReturnsNull()
    {
        Assert.IsNull(Library.Not((short?)null));
    }

    [TestMethod]
    public void Not_ShortValid_ReturnsNegatedValue()
    {
        var result = Library.Not((short?)5);
        Assert.IsNotNull(result);
        Assert.AreEqual((short)-6, result.Value);
    }

    [TestMethod]
    public void Not_IntNull_ReturnsNull()
    {
        Assert.IsNull(Library.Not((int?)null));
    }

    [TestMethod]
    public void Not_IntValid_ReturnsNegatedValue()
    {
        var result = Library.Not((int?)5);
        Assert.IsNotNull(result);
        Assert.AreEqual(-6, result.Value);
    }

    [TestMethod]
    public void Not_LongNull_ReturnsNull()
    {
        Assert.IsNull(Library.Not((long?)null));
    }

    [TestMethod]
    public void Not_LongValid_ReturnsNegatedValue()
    {
        var result = Library.Not(5L);
        Assert.IsNotNull(result);
        Assert.AreEqual(-6L, result.Value);
    }

    [TestMethod]
    public void Not_SByteNull_ReturnsNull()
    {
        Assert.IsNull(Library.Not(null));
    }

    [TestMethod]
    public void Not_SByteValid_ReturnsNegatedValue()
    {
        var result = Library.Not((sbyte?)5);
        Assert.IsNotNull(result);
        Assert.AreEqual((sbyte)-6, result.Value);
    }

    [TestMethod]
    public void Not_UShortNull_ReturnsNull()
    {
        Assert.IsNull(Library.Not((ushort?)null));
    }

    [TestMethod]
    public void Not_UShortValid_ReturnsNegatedValue()
    {
        var result = Library.Not((ushort?)5);
        Assert.IsNotNull(result);
        Assert.AreEqual((ushort)65530, result.Value);
    }

    [TestMethod]
    public void Not_UIntNull_ReturnsNull()
    {
        Assert.IsNull(Library.Not((uint?)null));
    }

    [TestMethod]
    public void Not_UIntValid_ReturnsNegatedValue()
    {
        var result = Library.Not((uint?)5);
        Assert.IsNotNull(result);
        Assert.AreEqual(4294967290u, result.Value);
    }

    [TestMethod]
    public void Not_ULongNull_ReturnsNull()
    {
        Assert.IsNull(Library.Not((ulong?)null));
    }

    [TestMethod]
    public void Not_ULongValid_ReturnsNegatedValue()
    {
        var result = Library.Not((ulong?)5);
        Assert.IsNotNull(result);
        Assert.AreEqual(18446744073709551610UL, result.Value);
    }

    #endregion

    #region And Tests (byte combinations)

    [TestMethod]
    public void And_ByteByte_BothNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((byte?)null, (byte?)null));
    }

    [TestMethod]
    public void And_ByteByte_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((byte?)null, (byte?)5));
    }

    [TestMethod]
    public void And_ByteByte_RightNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((byte?)5, (byte?)null));
    }

    [TestMethod]
    public void And_ByteByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((byte?)5, (byte?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual((byte)4, result.Value);
    }

    [TestMethod]
    public void And_ByteSByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((byte?)5, (sbyte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ByteShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((byte?)5, (short?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ByteUShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((byte?)5, (ushort?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ByteInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((byte?)5, (int?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ByteUInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((byte?)5, (uint?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ByteLong_Valid_ReturnsAndResult()
    {
        var result = Library.And((byte?)5, (long?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ByteULong_Valid_ReturnsAndResult()
    {
        var result = Library.And((byte?)5, (ulong?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    #endregion

    #region And Tests (sbyte combinations)

    [TestMethod]
    public void And_SByteByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((sbyte?)5, (byte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_SByteSByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((sbyte?)5, (sbyte?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual((sbyte)4, result.Value);
    }

    [TestMethod]
    public void And_SByteShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((sbyte?)5, (short?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_SByteUShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((sbyte?)5, (ushort?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_SByteInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((sbyte?)5, (int?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_SByteUInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((sbyte?)5, (uint?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_SByteLong_Valid_ReturnsAndResult()
    {
        var result = Library.And((sbyte?)5, (long?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    #endregion

    #region And Tests (short combinations)

    [TestMethod]
    public void And_ShortByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((short?)5, (byte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ShortSByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((short?)5, (sbyte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ShortShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((short?)5, (short?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual((short)4, result.Value);
    }

    [TestMethod]
    public void And_ShortUShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((short?)5, (ushort?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ShortInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((short?)5, (int?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ShortUInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((short?)5, (uint?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ShortLong_Valid_ReturnsAndResult()
    {
        var result = Library.And((short?)5, (long?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    #endregion

    #region And Tests (ushort combinations)

    [TestMethod]
    public void And_UShortByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((ushort?)5, (byte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_UShortSByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((ushort?)5, (sbyte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_UShortShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((ushort?)5, (short?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_UShortUShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((ushort?)5, (ushort?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual((ushort)4, result.Value);
    }

    [TestMethod]
    public void And_UShortInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((ushort?)5, (int?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_UShortUInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((ushort?)5, (uint?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_UShortLong_Valid_ReturnsAndResult()
    {
        var result = Library.And((ushort?)5, (long?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_UShortULong_Valid_ReturnsAndResult()
    {
        var result = Library.And((ushort?)5, (ulong?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    #endregion

    #region And Tests (int combinations)

    [TestMethod]
    public void And_IntByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((int?)5, (byte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_IntSByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((int?)5, (sbyte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_IntShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((int?)5, (short?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_IntUShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((int?)5, (ushort?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_IntInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((int?)5, (int?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual(4, result.Value);
    }

    [TestMethod]
    public void And_IntUInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((int?)5, (uint?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_IntLong_Valid_ReturnsAndResult()
    {
        var result = Library.And((int?)5, (long?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    #endregion

    #region And Tests (uint combinations)

    [TestMethod]
    public void And_UIntByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((uint?)5, (byte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_UIntSByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((uint?)5, (sbyte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_UIntShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((uint?)5, (short?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_UIntUShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((uint?)5, (ushort?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_UIntInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((uint?)5, (int?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_UIntUInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((uint?)5, (uint?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual((uint)4, result.Value);
    }

    [TestMethod]
    public void And_UIntLong_Valid_ReturnsAndResult()
    {
        var result = Library.And((uint?)5, (long?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_UIntULong_Valid_ReturnsAndResult()
    {
        var result = Library.And((uint?)5, (ulong?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    #endregion

    #region And Tests (long combinations)

    [TestMethod]
    public void And_LongByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((long?)5, (byte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_LongSByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((long?)5, (sbyte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_LongShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((long?)5, (short?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_LongUShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((long?)5, (ushort?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_LongInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((long?)5, (int?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_LongUInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((long?)5, (uint?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_LongLong_Valid_ReturnsAndResult()
    {
        var result = Library.And((long?)5, (long?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual(4L, result.Value);
    }

    #endregion

    #region And Tests (ulong combinations)

    [TestMethod]
    public void And_ULongByte_Valid_ReturnsAndResult()
    {
        var result = Library.And((ulong?)5, (byte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ULongUShort_Valid_ReturnsAndResult()
    {
        var result = Library.And((ulong?)5, (ushort?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ULongUInt_Valid_ReturnsAndResult()
    {
        var result = Library.And((ulong?)5, (uint?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 4);
    }

    [TestMethod]
    public void And_ULongULong_Valid_ReturnsAndResult()
    {
        var result = Library.And((ulong?)5, (ulong?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual((ulong)4, result.Value);
    }

    #endregion

    #region Or Tests (basic combinations)

    [TestMethod]
    public void Or_ByteByte_BothNull_ReturnsNull()
    {
        Assert.IsNull(Library.Or((byte?)null, (byte?)null));
    }

    [TestMethod]
    public void Or_ByteByte_Valid_ReturnsOrResult()
    {
        var result = Library.Or((byte?)4, (byte?)1);
        Assert.IsNotNull(result);
        Assert.AreEqual((byte)5, result.Value);
    }

    [TestMethod]
    public void Or_IntInt_Valid_ReturnsOrResult()
    {
        var result = Library.Or((int?)4, (int?)1);
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Value);
    }

    [TestMethod]
    public void Or_LongLong_Valid_ReturnsOrResult()
    {
        var result = Library.Or((long?)4, (long?)1);
        Assert.IsNotNull(result);
        Assert.AreEqual(5L, result.Value);
    }

    [TestMethod]
    public void Or_ShortShort_Valid_ReturnsOrResult()
    {
        var result = Library.Or((short?)4, (short?)1);
        Assert.IsNotNull(result);
        Assert.AreEqual((short)5, result.Value);
    }

    [TestMethod]
    public void Or_SByteSByte_Valid_ReturnsOrResult()
    {
        var result = Library.Or((sbyte?)4, (sbyte?)1);
        Assert.IsNotNull(result);
        Assert.AreEqual((sbyte)5, result.Value);
    }

    [TestMethod]
    public void Or_UShortUShort_Valid_ReturnsOrResult()
    {
        var result = Library.Or((ushort?)4, (ushort?)1);
        Assert.IsNotNull(result);
        Assert.AreEqual((ushort)5, result.Value);
    }

    [TestMethod]
    public void Or_UIntUInt_Valid_ReturnsOrResult()
    {
        var result = Library.Or((uint?)4, (uint?)1);
        Assert.IsNotNull(result);
        Assert.AreEqual((uint)5, result.Value);
    }

    [TestMethod]
    public void Or_ULongULong_Valid_ReturnsOrResult()
    {
        var result = Library.Or((ulong?)4, (ulong?)1);
        Assert.IsNotNull(result);
        Assert.AreEqual((ulong)5, result.Value);
    }

    #endregion

    #region Xor Tests (basic combinations)

    [TestMethod]
    public void Xor_ByteByte_BothNull_ReturnsNull()
    {
        Assert.IsNull(Library.Xor((byte?)null, (byte?)null));
    }

    [TestMethod]
    public void Xor_ByteByte_Valid_ReturnsXorResult()
    {
        var result = Library.Xor((byte?)5, (byte?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual((byte)1, result.Value);
    }

    [TestMethod]
    public void Xor_IntInt_Valid_ReturnsXorResult()
    {
        var result = Library.Xor((int?)5, (int?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Value);
    }

    [TestMethod]
    public void Xor_LongLong_Valid_ReturnsXorResult()
    {
        var result = Library.Xor((long?)5, (long?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual(1L, result.Value);
    }

    [TestMethod]
    public void Xor_ShortShort_Valid_ReturnsXorResult()
    {
        var result = Library.Xor((short?)5, (short?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual((short)1, result.Value);
    }

    [TestMethod]
    public void Xor_SByteSByte_Valid_ReturnsXorResult()
    {
        var result = Library.Xor((sbyte?)5, (sbyte?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual((sbyte)1, result.Value);
    }

    [TestMethod]
    public void Xor_UShortUShort_Valid_ReturnsXorResult()
    {
        var result = Library.Xor((ushort?)5, (ushort?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual((ushort)1, result.Value);
    }

    [TestMethod]
    public void Xor_UIntUInt_Valid_ReturnsXorResult()
    {
        var result = Library.Xor((uint?)5, (uint?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual((uint)1, result.Value);
    }

    [TestMethod]
    public void Xor_ULongULong_Valid_ReturnsXorResult()
    {
        var result = Library.Xor((ulong?)5, (ulong?)4);
        Assert.IsNotNull(result);
        Assert.AreEqual((ulong)1, result.Value);
    }

    #endregion

    #region Mixed type And null tests

    [TestMethod]
    public void And_ByteSByte_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((byte?)null, (sbyte?)5));
    }

    [TestMethod]
    public void And_ByteShort_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((byte?)null, (short?)5));
    }

    [TestMethod]
    public void And_ByteUShort_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((byte?)null, (ushort?)5));
    }

    [TestMethod]
    public void And_ByteInt_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((byte?)null, (int?)5));
    }

    [TestMethod]
    public void And_ByteUInt_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((byte?)null, (uint?)5));
    }

    [TestMethod]
    public void And_ByteLong_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((byte?)null, (long?)5));
    }

    [TestMethod]
    public void And_ByteULong_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And(null, (ulong?)5));
    }

    [TestMethod]
    public void And_SByteSByte_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And(null, (sbyte?)5));
    }

    [TestMethod]
    public void And_IntInt_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((int?)null, (int?)5));
    }

    [TestMethod]
    public void And_LongLong_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((long?)null, (long?)5));
    }

    [TestMethod]
    public void And_ShortShort_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((short?)null, (short?)5));
    }

    [TestMethod]
    public void And_UShortUShort_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((ushort?)null, (ushort?)5));
    }

    [TestMethod]
    public void And_UIntUInt_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((uint?)null, (uint?)5));
    }

    [TestMethod]
    public void And_ULongULong_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.And((ulong?)null, (ulong?)5));
    }

    #endregion

    #region Mixed type Or tests

    [TestMethod]
    public void Or_ByteSByte_Valid_ReturnsOrResult()
    {
        var result = Library.Or((byte?)4, (sbyte?)1);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 5);
    }

    [TestMethod]
    public void Or_ByteSByte_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.Or((byte?)null, (sbyte?)5));
    }

    [TestMethod]
    public void Or_ByteShort_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.Or((byte?)null, (short?)5));
    }

    [TestMethod]
    public void Or_IntInt_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.Or((int?)null, (int?)5));
    }

    [TestMethod]
    public void Or_LongLong_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.Or((long?)null, (long?)5));
    }

    #endregion

    #region Mixed type Xor tests

    [TestMethod]
    public void Xor_ByteSByte_Valid_ReturnsXorResult()
    {
        var result = Library.Xor((byte?)5, (sbyte?)4);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value == 1);
    }

    [TestMethod]
    public void Xor_ByteSByte_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.Xor((byte?)null, (sbyte?)5));
    }

    [TestMethod]
    public void Xor_ByteShort_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.Xor((byte?)null, (short?)5));
    }

    [TestMethod]
    public void Xor_IntInt_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.Xor((int?)null, (int?)5));
    }

    [TestMethod]
    public void Xor_LongLong_LeftNull_ReturnsNull()
    {
        Assert.IsNull(Library.Xor((long?)null, (long?)5));
    }

    #endregion
}
