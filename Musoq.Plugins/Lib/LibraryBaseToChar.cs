using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(string? value)
    {
        if (value == null)
            return null;

        if (value == string.Empty)
            return null;

        return value[0];
    }

    /// <summary>
    /// Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(int? value)
    {
        if (value == null)
            return null;

        return (char) value;
    }

    /// <summary>
    /// Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(short? value)
    {
        if (value == null)
            return null;

        return (char) value;
    }

    /// <summary>
    /// Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(byte? value)
    {
        if (value == null)
            return null;

        return (char) value;
    }

    /// <summary>
    /// Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(object? value)
    {
        if (value == null)
            return null;

        return Convert.ToChar(value);
    }
}