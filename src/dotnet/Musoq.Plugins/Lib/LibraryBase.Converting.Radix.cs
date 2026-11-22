using System.Globalization;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given bytes to binary with defined delimiter
    /// </summary>
    /// <param name="bytes">The bytes</param>
    /// <param name="delimiter">The delimiter</param>
    /// <returns>Binary representation of a given bytes</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToBin(byte[]? bytes, string delimiter = "")
    {
        if (bytes == null)
            return null;

        var builder = new StringBuilder();

        foreach (var @byte in bytes)
        {
            var binaryRepresentation = Convert.ToString(@byte, 2).PadLeft(8, '0');

            builder.Append(binaryRepresentation);
            builder.Append(delimiter);
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Converts given value to binary
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Binary representation of a given bytes</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string ToBin<T>(T value)
        where T : IConvertible
    {
        return ToBase(value, 2);
    }

    /// <summary>
    ///     Converts a nullable value to binary with the same behavior as its underlying type.
    /// </summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToBin<T>(T? value)
        where T : struct, IConvertible => value.HasValue ? ToBin(value.GetValueOrDefault()) : null;

    /// <summary>
    ///     Converts given value to octal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Hex representation of a given bytes</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string ToOcta<T>(T value)
        where T : IConvertible
    {
        return ToBase(value, 8);
    }

    /// <summary>
    ///     Converts a nullable value to octal with the same behavior as its underlying type.
    /// </summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToOcta<T>(T? value)
        where T : struct, IConvertible => value.HasValue ? ToOcta(value.GetValueOrDefault()) : null;

    /// <summary>
    ///     Converts given value to decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Decimal representation of a given value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string ToDec<T>(T value)
        where T : IConvertible
    {
        return ToBase(value, 10);
    }

    /// <summary>
    ///     Converts a nullable value to decimal with the same behavior as its underlying type.
    /// </summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToDec<T>(T? value)
        where T : struct, IConvertible => value.HasValue ? ToDec(value.GetValueOrDefault()) : null;

    /// <summary>
    ///     Converts hexadecimal string to long integer
    /// </summary>
    /// <param name="value">The hexadecimal string (with or without 0x prefix)</param>
    /// <returns>Converted long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? FromHex(string? value) => FromBase(value, "0x", 16);

    /// <summary>
    ///     Converts binary string to long integer
    /// </summary>
    /// <param name="value">The binary string (with or without 0b prefix)</param>
    /// <returns>Converted long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? FromBin(string? value) => FromBase(value, "0b", 2);

    /// <summary>
    ///     Converts octal string to long integer
    /// </summary>
    /// <param name="value">The octal string (with or without 0o prefix)</param>
    /// <returns>Converted long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? FromOct(string? value) => FromBase(value, "0o", 8);

    private static long? FromBase(string? value, string prefix, int radix)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = value.Trim();
        if (digits.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            digits = digits.Substring(prefix.Length);

        try
        {
            return Convert.ToInt64(digits, radix);
        }
        catch
        {
            return null;
        }
    }

    private static string ToBase<T>(T value, int baseNumber)
        where T : IConvertible
    {
        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Boolean => Convert.ToString(value.ToBoolean(CultureInfo.CurrentCulture) ? 1 : 0, baseNumber),
            TypeCode.Byte => Convert.ToString(value.ToByte(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.Char => Convert.ToString(value.ToChar(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.DateTime => "CONVERSION_NOT_SUPPORTED",
            TypeCode.DBNull => "CONVERSION_NOT_SUPPORTED",
            TypeCode.Decimal => "CONVERSION_NOT_SUPPORTED",
            TypeCode.Double => Convert.ToString(
                BitConverter.DoubleToInt64Bits(value.ToDouble(CultureInfo.CurrentCulture)),
                baseNumber
            ),
            TypeCode.Empty => "CONVERSION_NOT_SUPPORTED",
            TypeCode.Int16 => Convert.ToString(value.ToInt16(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.Int32 => Convert.ToString(value.ToInt32(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.Int64 => Convert.ToString(value.ToInt64(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.Object => "CONVERSION_NOT_SUPPORTED",
            TypeCode.SByte => Convert.ToString(value.ToSByte(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.Single => "CONVERSION_NOT_SUPPORTED",
            TypeCode.String => "CONVERSION_NOT_SUPPORTED",
            TypeCode.UInt16 => Convert.ToString(value.ToUInt16(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.UInt32 => Convert.ToString(value.ToUInt32(CultureInfo.CurrentCulture), baseNumber),
            TypeCode.UInt64 => "CONVERSION_NOT_SUPPORTED",
            _ => "CONVERSION_NOT_SUPPORTED",
        };
    }
}
