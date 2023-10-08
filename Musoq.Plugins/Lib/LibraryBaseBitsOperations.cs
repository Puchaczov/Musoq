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
        => left.HasValue && right.HasValue ? (byte)(left & right) : null;
    
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
    public int? And(int? left, int? right)
        => left & right;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public long? And(long? left, long? right)
        => left & right;
    
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
    public ushort? And(ushort? left, ushort? right)
        => left.HasValue && right.HasValue ? (ushort)(left & right) : null;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public uint? And(uint? left, uint? right)
        => left & right;
    
    /// <summary>
    /// Performs bitwise AND operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of AND operation</returns>
    [BindableMethod]
    public ulong? And(ulong? left, ulong? right)
        => left & right;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public byte? Or(byte? left, byte? right)
        => left.HasValue && right.HasValue ? (byte)(left | right) : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public short? Or(short? left, short? right)
        => left.HasValue && right.HasValue ? (short)(left | right) : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public int? Or(int? left, int? right)
        => left | right;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public long? Or(long? left, long? right)
        => left | right;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public sbyte? Or(sbyte? left, sbyte? right)
        => left.HasValue && right.HasValue ? (sbyte)(left | right) : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public ushort? Or(ushort? left, ushort? right)
        => left.HasValue && right.HasValue ? (ushort)(left | right) : null;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public uint? Or(uint? left, uint? right)
        => left | right;
    
    /// <summary>
    /// Performs bitwise OR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>Result of OR operation</returns>
    [BindableMethod]
    public ulong? Or(ulong? left, ulong? right)
        => left | right;
    
    /// <summary>
    /// Performs bitwise XOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XORed value</returns>
    [BindableMethod]
    public byte? Xor(byte? left, byte? right)
        => left.HasValue && right.HasValue ? (byte)(left ^ right) : null;
    
    /// <summary>
    /// Performs bitwise XOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XORed value</returns>
    [BindableMethod]
    public short? Xor(short? left, short? right)
        => left.HasValue && right.HasValue ? (short)(left ^ right) : null;
    
    /// <summary>
    /// Performs bitwise XOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XORed value</returns>
    [BindableMethod]
    public int? Xor(int? left, int? right)
        => left ^ right;
    
    /// <summary>
    /// Performs bitwise XOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XORed value</returns>
    [BindableMethod]
    public long? Xor(long? left, long? right)
        => left ^ right;
    
    /// <summary>
    /// Performs bitwise XOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XORed value</returns>
    [BindableMethod]
    public sbyte? Xor(sbyte? left, sbyte? right)
        => left.HasValue && right.HasValue ? (sbyte)(left ^ right) : null;
    
    /// <summary>
    /// Performs bitwise XOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XORed value</returns>
    [BindableMethod]
    public ushort? Xor(ushort? left, ushort? right)
        => left.HasValue && right.HasValue ? (ushort)(left ^ right) : null;
    
    /// <summary>
    /// Performs bitwise XOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XORed value</returns>
    [BindableMethod]
    public uint? Xor(uint? left, uint? right)
        => left ^ right;
    
    /// <summary>
    /// Performs bitwise XOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XORed value</returns>
    [BindableMethod]
    public ulong? Xor(ulong? left, ulong? right)
        => left ^ right;
    
    /// <summary>
    /// Performs bitwise XNOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XNOR value</returns>
    [BindableMethod]
    public byte? XNor(byte? left, byte? right)
        => left.HasValue && right.HasValue ? (byte)~(left ^ right) : null;
    
    /// <summary>
    /// Performs bitwise XNOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XNOR value</returns>
    [BindableMethod]
    public short? XNor(short? left, short? right)
        => left.HasValue && right.HasValue ? (short)~(left ^ right) : null;
    
    /// <summary>
    /// Performs bitwise XNOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XNOR value</returns>
    [BindableMethod]
    public int? XNor(int? left, int? right)
        => ~(left ^ right);
    
    /// <summary>
    /// Performs bitwise XNOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XNOR value</returns>
    [BindableMethod]
    public long? XNor(long? left, long? right)
        => ~(left ^ right);
    
    /// <summary>
    /// Performs bitwise XNOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XNOR value</returns>
    [BindableMethod]
    public sbyte? XNor(sbyte? left, sbyte? right)
        => left.HasValue && right.HasValue ? (sbyte)~(left ^ right) : null;
    
    /// <summary>
    /// Performs bitwise XNOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XNOR value</returns>
    [BindableMethod]
    public ushort? XNor(ushort? left, ushort? right)
        => left.HasValue && right.HasValue ? (ushort)~(left ^ right) : null;
    
    /// <summary>
    /// Performs bitwise XNOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XNOR value</returns>
    [BindableMethod]
    public uint? XNor(uint? left, uint? right)
        => ~(left ^ right);
    
    /// <summary>
    /// Performs bitwise XNOR operation on two given values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns>XNOR value</returns>
    [BindableMethod]
    public ulong? XNor(ulong? left, ulong? right)
        => ~(left ^ right);
}