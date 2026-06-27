using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public partial class LibraryBase
{
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(string? value)
    {
        return ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(byte? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(sbyte? value) => ToUInt64Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(short? value) => ToUInt64Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(ushort? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(int? value) => ToUInt64Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(uint? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(long? value) => ToUInt64Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(ulong? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(float? value) => ToUInt64Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(double? value) => ToUInt64Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(decimal? value) => ToUInt64Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(bool? value) => value.HasValue ? value.Value ? 1ul : 0ul : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(char? value) => value.HasValue ? value.Value : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public ulong? ToUInt64(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        try
        {
            return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static ulong? ToUInt64Core(sbyte? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt64(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ulong? ToUInt64Core(short? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt64(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ulong? ToUInt64Core(int? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt64(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ulong? ToUInt64Core(long? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt64(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ulong? ToUInt64Core(float? value)
    {
        if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToUInt64(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ulong? ToUInt64Core(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToUInt64(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static ulong? ToUInt64Core(decimal? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt64(value.Value);
        }
        catch
        {
            return null;
        }
    }
}
