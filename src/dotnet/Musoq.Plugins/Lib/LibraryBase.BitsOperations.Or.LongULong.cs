using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Or(long? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Or(long? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Or(long? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left | (byte)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Or(long? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left | (ushort)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Or(long? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Or(long? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left | (uint)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Or(long? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Or(ulong? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Or(ulong? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Or(ulong? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Or(ulong? left, ulong? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
}
