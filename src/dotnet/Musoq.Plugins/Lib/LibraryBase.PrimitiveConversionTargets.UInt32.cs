using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public partial class LibraryBase
{
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(string? value)
    {
        return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(byte? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(sbyte? value) => ToUInt32Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(short? value) => ToUInt32Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(ushort? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(int? value) => ToUInt32Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(uint? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(long? value) => ToUInt32Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(ulong? value) => ToUInt32Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(float? value) => ToUInt32Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(double? value) => ToUInt32Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(decimal? value) => ToUInt32Core(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(bool? value) => value.HasValue ? value.Value ? 1u : 0u : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(char? value) => value.HasValue ? value.Value : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public uint? ToUInt32(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        try
        {
            return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static uint? ToUInt32Core(sbyte? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt32(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static uint? ToUInt32Core(short? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt32(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static uint? ToUInt32Core(int? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt32(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static uint? ToUInt32Core(long? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt32(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static uint? ToUInt32Core(ulong? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt32(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static uint? ToUInt32Core(float? value)
    {
        if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToUInt32(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static uint? ToUInt32Core(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToUInt32(value.Value);
        }
        catch
        {
            return null;
        }
    }

    private static uint? ToUInt32Core(decimal? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToUInt32(value.Value);
        }
        catch
        {
            return null;
        }
    }
}
