using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Shifts the value to the left by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public byte? ShiftLeft(byte? value, int shift)
        => value.HasValue ? (byte)(value << shift) : null;
    
    /// <summary>
    /// Shifts the value to the left by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public short? ShiftLeft(short? value, int shift)
        => value.HasValue ? (short)(value << shift) : null;
    
    /// <summary>
    /// Shifts the value to the left by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public int? ShiftLeft(int? value, int shift)
        => value << shift;
    
    /// <summary>
    /// Shifts the value to the left by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public long? ShiftLeft(long? value, int shift)
        => value << shift;
    
    /// <summary>
    /// Shifts the value to the left by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public sbyte? ShiftLeft(sbyte? value, int shift)
        => value.HasValue ? (sbyte)(value << shift) : null;

    /// <summary>
    /// Shifts the value to the left by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public ushort? ShiftLeft(ushort? value, int shift)
        => value.HasValue ? (ushort)(value << shift) : null;
    
    /// <summary>
    /// Shifts the value to the left by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public uint? ShiftLeft(uint? value, int shift)
        => value << shift;
    
    /// <summary>
    /// Shifts the value to the left by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public ulong? ShiftLeft(ulong? value, int shift)
        => value << shift;

    /// <summary>
    /// Shifts the value to the right by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public byte? ShiftRight(byte? value, int shift)
        => value.HasValue ? (byte)(value >> shift) : null;
    
    /// <summary>
    /// Shifts the value to the right by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public short? ShiftRight(short? value, int shift)
        => value.HasValue ? (short)(value >> shift) : null;
    
    /// <summary>
    /// Shifts the value to the right by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public int? ShiftRight(int? value, int shift)
        => value >> shift;
    
    /// <summary>
    /// Shifts the value to the right by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public long? ShiftRight(long? value, int shift)
        => value >> shift;
    
    /// <summary>
    /// Shifts the value to the right by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public sbyte? ShiftRight(sbyte? value, int shift)
        =>value.HasValue ? (sbyte)(value >> shift) : null;
    
    /// <summary>
    /// Shifts the value to the right by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public ushort? ShiftRight(ushort? value, int shift)
        => value.HasValue ? (ushort)(value >> shift) : null;
    
    /// <summary>
    /// Shifts the value to the right by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public uint? ShiftRight(uint? value, int shift)
        => value >> shift;
    
    /// <summary>
    /// Shifts the value to the right by the specified number of bits.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="shift">The shift</param>
    /// <returns>Shifted value</returns>
    [BindableMethod]
    public ulong? ShiftRight(ulong? value, int shift)
        => value >> shift;
    
    /// <summary>
    /// Performs bitwise NOT operation on a given value.
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Negated value</returns>
    [BindableMethod]
    public byte? Not(byte? value)
        => value.HasValue ? (byte)~value : null;
    
    /// <summary>
    /// Performs bitwise NOT operation on a given value.
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Negated value</returns>
    [BindableMethod]
    public short? Not(short? value)
        => value.HasValue ? (short)~value : null;
    
    /// <summary>
    /// Performs bitwise NOT operation on a given value.
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Negated value</returns>
    [BindableMethod]
    public int? Not(int? value)
        => ~value;
    
    /// <summary>
    /// Performs bitwise NOT operation on a given value.
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Negated value</returns>
    [BindableMethod]
    public long? Not(long? value)
        => ~value;

    /// <summary>
    /// Performs bitwise NOT operation on a given value.
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Negated value</returns>
    [BindableMethod]
    public sbyte? Not(sbyte? value)
        => value.HasValue ? (sbyte)~value : null;
    
    /// <summary>
    /// Performs bitwise NOT operation on a given value.
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Negated value</returns>
    [BindableMethod]
    public ushort? Not(ushort? value)
        => value.HasValue ? (ushort)~value : null;
    
    /// <summary>
    /// Performs bitwise NOT operation on a given value.
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Negated value</returns>
    [BindableMethod]
    public uint? Not(uint? value)
        => ~value;
    
    /// <summary>
    /// Performs bitwise NOT operation on a given value.
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Negated value</returns>
    [BindableMethod]
    public ulong? Not(ulong? value)
        => ~value;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public byte? And(byte? left, byte? right)
        => left.HasValue && right.HasValue ? (byte?)(left.Value & right.Value) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(byte? left, sbyte? right)
        => left.HasValue && right.HasValue ? left.Value & right.Value : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(byte? left, short? right)
        => left.HasValue && right.HasValue ? left.Value & right.Value : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(byte? left, ushort? right)
        => left.HasValue && right.HasValue ? left.Value & right.Value : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(byte? left, int? right)
        => left.HasValue && right.HasValue ? left.Value & right.Value : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public uint? And(byte? left, uint? right)
        => left.HasValue && right.HasValue ? left.Value & right.Value : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(byte? left, long? right)
        => left.HasValue && right.HasValue ? left.Value & right.Value : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public ulong? And(byte? left, ulong? right)
        => left.HasValue && right.HasValue ? left.Value & right.Value : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(sbyte? left, byte? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public sbyte? And(sbyte? left, sbyte? right)
        => left.HasValue && right.HasValue ? (sbyte)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(sbyte? left, short? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(sbyte? left, ushort? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(sbyte? left, int? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public uint? And(sbyte? left, uint? right)
        => left.HasValue && right.HasValue ? (uint)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(sbyte? left, long? right)
        => left.HasValue && right.HasValue ? (long)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(short? left, byte? right)
        => left.HasValue && right.HasValue ? left & right : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(short? left, sbyte? right)
        => left.HasValue && right.HasValue ? left & right : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public short? And(short? left, short? right)
        => left.HasValue && right.HasValue ? (short)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(short? left, ushort? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(short? left, int? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public uint? And(short? left, uint? right)
        => left.HasValue && right.HasValue ? (uint)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(short? left, long? right)
        => left.HasValue && right.HasValue ? (long)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(ushort? left, byte? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;

    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(ushort? left, sbyte? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;

    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(ushort? left, short? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public ushort? And(ushort? left, ushort? right)
        => left.HasValue && right.HasValue ? (ushort)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(ushort? left, int? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public uint? And(ushort? left, uint? right)
        => left.HasValue && right.HasValue ? (uint)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(ushort? left, long? right)
        => left.HasValue && right.HasValue ? (long)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public ulong? And(ushort? left, ulong? right)
        => left.HasValue && right.HasValue ? (ulong)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(int? left, byte? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(int? left, sbyte? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(int? left, short? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(int? left, ushort? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public int? And(int? left, int? right)
        => left.HasValue && right.HasValue ? (int)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public uint? And(int? left, uint? right)
        => left.HasValue && right.HasValue ? (uint)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(int? left, long? right)
        => left.HasValue && right.HasValue ? (long)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public uint? And(uint? left, uint? right)
        => left.HasValue && right.HasValue ? left & right : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public uint? And(uint? left, byte? right)
        => left.HasValue && right.HasValue ? left & right : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public uint? And(uint? left, sbyte? right)
        => left.HasValue && right.HasValue ? (uint?)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public uint? And(uint? left, short? right)
        => left.HasValue && right.HasValue ? (uint?)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public uint? And(uint? left, ushort? right)
        => left.HasValue && right.HasValue ? left & right : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public uint? And(uint? left, int? right)
        => left.HasValue && right.HasValue ? (uint?)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public ulong? And(uint? left, long? right)
        => left.HasValue && right.HasValue ? (ulong?)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public ulong? And(uint? left, ulong? right)
        => left.HasValue && right.HasValue ? left & right : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(long? left, long? right)
        => left.HasValue && right.HasValue ? (long)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(long? left, byte? right)
        => left.HasValue && right.HasValue ? (long)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(long? left, sbyte? right)
        => left.HasValue && right.HasValue ? (long)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(long? left, short? right)
        => left.HasValue && right.HasValue ? (long)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(long? left, ushort? right)
        => left.HasValue && right.HasValue ? (long)(left & right) : null;

    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(long? left, int? right)
        => left.HasValue && right.HasValue ? (long)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(long? left, uint? right)
        => left.HasValue && right.HasValue ? left & right : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public ulong? And(ulong? left, byte? right)
        => left.HasValue && right.HasValue ? left.Value & right.Value : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public ulong? And(ulong? left, ushort? right)
        => left.HasValue && right.HasValue ? left.Value & right.Value : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public ulong? And(ulong? left, uint? right)
        => left.HasValue && right.HasValue ? left.Value & right.Value : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public ulong? And(ulong? left, ulong? right)
        => left.HasValue && right.HasValue ? left.Value & right.Value : null;
    
    ///START OF OR OPERATIONS
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public byte? Or(byte? left, byte? right)
        => left.HasValue && right.HasValue ? (byte?)(left.Value | right.Value) : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(byte? left, sbyte? right)
        => left.HasValue && right.HasValue ? left.Value | (byte) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(byte? left, short? right)
        => left.HasValue && right.HasValue ? left.Value | (ushort) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(byte? left, ushort? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(byte? left, int? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public uint? Or(byte? left, uint? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(byte? left, long? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public ulong? Or(byte? left, ulong? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(sbyte? left, byte? right)
        => left.HasValue && right.HasValue ? (byte) left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public sbyte? Or(sbyte? left, sbyte? right)
        => left.HasValue && right.HasValue ? (sbyte)(left.Value | right.Value) : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(sbyte? left, short? right)
        => left.HasValue && right.HasValue ? (byte) left.Value | (ushort) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(sbyte? left, ushort? right)
        => left.HasValue && right.HasValue ? (byte) left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(sbyte? left, int? right)
        => left.HasValue && right.HasValue ? (byte) left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public uint? Or(sbyte? left, uint? right)
        => left.HasValue && right.HasValue ? (byte) left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(sbyte? left, long? right)
        => left.HasValue && right.HasValue ? (byte) left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(short? left, byte? right)
        => left.HasValue && right.HasValue ? (ushort) left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(short? left, sbyte? right)
        => left.HasValue && right.HasValue ? (ushort) left.Value | (byte) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public short? Or(short? left, short? right)
        => left.HasValue && right.HasValue ? (short)(left.Value | right.Value) : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(short? left, ushort? right)
        => left.HasValue && right.HasValue ? (ushort) left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(short? left, int? right)
        => left.HasValue && right.HasValue ? (ushort) left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public uint? Or(short? left, uint? right)
        => left.HasValue && right.HasValue ? (ushort) left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(short? left, long? right)
        => left.HasValue && right.HasValue ? (ushort) left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(ushort? left, byte? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;

    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(ushort? left, sbyte? right)
        => left.HasValue && right.HasValue ? left.Value | (byte) right.Value : null;

    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(ushort? left, short? right)
        => left.HasValue && right.HasValue ? left.Value | (ushort) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public ushort? Or(ushort? left, ushort? right)
        => left.HasValue && right.HasValue ? (ushort)(left.Value | right.Value) : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(ushort? left, int? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public uint? Or(ushort? left, uint? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(ushort? left, long? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public ulong? Or(ushort? left, ulong? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(int? left, byte? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(int? left, sbyte? right)
        => left.HasValue && right.HasValue ? left.Value | (byte) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(int? left, short? right)
        => left.HasValue && right.HasValue ? left.Value | (ushort) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(int? left, ushort? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(int? left, int? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public uint? Or(int? left, uint? right)
        => left.HasValue && right.HasValue ? (uint) left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(int? left, long? right)
        => left.HasValue && right.HasValue ? (uint) left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public uint? Or(uint? left, uint? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public uint? Or(uint? left, byte? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public uint? Or(uint? left, sbyte? right)
        => left.HasValue && right.HasValue ? left.Value | (byte) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public uint? Or(uint? left, short? right)
        => left.HasValue && right.HasValue ? left.Value | (ushort) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public uint? Or(uint? left, ushort? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public uint? Or(uint? left, int? right)
        => left.HasValue && right.HasValue ? left.Value | (uint) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public ulong? Or(uint? left, long? right)
        => left.HasValue && right.HasValue ? (ulong?)(left.Value | right.Value) : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public ulong? Or(uint? left, ulong? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(long? left, long? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(long? left, byte? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(long? left, sbyte? right)
        => left.HasValue && right.HasValue ? left.Value | (byte) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(long? left, short? right)
        => left.HasValue && right.HasValue ? left.Value | (ushort) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(long? left, ushort? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;

    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(long? left, int? right)
        => left.HasValue && right.HasValue ? left.Value | (uint) right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(long? left, uint? right)
        => left.HasValue && right.HasValue ? left.Value | right.Value : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public ulong? Or(ulong? left, byte? right)
        => left.HasValue && right.HasValue ? (ulong)(left | right) : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public ulong? Or(ulong? left, ushort? right)
        => left.HasValue && right.HasValue ? (ulong)(left | right) : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public ulong? Or(ulong? left, uint? right)
        => left.HasValue && right.HasValue ? (ulong)(left | right) : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public ulong? Or(ulong? left, ulong? right)
        => left.HasValue && right.HasValue ? (ulong)(left | right) : null;
    
    ///START OF XOR OPERATIONS
    
       /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public byte? Xor(byte? left, byte? right)
        => left.HasValue && right.HasValue ? (byte?)(left.Value ^ right.Value) : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(byte? left, sbyte? right)
        => left.HasValue && right.HasValue ? left.Value ^ (byte) right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(byte? left, short? right)
        => left.HasValue && right.HasValue ? left.Value ^ (ushort) right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(byte? left, ushort? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(byte? left, int? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public uint? Xor(byte? left, uint? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public long? Xor(byte? left, long? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public ulong? Xor(byte? left, ulong? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(sbyte? left, byte? right)
        => left.HasValue && right.HasValue ? (byte) left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public sbyte? Xor(sbyte? left, sbyte? right)
        => left.HasValue && right.HasValue ? (sbyte)(left.Value ^ right.Value) : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(sbyte? left, short? right)
        => left.HasValue && right.HasValue ? (byte) left.Value ^ (ushort) right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(sbyte? left, ushort? right)
        => left.HasValue && right.HasValue ? (byte) left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(sbyte? left, int? right)
        => left.HasValue && right.HasValue ? (byte) left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public uint? Xor(sbyte? left, uint? right)
        => left.HasValue && right.HasValue ? (byte) left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public long? Xor(sbyte? left, long? right)
        => left.HasValue && right.HasValue ? (byte) left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(short? left, byte? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(short? left, sbyte? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public short? Xor(short? left, short? right)
        => left.HasValue && right.HasValue ? (short)(left.Value ^ right.Value) : null;
    
    /// <summary>
    /// Performs bitwise XOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of XOR operation</returns>
    [BindableMethod]
    public int? Xor(short? left, ushort? right)
        => left.HasValue && right.HasValue ? (ushort) left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(short? left, int? right)
        => left.HasValue && right.HasValue ? (ushort) left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public uint? Xor(short? left, uint? right)
        => left.HasValue && right.HasValue ? (ushort) left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public long? Xor(short? left, long? right)
        => left.HasValue && right.HasValue ? (ushort) left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(ushort? left, byte? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;

    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(ushort? left, sbyte? right)
        => left.HasValue && right.HasValue ? left.Value ^ (byte) right.Value : null;

    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(ushort? left, short? right)
        => left.HasValue && right.HasValue ? left.Value ^ (ushort) right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public ushort? Xor(ushort? left, ushort? right)
        => left.HasValue && right.HasValue ? (ushort)(left.Value ^ right.Value) : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(ushort? left, int? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public uint? Xor(ushort? left, uint? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public long? Xor(ushort? left, long? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public ulong? Xor(ushort? left, ulong? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(int? left, byte? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(int? left, sbyte? right)
        => left.HasValue && right.HasValue ? left.Value ^ (byte) right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(int? left, short? right)
        => left.HasValue && right.HasValue ? left.Value ^ (ushort) right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(int? left, ushort? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public int? Xor(int? left, int? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public uint? Xor(int? left, uint? right)
        => left.HasValue && right.HasValue ? (uint) left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public long? Xor(int? left, long? right)
        => left.HasValue && right.HasValue ? (uint) left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public uint? Xor(uint? left, uint? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public uint? Xor(uint? left, byte? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public uint? Xor(uint? left, sbyte? right)
        => left.HasValue && right.HasValue ? left.Value ^ (byte) right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public uint? Xor(uint? left, short? right)
        => left.HasValue && right.HasValue ? left.Value ^ (ushort) right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public uint? Xor(uint? left, ushort? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public uint? Xor(uint? left, int? right)
        => left.HasValue && right.HasValue ? left.Value ^ (uint) right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public ulong? Xor(uint? left, long? right)
        => left.HasValue && right.HasValue ? (ulong?)(left.Value ^ right.Value) : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public ulong? Xor(uint? left, ulong? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public long? Xor(long? left, long? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public long? Xor(long? left, byte? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public long? Xor(long? left, sbyte? right)
        => left.HasValue && right.HasValue ? left.Value ^ (byte) right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public long? Xor(long? left, short? right)
        => left.HasValue && right.HasValue ? left.Value ^ (ushort) right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public long? Xor(long? left, ushort? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;

    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public long? Xor(long? left, int? right)
        => left.HasValue && right.HasValue ? left.Value ^ (uint) right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public long? Xor(long? left, uint? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public ulong? Xor(ulong? left, ulong? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public ulong? Xor(ulong? left, byte? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
    
    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public ulong? Xor(ulong? left, ushort? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;

    /// <summary>
    /// Performs bitwise Xor operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of Xor operation</returns>
    [BindableMethod]
    public ulong? Xor(ulong? left, uint? right)
        => left.HasValue && right.HasValue ? left.Value ^ right.Value : null;
}