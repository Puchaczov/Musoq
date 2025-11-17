using System;

namespace Musoq.Plugins.Lib.TypeConversion;

/// <summary>
/// Interface for type conversion strategies that handle converting object values to specific numeric types.
/// Supports different conversion modes: strict (no precision loss), comparison (lossy), and numeric-only (reject strings).
/// </summary>
internal interface ITypeConverter
{
    /// <summary>
    /// Attempts to convert a value to Int32 with the converter's specific validation rules.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted Int32 value if successful; otherwise, null.</returns>
    int? TryConvertToInt32(object? value);

    /// <summary>
    /// Attempts to convert a value to Int64 with the converter's specific validation rules.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted Int64 value if successful; otherwise, null.</returns>
    long? TryConvertToInt64(object? value);

    /// <summary>
    /// Attempts to convert a value to Decimal with the converter's specific validation rules.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted Decimal value if successful; otherwise, null.</returns>
    decimal? TryConvertToDecimal(object? value);

    /// <summary>
    /// Attempts to convert a value to Double with the converter's specific validation rules.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted Double value if successful; otherwise, null.</returns>
    double? TryConvertToDouble(object? value);
}
