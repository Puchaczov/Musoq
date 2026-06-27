using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public byte? Xor(byte? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)(left ^ right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(byte? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ (byte)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(byte? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ (ushort)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(byte? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(byte? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Xor(byte? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Xor(byte? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Xor(byte? left, ulong? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(sbyte? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public sbyte? Xor(sbyte? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => (sbyte)(left ^ right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(sbyte? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)left ^ (ushort)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(sbyte? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(sbyte? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Xor(sbyte? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Xor(sbyte? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)left ^ right);
}
