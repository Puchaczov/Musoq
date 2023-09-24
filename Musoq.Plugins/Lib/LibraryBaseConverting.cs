using System;
using System.Globalization;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        /// <summary>
        /// Converts given bytes to hex with defined delimiter
        /// </summary>
        /// <param name="bytes">The bytes</param>
        /// <param name="delimiter">The delimiter</param>
        /// <returns>Hex representation of a given bytes</returns>
        [BindableMethod]
        public string? ToHex(byte[]? bytes, string delimiter = "")
        {
            if (bytes == null)
                return null;

            var hexBuilder = new StringBuilder();

            if (bytes.Length > 0)
            {
                for (int i = 0; i < bytes.Length - 1; i++)
                {
                    var byteValue = bytes[i];
                    hexBuilder.Append(byteValue.ToString("X2"));
                    hexBuilder.Append(delimiter);
                }

                hexBuilder.Append(bytes[^1].ToString("X2"));
            }

            return hexBuilder.ToString();
        }

        /// <summary>
        /// Converts given bytes to binary with defined delimiter
        /// </summary>
        /// <param name="bytes">The bytes</param>
        /// <param name="delimiter">The delimiter</param>
        /// <returns>Binary representation of a given bytes</returns>
        [BindableMethod]
        public string ToBin(byte[]? bytes, string delimiter = "")
        {
            if (bytes == null)
                return null;

            var builder = new StringBuilder();

            foreach (var @byte in bytes)
            {
                var binaryRepresentation = Convert
                    .ToString(@byte, 2)
                    .PadLeft(8, '0');

                builder.Append(binaryRepresentation);
                builder.Append(delimiter);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts given value to binary
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Hex representation of a given bytes</returns>
        [BindableMethod]
        public string? ToHex<T>(T value) where T : IConvertible
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    return ToHex(GetBytes(value.ToBoolean(CultureInfo.CurrentCulture)));
                case TypeCode.Byte:
                    return ToHex(GetBytes(value.ToByte(CultureInfo.CurrentCulture)));
                case TypeCode.Char:
                    return ToHex(GetBytes(value.ToChar(CultureInfo.CurrentCulture)));
                case TypeCode.DateTime:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.DBNull:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.Decimal:
                    return ToHex(GetBytes(value.ToDecimal(CultureInfo.CurrentCulture)));
                case TypeCode.Double:
                    return ToHex(GetBytes(value.ToDouble(CultureInfo.CurrentCulture)));
                case TypeCode.Empty:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.Int16:
                    return ToHex(GetBytes(value.ToInt16(CultureInfo.CurrentCulture)));
                case TypeCode.Int32:
                    return ToHex(GetBytes(value.ToInt32(CultureInfo.CurrentCulture)));
                case TypeCode.Int64:
                    return ToHex(GetBytes(value.ToInt64(CultureInfo.CurrentCulture)));
                case TypeCode.Object:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.SByte:
                    return ToHex(GetBytes(value.ToSByte(CultureInfo.CurrentCulture)));
                case TypeCode.Single:
                    return ToHex(GetBytes(value.ToSingle(CultureInfo.CurrentCulture)));
                case TypeCode.String:
                    return ToHex(GetBytes(value.ToString(CultureInfo.CurrentCulture)));
                case TypeCode.UInt16:
                    return ToHex(GetBytes(value.ToUInt16(CultureInfo.CurrentCulture)));
                case TypeCode.UInt32:
                    return ToHex(GetBytes(value.ToUInt32(CultureInfo.CurrentCulture)));
                case TypeCode.UInt64:
                    return ToHex(GetBytes(value.ToUInt64(CultureInfo.CurrentCulture)));
            }

            return "CONVERSION_NOT_SUPPORTED";
        }

        /// <summary>
        /// Converts given value to binary
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Binary representation of a given bytes</returns>
        [BindableMethod]
        public string ToBin<T>(T value) where T : IConvertible
        {
            return ToBase(value, 2);
        }

        /// <summary>
        /// Converts given value to octal
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Hex representation of a given bytes</returns>
        [BindableMethod]
        public string ToOcta<T>(T value) where T : IConvertible
        {
            return ToBase(value, 8);
        }

        /// <summary>
        /// Converts given value to decimal
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Decimal representation of a given value</returns>
        [BindableMethod]
        public string ToDec<T>(T value) where T : IConvertible
        {
            return ToBase(value, 10);
        }

        /// <summary>
        /// Converts given value to decimal
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to decimal value</returns>
        [BindableMethod]
        public decimal? ToDecimal(string? value)
        {
            if (value == null)
                return null;

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal result))
                return result;

            return null;
        }

        /// <summary>
        /// Converts given value to decimal withing given culture
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="culture">The culture</param>
        /// <returns>Converted to decimal value</returns>
        [BindableMethod]
        public decimal? ToDecimal(string value, string culture)
        {
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo(culture), out decimal result))
                return result;

            return null;
        }

        /// <summary>
        /// Converts given value to TimeSpan
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to TimeSpan value</returns>
        [BindableMethod]
        public TimeSpan? ToTimeSpan(string value) => ToTimeSpan(value, CultureInfo.CurrentCulture.Name);

        /// <summary>
        /// Converts given value to TimeSpan
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="culture">The culture</param>
        /// <returns>Converted to TimeSpan value</returns>
        [BindableMethod]
        public TimeSpan? ToTimeSpan(string value, string culture)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (!TimeSpan.TryParse(value, CultureInfo.GetCultureInfo(culture), out var result))
                return null;

            return result;
        }
        
        /// <summary>
        /// Converts given value to DateTime
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to DateTime value</returns>
        [BindableMethod]
        public DateTime? ToDateTime(string value) => ToDateTime(value, CultureInfo.CurrentCulture.Name);

        /// <summary>
        /// Converts given value to DateTime
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="culture">The culture</param>
        /// <returns>Converted to DateTime value</returns>
        [BindableMethod]
        public DateTime? ToDateTime(string value, string culture)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (!DateTime.TryParse(value, CultureInfo.GetCultureInfo(culture), DateTimeStyles.None, out var result))
                return null;

            return result;
        }

        /// <summary>
        /// Converts given value to Decimal
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to Decimal value</returns>
        [BindableMethod]
        public decimal? ToDecimal(long? value)
        {
            return value;
        }

        /// <summary>
        /// Converts given value to Decimal
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to Decimal value</returns>
        [BindableMethod]
        public decimal? ToDecimal(double? value)
        {
            if (value == null)
                return null;

            return Convert.ToDecimal(value.Value);
        }

        /// <summary>
        /// Converts given value to Decimal
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to Decimal value</returns>
        [BindableMethod]
        public decimal? ToDecimal(object? value)
        {
            if (value == null)
                return null;

            return Convert.ToDecimal(value);
        }

        /// <summary>
        /// Converts given value to Int64
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to Int64 value</returns>
        [BindableMethod]
        public long? ToInt64(string value)
        {
            if (long.TryParse(value, out var number))
                return number;

            return null;
        }

        /// <summary>
        /// Converts given value to long
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to long value</returns>
        [BindableMethod]
        public long? ToInt64(long? value)
        {
            return value;
        }

        /// <summary>
        /// Converts given value to long
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to long value</returns>
        [BindableMethod]
        public long? ToInt64(decimal? value)
        {
            if (value == null)
                return null;

            return Convert.ToInt64(value.Value);
        }

        /// <summary>
        /// Converts given value to long
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to long value</returns>
        [BindableMethod]
        public long? ToInt64(object? value)
        {
            if (value == null)
                return null;

            return Convert.ToInt64(value);
        }

        /// <summary>
        /// Converts given value to int
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to int value</returns>
        [BindableMethod]
        public int? ToInt32(string value)
        {
            if (int.TryParse(value, out var number))
                return number;

            return null;
        }

        /// <summary>
        /// Converts given value to int
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to int value</returns>
        [BindableMethod]
        public int? ToInt32(long? value)
        {
            if (value == null)
                return null;

            return Convert.ToInt32(value.Value);
        }

        /// <summary>
        /// Converts given value to int
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to int value</returns>
        [BindableMethod]
        public int? ToInt32(decimal? value)
        {
            if (value == null)
                return null;

            return Convert.ToInt32(value.Value);
        }
        
        /// <summary>
        /// Converts given value to int
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to int value</returns>
        [BindableMethod]
        public int? ToInt32(object? value)
        {
            if (value == null)
                return null;

            return Convert.ToInt32(value);
        }

        /// <summary>
        /// Converts given value to character
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to character value</returns>
        [BindableMethod]
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
        public char? ToChar(int? value)
        {
            if (value == null)
                return null;

            return (char)value;
        }

        /// <summary>
        /// Converts given value to character
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to character value</returns>
        [BindableMethod]
        public char? ToChar(short? value)
        {
            if (value == null)
                return null;

            return (char)value;
        }

        /// <summary>
        /// Converts given value to character
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to character value</returns>
        [BindableMethod]
        public char? ToChar(byte? value)
        {
            if (value == null)
                return null;

            return (char)value;
        }
        
        /// <summary>
        /// Converts given value to character
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to character value</returns>
        [BindableMethod]
        public char? ToChar(object? value)
        {
            if (value == null)
                return null;

            return Convert.ToChar(value);
        }

        /// <summary>
        /// Converts given value to string
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to string value</returns>
        [BindableMethod]
        public string? ToString(char? value)
        {
            if (value == null)
                return null;

            return value.ToString();
        }

        /// <summary>
        /// Converts given value to string
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to string value</returns>
        [BindableMethod]
        public string? ToString(DateTimeOffset? value)
        {
            return value?.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Converts given value to string
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to string value</returns>
        [BindableMethod]
        public string? ToString(decimal? value)
        {
            return value?.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Converts given value to string
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to string value</returns>
        [BindableMethod]
        public string? ToString(long? value)
        {
            return value?.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Converts given value to string
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to string value</returns>
        [BindableMethod]
        public string? ToString(object? value)
        {
            if (value == null)
                return null;

            return value.ToString();
        }

        /// <summary>
        /// Converts given value to string
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to string value</returns>
        [BindableMethod]
        public string ToString<T>(T? value)
            where T : class
        {
            if (value == default(T))
                return null;

            return value.ToString();
        }

        /// <summary>
        /// Converts given value to string
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to string value</returns>
        [BindableMethod]
        public string ToString(string[] value)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < value.Length - 1; ++i)
            {
                builder.Append(value[i]);
                builder.Append(',');
            }

            if (value.Length > 0)
            {
                builder.Append(value[^1]);
            }

            return builder.ToString();
        }
        
        /// <summary>
        /// Converts given value to string
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to string value</returns>
        [BindableMethod]
        public string ToString<T>(T[] value)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < value.Length - 1; ++i)
            {
                builder.Append(value[i]);
                builder.Append(',');
            }

            if (value.Length > 0)
            {
                builder.Append(value[^1]);
            }

            return builder.ToString();
        }
        
        /// <summary>
        /// Converts given value to string
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to string value</returns>
        [BindableMethod]
        public string ToBase64(byte[] value)
        {
            return Convert.ToBase64String(value, Base64FormattingOptions.None);
        }

        /// <summary>
        /// Converts given array of bytes to string
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="offset">The offset of bytes</param>
        /// <param name="length">The length of bytes</param>
        /// <returns>Converted to base64 value</returns>
        [BindableMethod]
        public string ToBase64(byte[] value, int offset, int length)
        {
            return Convert.ToBase64String(value, offset, length, Base64FormattingOptions.None);
        }

        /// <summary>
        /// Converts given string to bytes array
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Converted to base64 value</returns>
        [BindableMethod]
        public byte[] FromBase64(string value)
        {
            return Convert.FromBase64String(value);
        }

        private string ToBase<T>(T value, int baseNumber) where T : IConvertible
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    return Convert.ToString(value.ToBoolean(CultureInfo.CurrentCulture) ? 1 : 0, baseNumber);
                case TypeCode.Byte:
                    return Convert.ToString(value.ToByte(CultureInfo.CurrentCulture), baseNumber);
                case TypeCode.Char:
                    return Convert.ToString(value.ToChar(CultureInfo.CurrentCulture), baseNumber);
                case TypeCode.DateTime:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.DBNull:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.Decimal:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.Double:
                    return Convert.ToString(BitConverter.DoubleToInt64Bits(value.ToDouble(CultureInfo.CurrentCulture)), baseNumber);
                case TypeCode.Empty:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.Int16:
                    return Convert.ToString(value.ToInt16(CultureInfo.CurrentCulture), baseNumber);
                case TypeCode.Int32:
                    return Convert.ToString(value.ToInt32(CultureInfo.CurrentCulture), baseNumber);
                case TypeCode.Int64:
                    return Convert.ToString(value.ToInt64(CultureInfo.CurrentCulture), baseNumber);
                case TypeCode.Object:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.SByte:
                    return Convert.ToString(value.ToSByte(CultureInfo.CurrentCulture), baseNumber);
                case TypeCode.Single:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.String:
                    return "CONVERSION_NOT_SUPPORTED";
                case TypeCode.UInt16:
                    return Convert.ToString(value.ToUInt16(CultureInfo.CurrentCulture), baseNumber);
                case TypeCode.UInt32:
                    return Convert.ToString(value.ToUInt32(CultureInfo.CurrentCulture), baseNumber);
                case TypeCode.UInt64:
                    return "CONVERSION_NOT_SUPPORTED";
            }

            return "CONVERSION_NOT_SUPPORTED";
        }
    }
}
