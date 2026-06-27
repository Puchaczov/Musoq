using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public partial class LibraryBase
{
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(string? value)
    {
        return ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(byte? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(sbyte? value) => ToUInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(short? value) => ToUInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(ushort? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(int? value) => ToUInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(uint? value) => ToUInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(long? value) => ToUInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(ulong? value) => ToUInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(float? value) => ToUInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(double? value) => ToUInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(decimal? value) => ToUInt16Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(bool? value) => value.HasValue ? value.Value ? (ushort)1 : (ushort)0 : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(char? value) => value.HasValue ? value.Value : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ushort? ToUInt16(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        try
        {
            return Convert.ToUInt16(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static ushort? ToUInt16Core(sbyte? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ushort? ToUInt16Core(short? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ushort? ToUInt16Core(int? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ushort? ToUInt16Core(uint? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ushort? ToUInt16Core(long? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ushort? ToUInt16Core(ulong? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ushort? ToUInt16Core(float? value)
    {
        if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToUInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ushort? ToUInt16Core(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToUInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ushort? ToUInt16Core(decimal? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt16(value.Value);
        }
        catch
        {
            return null;
        }
    }
}
