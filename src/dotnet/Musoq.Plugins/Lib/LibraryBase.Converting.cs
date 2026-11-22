using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given bytes to base64 string
    /// </summary>
    /// <param name="value">The bytes to encode</param>
    /// <returns>Base64 encoded string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToBase64(byte[]? value)
    {
        if (value == null)
            return null;

        return Convert.ToBase64String(value, Base64FormattingOptions.None);
    }

    /// <summary>
    ///     Converts given array of bytes to base64 string with offset and length
    /// </summary>
    /// <param name="value">The bytes to encode</param>
    /// <param name="offset">The offset of bytes</param>
    /// <param name="length">The length of bytes</param>
    /// <returns>Base64 encoded string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToBase64(byte[]? value, int offset, int length)
    {
        if (value == null)
            return null;

        return Convert.ToBase64String(value, offset, length, Base64FormattingOptions.None);
    }

    /// <summary>
    ///     Converts given string to base64 encoded string using UTF-8 encoding
    /// </summary>
    /// <param name="value">The string to encode</param>
    /// <returns>Base64 encoded string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToBase64(string? value)
    {
        if (value == null)
            return null;

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value), Base64FormattingOptions.None);
    }

    /// <summary>
    ///     Converts given string to base64 encoded string using specified encoding
    /// </summary>
    /// <param name="value">The string to encode</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>Base64 encoded string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToBase64(string? value, string encodingName)
    {
        if (value == null)
            return null;

        var encoding = Encoding.GetEncoding(encodingName);
        return Convert.ToBase64String(encoding.GetBytes(value), Base64FormattingOptions.None);
    }

    /// <summary>
    ///     Converts base64 encoded string to bytes array
    /// </summary>
    /// <param name="value">The base64 encoded string</param>
    /// <returns>Decoded bytes, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[]? FromBase64(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return Convert.FromBase64String(value);
    }

    /// <summary>
    ///     Converts base64 encoded string directly to a decoded string using UTF-8 encoding
    /// </summary>
    /// <param name="value">The base64 encoded string</param>
    /// <returns>Decoded string, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? FromBase64ToString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var bytes = Convert.FromBase64String(value);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    ///     Converts base64 encoded string directly to a decoded string using specified encoding
    /// </summary>
    /// <param name="value">The base64 encoded string</param>
    /// <param name="encodingName">The encoding name (e.g., "UTF-8", "UTF-16", "ASCII")</param>
    /// <returns>Decoded string, or null if input is null or empty</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? FromBase64ToString(string? value, string encodingName)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var encoding = Encoding.GetEncoding(encodingName);
        var bytes = Convert.FromBase64String(value);
        return encoding.GetString(bytes);
    }
}
