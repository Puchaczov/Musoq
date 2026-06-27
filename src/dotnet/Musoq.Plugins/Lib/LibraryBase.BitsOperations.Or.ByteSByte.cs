using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public byte? Or(byte? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)(left | right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(byte? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => left | (byte)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(byte? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => left | (ushort)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(byte? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(byte? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Or(byte? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Or(byte? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Or(byte? left, ulong? right)
        => BitwiseOperation(left, right, static (left, right) => left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(sbyte? left, byte? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public sbyte? Or(sbyte? left, sbyte? right)
        => BitwiseOperation(left, right, static (left, right) => (sbyte)(left | right));
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(sbyte? left, short? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)left | (ushort)right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(sbyte? left, ushort? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Or(sbyte? left, int? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Or(sbyte? left, uint? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)left | right);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Or(sbyte? left, long? right)
        => BitwiseOperation(left, right, static (left, right) => (byte)left | right);
}
