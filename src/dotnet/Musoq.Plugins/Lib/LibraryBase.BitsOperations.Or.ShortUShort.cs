using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(short? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(short? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)left | (byte)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public short? Or(short? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => (short)(left | right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(short? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(short? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Or(short? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Or(short? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(ushort? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(ushort? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left | (byte)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(ushort? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left | (ushort)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ushort? Or(ushort? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => (ushort)(left | right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(ushort? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Or(ushort? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Or(ushort? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Or(ushort? left, ulong? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
}
