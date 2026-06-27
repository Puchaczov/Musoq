using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public partial class LibraryBase
{
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(string? value)
    {
        return byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(byte? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(sbyte? value) => ToByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(short? value) => ToByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(ushort? value) => ToByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(int? value) => ToByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(uint? value) => ToByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(long? value) => ToByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(ulong? value) => ToByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(float? value) => ToByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(double? value) => ToByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(decimal? value) => ToByteCore(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(bool? value) => value.HasValue ? value.Value ? (byte)1 : (byte)0 : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(char? value) => value.HasValue ? ToByte((ushort)value.Value) : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte? ToByte(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        try
        {
            return Convert.ToByte(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static byte? ToByteCore(sbyte? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static byte? ToByteCore(short? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static byte? ToByteCore(ushort? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static byte? ToByteCore(int? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static byte? ToByteCore(uint? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static byte? ToByteCore(long? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static byte? ToByteCore(ulong? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static byte? ToByteCore(float? value)
    {
        if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static byte? ToByteCore(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToByte(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static byte? ToByteCore(decimal? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToByte(value.Value);
        }
        catch
        {
            return null;
        }
    }
}
