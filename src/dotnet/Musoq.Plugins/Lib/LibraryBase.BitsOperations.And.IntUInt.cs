using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(int? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(int? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(int? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(int? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? And(int? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? And(int? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => (uint)(left & right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? And(int? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? And(uint? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? And(uint? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? And(uint? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => (uint)(left & right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? And(uint? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => (uint)(left & right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? And(uint? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? And(uint? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => (uint)(left & right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? And(uint? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => (ulong)(left & right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? And(uint? left, ulong? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
}
