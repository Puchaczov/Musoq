using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public byte? And(byte? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)(left & right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(byte? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(byte? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(byte? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(byte? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? And(byte? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? And(byte? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? And(byte? left, ulong? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(sbyte? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public sbyte? And(sbyte? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => (sbyte)(left & right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(sbyte? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(sbyte? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(sbyte? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? And(sbyte? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => (uint)(left & right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? And(sbyte? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
}
