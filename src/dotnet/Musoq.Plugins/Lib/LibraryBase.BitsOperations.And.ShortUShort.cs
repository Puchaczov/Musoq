using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(short? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(short? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public short? And(short? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => (short)(left & right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(short? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(short? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? And(short? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => (uint)(left & right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? And(short? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(ushort? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(ushort? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(ushort? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ushort? And(ushort? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)(left & right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(ushort? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? And(ushort? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? And(ushort? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? And(ushort? left, ulong? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
}
