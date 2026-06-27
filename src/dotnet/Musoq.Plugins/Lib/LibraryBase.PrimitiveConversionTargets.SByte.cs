using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public partial class LibraryBase
{
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(string? value)
    {
        return sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(byte? value) => ToSByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(sbyte? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(short? value) => ToSByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(ushort? value) => ToSByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(int? value) => ToSByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(uint? value) => ToSByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(long? value) => ToSByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(ulong? value) => ToSByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(float? value) => ToSByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(double? value) => ToSByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(decimal? value) => ToSByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(bool? value) => value.HasValue ? value.Value ? (sbyte)1 : (sbyte)0 : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(char? value) => value.HasValue ? ToSByte((ushort)value.Value) : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public sbyte? ToSByte(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        try
        {
            return Convert.ToSByte(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static sbyte? ToSByteCore(byte? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToSByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static sbyte? ToSByteCore(short? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToSByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static sbyte? ToSByteCore(ushort? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToSByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static sbyte? ToSByteCore(int? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToSByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static sbyte? ToSByteCore(uint? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToSByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static sbyte? ToSByteCore(long? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToSByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static sbyte? ToSByteCore(ulong? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToSByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static sbyte? ToSByteCore(float? value)
    {
        if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToSByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static sbyte? ToSByteCore(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToSByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static sbyte? ToSByteCore(decimal? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToSByte(value.Value);
        }
        catch
        {
            return null;
        }
    }
}
