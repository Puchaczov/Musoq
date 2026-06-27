using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(byte? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(byte? value, string format)
    {
        return value?.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(sbyte? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(sbyte? value, string format)
    {
        return value?.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(int? value, string format)
    {
        return value?.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(uint? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(uint? value, string format)
    {
        return value?.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(long? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(long? value, string format)
    {
        return value?.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(ulong? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(ulong? value, string format)
    {
        return value?.ToString(format, CultureInfo.InvariantCulture);
    }
}
