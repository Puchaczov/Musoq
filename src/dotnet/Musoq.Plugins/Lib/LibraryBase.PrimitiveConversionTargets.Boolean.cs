using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public partial class LibraryBase
{
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(string? value)
    {
        return bool.TryParse(value, out var result)
            ? result
            : null;
    }

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(byte? value) => value.HasValue ? value.Value != 0 : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(sbyte? value) => value.HasValue ? value.Value != 0 : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(short? value) => value.HasValue ? value.Value != 0 : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(ushort? value) => value.HasValue ? value.Value != 0 : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(int? value) => value.HasValue ? value.Value != 0 : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(uint? value) => value.HasValue ? value.Value != 0 : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(long? value) => value.HasValue ? value.Value != 0 : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(ulong? value) => value.HasValue ? value.Value != 0 : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(float? value)
    {
        if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            return null;

        return value.Value != 0f;
    }

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        return value.Value != 0d;
    }

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(decimal? value) => value.HasValue ? value.Value != 0m : null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(char? value) => null;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(bool? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public bool? ToBoolean(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        try
        {
            return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}
