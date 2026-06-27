using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public partial class LibraryBase
{
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(int? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(uint? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(decimal? value) => value;

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToSingle(value.Value);
        }
        catch
        {
            return null;
        }
    }

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        if (value is string stringValue)
            return ToFloat(stringValue);

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

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(short? value) => value?.ToString(CultureInfo.InvariantCulture);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(short? value, string format) => value?.ToString(format, CultureInfo.InvariantCulture);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(ushort? value) => value?.ToString(CultureInfo.InvariantCulture);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(ushort? value, string format) => value?.ToString(format, CultureInfo.InvariantCulture);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(DateTime? value) => value?.ToString(CultureInfo.InvariantCulture);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(DateTime? value, string? format) =>
        value?.ToString(format ?? "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(DateTime? value, string? format, string? culture) =>
        value?.ToString(format ?? "dd.MM.yyyy HH:mm:ss", CultureInfo.GetCultureInfo(culture ?? "en-EN"));
}
