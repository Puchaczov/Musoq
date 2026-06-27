using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    private static TResult? BitwiseOperation<TLeft, TRight, TResult>(
        TLeft? left,
        TRight? right,
        Func<TLeft, TRight, TResult> operation)
        where TLeft : struct
        where TRight : struct
        where TResult : struct
    {
        return left.HasValue && right.HasValue
            ? operation(left.Value, right.Value)
            : null;
    }

    private static TResult? BitwiseOperation<TValue, TResult>(
        TValue? value,
        Func<TValue, TResult> operation)
        where TValue : struct
        where TResult : struct
    {
        return value.HasValue
            ? operation(value.Value)
            : null;
    }

    private static TResult? BitwiseShift<TValue, TResult>(
        TValue? value,
        int shift,
        Func<TValue, int, TResult> operation)
        where TValue : struct
        where TResult : struct
    {
        return value.HasValue
            ? operation(value.Value, shift)
            : null;
    }
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public byte? ShiftLeft(byte? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => (byte)(value << shift));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public short? ShiftLeft(short? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => (short)(value << shift));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? ShiftLeft(int? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => value << shift);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? ShiftLeft(long? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => value << shift);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public sbyte? ShiftLeft(sbyte? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => (sbyte)(value << shift));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ushort? ShiftLeft(ushort? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => (ushort)(value << shift));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? ShiftLeft(uint? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => value << shift);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? ShiftLeft(ulong? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => value << shift);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public byte? ShiftRight(byte? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => (byte)(value >> shift));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public short? ShiftRight(short? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => (short)(value >> shift));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? ShiftRight(int? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => value >> shift);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? ShiftRight(long? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => value >> shift);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public sbyte? ShiftRight(sbyte? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => (sbyte)(value >> shift));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ushort? ShiftRight(ushort? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => (ushort)(value >> shift));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? ShiftRight(uint? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => value >> shift);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? ShiftRight(ulong? value, int shift)
        => BitwiseShift(value, shift, static (value, shift) => value >> shift);
}
