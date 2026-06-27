using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(string? value)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return null;

        return float.IsNaN(number) || float.IsInfinity(number) ? null : number;
    }

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(byte? value) => value;

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(sbyte? value) => value;

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(short? value) => value;

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(ushort? value) => value;

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(int? value) => value;

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(uint? value) => value;

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(long? value) => value;

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(ulong? value) => value;

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(float? value) => value;

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(decimal? value)
    {
        if (!value.HasValue)
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

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(bool? value) => value.HasValue ? value.Value ? 1f : 0f : null;

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(char? value) => value.HasValue ? value.Value : null;
}
