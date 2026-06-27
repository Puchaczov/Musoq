using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(int? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(int? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ (byte)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(int? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ (ushort)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(int? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Xor(int? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Xor(int? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => (uint)left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Xor(int? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => (uint)left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Xor(uint? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Xor(uint? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Xor(uint? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ (byte)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Xor(uint? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ (ushort)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Xor(uint? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Xor(uint? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ (uint)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Xor(uint? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => (ulong)(left ^ right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Xor(uint? left, ulong? right)
        => BitwiseOperation(left, right, static (left, right) => left ^ right);
}
