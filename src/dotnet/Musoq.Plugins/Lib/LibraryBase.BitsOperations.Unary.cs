using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public byte? Not(byte? value)
        => BitwiseOperation(value, static value => (byte)~value);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public short? Not(short? value)
        => BitwiseOperation(value, static value => (short)~value);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public int? Not(int? value)
        => BitwiseOperation(value, static value => ~value);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public long? Not(long? value)
        => BitwiseOperation(value, static value => ~value);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public sbyte? Not(sbyte? value)
        => BitwiseOperation(value, static value => (sbyte)~value);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ushort? Not(ushort? value)
        => BitwiseOperation(value, static value => (ushort)~value);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public uint? Not(uint? value)
        => BitwiseOperation(value, static value => ~value);
    [BindableMethod]
    [MethodCategory(MethodCategories.Bitwise)]
    public ulong? Not(ulong? value)
        => BitwiseOperation(value, static value => ~value);
}
