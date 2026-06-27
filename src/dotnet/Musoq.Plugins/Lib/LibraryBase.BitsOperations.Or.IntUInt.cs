using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(int? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(int? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left | (byte)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(int? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left | (ushort)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(int? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(int? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Or(int? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => (uint)left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Or(int? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => (uint)left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Or(uint? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Or(uint? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Or(uint? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left | (byte)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Or(uint? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left | (ushort)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Or(uint? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Or(uint? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left | (uint)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Or(uint? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => (ulong)(left | right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Or(uint? left, ulong? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
}
