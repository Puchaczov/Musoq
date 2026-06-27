using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(short? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(short? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public short? Xor(short? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => (short)(left ^ right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(short? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(short? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Xor(short? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Xor(short? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(ushort? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(ushort? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ (byte)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(ushort? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ (ushort)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ushort? Xor(ushort? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)(left ^ right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(ushort? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Xor(ushort? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Xor(ushort? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Xor(ushort? left, ulong? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
}
