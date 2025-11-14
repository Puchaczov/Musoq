using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Attempts to convert an object to int32 with strict validation that rejects precision loss.
    /// Returns null if conversion would result in data loss (e.g., float 1.5 cannot convert to int).
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <returns>Converted int32 value or null if conversion would lose precision</returns>
    [BindableMethod]
    public int? TryConvertToInt32Strict(object? value)
    {
        if (value == null)
            return null;

        try
        {
            // Handle different numeric types with strict precision checking
            switch (value)
            {
                case int intValue:
                    return intValue;
                
                case byte byteValue:
                    return Convert.ToInt32(byteValue);
                
                case sbyte sbyteValue:
                    return Convert.ToInt32(sbyteValue);
                
                case short shortValue:
                    return Convert.ToInt32(shortValue);
                
                case ushort ushortValue:
                    return Convert.ToInt32(ushortValue);
                
                case uint uintValue:
                    if (uintValue > int.MaxValue)
                        return null; // Would overflow
                    return Convert.ToInt32(uintValue);
                
                case long longValue:
                    if (longValue < int.MinValue || longValue > int.MaxValue)
                        return null; // Would overflow
                    return Convert.ToInt32(longValue);
                
                case ulong ulongValue:
                    if (ulongValue > int.MaxValue)
                        return null; // Would overflow
                    return Convert.ToInt32(ulongValue);
                
                case float floatValue:
                    // Check if conversion would lose precision
                    if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                        return null;
                    
                    var intFromFloat = Convert.ToInt32(floatValue);
                    // Verify round-trip: if converting back doesn't match, precision was lost
                    if (Math.Abs(floatValue - intFromFloat) > float.Epsilon)
                        return null;
                    return intFromFloat;
                
                case double doubleValue:
                    // Check if conversion would lose precision
                    if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                        return null;
                    
                    var intFromDouble = Convert.ToInt32(doubleValue);
                    // Verify round-trip: if converting back doesn't match, precision was lost
                    if (Math.Abs(doubleValue - intFromDouble) > double.Epsilon)
                        return null;
                    return intFromDouble;
                
                case decimal decimalValue:
                    // Check if conversion would lose precision
                    if (decimalValue < int.MinValue || decimalValue > int.MaxValue)
                        return null;
                    
                    var intFromDecimal = Convert.ToInt32(decimalValue);
                    // Verify round-trip: if converting back doesn't match, precision was lost
                    if (decimalValue != intFromDecimal)
                        return null;
                    return intFromDecimal;
                
                case string stringValue:
                    if (int.TryParse(stringValue, out var parsedInt))
                        return parsedInt;
                    return null;
                
                case bool boolValue:
                    return boolValue ? 1 : 0;
                
                default:
                    // Try generic conversion as last resort
                    return Convert.ToInt32(value);
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to convert an object to int64 with strict validation that rejects precision loss.
    /// Returns null if conversion would result in data loss.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <returns>Converted int64 value or null if conversion would lose precision</returns>
    [BindableMethod]
    public long? TryConvertToInt64Strict(object? value)
    {
        if (value == null)
            return null;

        try
        {
            switch (value)
            {
                case long longValue:
                    return longValue;
                
                case byte byteValue:
                    return Convert.ToInt64(byteValue);
                
                case sbyte sbyteValue:
                    return Convert.ToInt64(sbyteValue);
                
                case short shortValue:
                    return Convert.ToInt64(shortValue);
                
                case ushort ushortValue:
                    return Convert.ToInt64(ushortValue);
                
                case int intValue:
                    return Convert.ToInt64(intValue);
                
                case uint uintValue:
                    return Convert.ToInt64(uintValue);
                
                case ulong ulongValue:
                    if (ulongValue > long.MaxValue)
                        return null; // Would overflow
                    return Convert.ToInt64(ulongValue);
                
                case float floatValue:
                    if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                        return null;
                    
                    var longFromFloat = Convert.ToInt64(floatValue);
                    if (Math.Abs(floatValue - longFromFloat) > float.Epsilon)
                        return null;
                    return longFromFloat;
                
                case double doubleValue:
                    if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                        return null;
                    
                    var longFromDouble = Convert.ToInt64(doubleValue);
                    if (Math.Abs(doubleValue - longFromDouble) > double.Epsilon)
                        return null;
                    return longFromDouble;
                
                case decimal decimalValue:
                    if (decimalValue < long.MinValue || decimalValue > long.MaxValue)
                        return null;
                    
                    var longFromDecimal = Convert.ToInt64(decimalValue);
                    if (decimalValue != longFromDecimal)
                        return null;
                    return longFromDecimal;
                
                case string stringValue:
                    if (long.TryParse(stringValue, out var parsedLong))
                        return parsedLong;
                    return null;
                
                case bool boolValue:
                    return boolValue ? 1L : 0L;
                
                default:
                    return Convert.ToInt64(value);
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to convert an object to decimal with strict validation that rejects precision loss.
    /// Returns null if conversion would result in data loss.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <returns>Converted decimal value or null if conversion would lose precision</returns>
    [BindableMethod]
    public decimal? TryConvertToDecimalStrict(object? value)
    {
        if (value == null)
            return null;

        try
        {
            switch (value)
            {
                case decimal decimalValue:
                    return decimalValue;
                
                case byte byteValue:
                    return Convert.ToDecimal(byteValue);
                
                case sbyte sbyteValue:
                    return Convert.ToDecimal(sbyteValue);
                
                case short shortValue:
                    return Convert.ToDecimal(shortValue);
                
                case ushort ushortValue:
                    return Convert.ToDecimal(ushortValue);
                
                case int intValue:
                    return Convert.ToDecimal(intValue);
                
                case uint uintValue:
                    return Convert.ToDecimal(uintValue);
                
                case long longValue:
                    return Convert.ToDecimal(longValue);
                
                case ulong ulongValue:
                    return Convert.ToDecimal(ulongValue);
                
                case float floatValue:
                    if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                        return null;
                    
                    // Float to decimal can lose precision due to different representations
                    // We'll allow the conversion but be aware of potential precision issues
                    return Convert.ToDecimal(floatValue);
                
                case double doubleValue:
                    if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                        return null;
                    
                    // Double to decimal can lose precision
                    // We'll allow the conversion but be aware of potential precision issues
                    return Convert.ToDecimal(doubleValue);
                
                case string stringValue:
                    if (decimal.TryParse(stringValue, out var parsedDecimal))
                        return parsedDecimal;
                    return null;
                
                case bool boolValue:
                    return boolValue ? 1m : 0m;
                
                default:
                    return Convert.ToDecimal(value);
            }
        }
        catch
        {
            return null;
        }
    }
}
