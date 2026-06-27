using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public partial class LibraryBase
{
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(string? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(byte? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(sbyte? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(short? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(ushort? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(int? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(uint? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(long? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(ulong? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(float? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(double? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(decimal? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(bool? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(char? value) => ToFloat(value);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToSingle(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        if (value is string stringValue)
            return ToSingle(stringValue);

        try
        {
            var result = Convert.ToSingle(value, CultureInfo.InvariantCulture);
            return float.IsNaN(result) || float.IsInfinity(result) ? null : result;
        }
        catch
        {
            return null;
        }
    }
}
