using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public partial class LibraryBase
{
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(string? value)
    {
        return short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(byte? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(sbyte? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(short? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(ushort? value) => ToInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(int? value) => ToInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(uint? value) => ToInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(long? value) => ToInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(ulong? value) => ToInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(float? value) => ToInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(double? value) => ToInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(decimal? value) => ToInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(bool? value) => value.HasValue ? value.Value ? (short)1 : (short)0 : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(char? value) => value.HasValue ? ToInt16((ushort)value.Value) : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public short? ToInt16(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        try
        {
            return Convert.ToInt16(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static short? ToInt16Core(ushort? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static short? ToInt16Core(int? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static short? ToInt16Core(uint? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static short? ToInt16Core(long? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static short? ToInt16Core(ulong? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static short? ToInt16Core(float? value)
    {
        if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static short? ToInt16Core(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static short? ToInt16Core(decimal? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }
}
