using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? And(long? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? And(long? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? And(long? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? And(long? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? And(long? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? And(long? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? And(long? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? And(ulong? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? And(ulong? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? And(ulong? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? And(ulong? left, ulong? right)
        => BitwiseOperation(left, right, static (left, right) => left & right);
}
