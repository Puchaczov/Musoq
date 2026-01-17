using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    private readonly Random _rand = new();

    /// <summary>
    ///     Gets the absolute value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Absolute value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public decimal? Abs(decimal? value)
    {
        if (!value.HasValue)
            return null;

        return Math.Abs(value.Value);
    }

    /// <summary>
    ///     Gets the absolute value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Absolute value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public long? Abs(long? value)
    {
        if (!value.HasValue)
            return null;

        return Math.Abs(value.Value);
    }

    /// <summary>
    ///     Gets the absolute value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Absolute value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public int? Abs(int? value)
    {
        if (!value.HasValue)
            return null;

        return Math.Abs(value.Value);
    }

    /// <summary>
    ///     Gets the ceiling value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Ceiling value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public decimal? Ceil(decimal? value)
    {
        if (!value.HasValue)
            return null;

        return Math.Ceiling(value.Value);
    }

    /// <summary>
    ///     Gets the floor value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Floor value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public decimal? Floor(decimal? value)
    {
        if (!value.HasValue)
            return null;

        return Math.Floor(value.Value);
    }

    /// <summary>
    ///     Determine whether value is greater, equal or less that zero
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Is less, equal or greater value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public decimal? Sign(decimal? value)
    {
        if (!value.HasValue)
            return null;

        if (value.Value > 0)
            return 1;
        if (value.Value == 0)
            return 0;

        return -1;
    }

    /// <summary>
    ///     Determine whether value is greater, equal or less that zero
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Is less, equal or greater value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public long? Sign(long? value)
    {
        if (!value.HasValue)
            return null;

        if (value.Value > 0)
            return 1;
        if (value.Value == 0)
            return 0;

        return -1;
    }

    /// <summary>
    ///     Rounds the value within given precision
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="precision">The precision</param>
    /// <returns>Is less, equal or greater value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public decimal? Round(decimal? value, int precision)
    {
        if (!value.HasValue)
            return null;

        return Math.Round(value.Value, precision);
    }

    /// <summary>
    ///     Gets the percentage of the value
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="max">The max</param>
    /// <returns>Percentage of a given value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public decimal? PercentOf(decimal? value, decimal? max)
    {
        if (!value.HasValue)
            return null;

        if (!max.HasValue)
            return null;

        return value * 100 / max;
    }

    /// <summary>
    ///     Gets the random integer value
    /// </summary>
    /// <returns>Random integer</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    [NonDeterministic]
    public int Rand()
    {
        return _rand.Next();
    }

    /// <summary>
    ///     Gets the random integer value
    /// </summary>
    /// <param name="min">The min</param>
    /// <param name="max">The max</param>
    /// <returns>Random value between min and max</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    [NonDeterministic]
    public int? Rand(int? min, int? max)
    {
        if (min == null || max == null)
            return null;

        return _rand.Next(min.Value, max.Value);
    }

    /// <summary>
    ///     Computes the pow between two values
    /// </summary>
    /// <param name="x">The x</param>
    /// <param name="y">The y</param>
    /// <returns>Power of two values</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public double? Pow(decimal? x, decimal? y)
    {
        if (x == null || y == null)
            return null;

        return Math.Pow(Convert.ToDouble(x.Value), Convert.ToDouble(y.Value));
    }

    /// <summary>
    ///     Computes the pow between two values
    /// </summary>
    /// <param name="x">The x</param>
    /// <param name="y">The y</param>
    /// <returns>Power of two values</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public double? Pow(double? x, double? y)
    {
        if (x == null || y == null)
            return null;

        return Math.Pow(x.Value, y.Value);
    }

    /// <summary>
    ///     Computes the sqrt of a given value
    /// </summary>
    /// <param name="x">The x</param>
    /// <returns>Sqrt of a value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public double? Sqrt(decimal? x)
    {
        if (x == null)
            return null;

        return Math.Sqrt(Convert.ToDouble(x.Value));
    }

    /// <summary>
    ///     Computes the sqrt of a given value
    /// </summary>
    /// <param name="x">The x</param>
    /// <returns>Sqrt of a value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public double? Sqrt(double? x)
    {
        if (x == null)
            return null;

        return Math.Sqrt(x.Value);
    }

    /// <summary>
    ///     Computes the sqrt of a given value
    /// </summary>
    /// <param name="x">The x</param>
    /// <returns>Sqrt of a value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public double? Sqrt(long? x)
    {
        if (x == null)
            return null;

        return Math.Sqrt(x.Value);
    }

    /// <summary>
    ///     Computes the percent rank of a given window
    /// </summary>
    /// <param name="window">The window</param>
    /// <param name="value">The value existing in a given window</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Percent rank of a given value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public double? PercentRank<T>(IEnumerable<T>? window, T? value)
        where T : IComparable<T>
    {
        if (window == null || value == null)
            return null;

        var orderedWindow = window.OrderBy(w => w).ToArray();
        var index = Array.IndexOf(orderedWindow, value);

        return (index - 1) / (orderedWindow.Length - 1);
    }

    /// <summary>
    ///     Calculates the logarithm of a value with a specified base.
    /// </summary>
    /// <param name="base">The base of the logarithm.</param>
    /// <param name="value">The value to calculate the logarithm for.</param>
    /// <returns>The logarithm of the value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static double? Log(decimal? @base, decimal? value)
    {
        if (!@base.HasValue || !value.HasValue || @base <= 0 || @base == 1 || value <= 0)
            return null;

        return Math.Log((double)value.Value, (double)@base.Value);
    }

    /// <summary>
    ///     Calculates sine of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Sine of a value.</returns>
    public static decimal? Sin(decimal? value)
    {
        if (!value.HasValue)
            return null;

        return (decimal)Math.Sin((double)value);
    }

    /// <summary>
    ///     Calculates sine of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Sine of a value.</returns>
    public static double? Sin(double? value)
    {
        if (!value.HasValue)
            return null;

        return Math.Sin(value.Value);
    }

    /// <summary>
    ///     Calculates sine of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Sine of a value.</returns>
    public static float? Sin(float? value)
    {
        if (!value.HasValue)
            return null;

        return (float)Math.Sin(value.Value);
    }

    /// <summary>
    ///     Calculates cosine of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Cosine of a value.</returns>
    public static decimal? Cos(decimal? value)
    {
        if (!value.HasValue)
            return null;

        return (decimal)Math.Cos((double)value);
    }

    /// <summary>
    ///     Calculates cosine of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Cosine of a value.</returns>
    public static double? Cos(double? value)
    {
        if (!value.HasValue)
            return null;

        return Math.Cos(value.Value);
    }

    /// <summary>
    ///     Calculates cosine of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Cosine of a value.</returns>
    public static float? Cos(float? value)
    {
        if (!value.HasValue)
            return null;

        return (float)Math.Cos(value.Value);
    }

    /// <summary>
    ///     Calculates tangent of a value.
    /// </summary>
    /// <param name="value">The value in radians.</param>
    /// <returns>Tangent of a value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static decimal? Tan(decimal? value)
    {
        if (!value.HasValue)
            return null;

        return (decimal)Math.Tan((double)value);
    }

    /// <summary>
    ///     Calculates tangent of a value.
    /// </summary>
    /// <param name="value">The value in radians.</param>
    /// <returns>Tangent of a value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static double? Tan(double? value)
    {
        if (!value.HasValue)
            return null;

        return Math.Tan(value.Value);
    }

    /// <summary>
    ///     Calculates e raised to the specified power.
    /// </summary>
    /// <param name="value">The exponent.</param>
    /// <returns>e raised to the power of value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static decimal? Exp(decimal? value)
    {
        if (!value.HasValue)
            return null;

        return (decimal)Math.Exp((double)value);
    }

    /// <summary>
    ///     Calculates e raised to the specified power.
    /// </summary>
    /// <param name="value">The exponent.</param>
    /// <returns>e raised to the power of value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static double? Exp(double? value)
    {
        if (!value.HasValue)
            return null;

        return Math.Exp(value.Value);
    }

    /// <summary>
    ///     Calculates the natural logarithm (base e) of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Natural logarithm of a value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static decimal? Ln(decimal? value)
    {
        if (!value.HasValue)
            return null;

        return (decimal)Math.Log((double)value);
    }

    /// <summary>
    ///     Calculates the natural logarithm (base e) of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Natural logarithm of a value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static double? Ln(double? value)
    {
        if (!value.HasValue)
            return null;

        return Math.Log(value.Value);
    }

    /// <summary>
    ///     Clamps a value to be within the specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static int? Clamp(int? value, int? min, int? max)
    {
        if (!value.HasValue || !min.HasValue || !max.HasValue)
            return null;

        return Math.Clamp(value.Value, min.Value, max.Value);
    }

    /// <summary>
    ///     Clamps a value to be within the specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static long? Clamp(long? value, long? min, long? max)
    {
        if (!value.HasValue || !min.HasValue || !max.HasValue)
            return null;

        return Math.Clamp(value.Value, min.Value, max.Value);
    }

    /// <summary>
    ///     Clamps a value to be within the specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static decimal? Clamp(decimal? value, decimal? min, decimal? max)
    {
        if (!value.HasValue || !min.HasValue || !max.HasValue)
            return null;

        return Math.Clamp(value.Value, min.Value, max.Value);
    }

    /// <summary>
    ///     Clamps a value to be within the specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static double? Clamp(double? value, double? min, double? max)
    {
        if (!value.HasValue || !min.HasValue || !max.HasValue)
            return null;

        return Math.Clamp(value.Value, min.Value, max.Value);
    }

    /// <summary>
    ///     Calculates the logarithm of a value with the specified base.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="baseValue">The base of the logarithm.</param>
    /// <returns>Logarithm of the value with the specified base.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static double? LogBase(double? value, double? baseValue)
    {
        if (!value.HasValue || !baseValue.HasValue)
            return null;

        return Math.Log(value.Value, baseValue.Value);
    }

    /// <summary>
    ///     Calculates the base-10 logarithm of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Base-10 logarithm of a value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static double? Log10(double? value)
    {
        if (!value.HasValue)
            return null;

        return Math.Log10(value.Value);
    }

    /// <summary>
    ///     Calculates the base-2 logarithm of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Base-2 logarithm of a value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static double? Log2(double? value)
    {
        if (!value.HasValue)
            return null;

        return Math.Log2(value.Value);
    }

    /// <summary>
    ///     Checks if an integer value is between the specified minimum and maximum (inclusive).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (inclusive).</param>
    /// <returns>True if value is between min and max; otherwise false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static bool? IsBetween(int? value, int? min, int? max)
    {
        if (!value.HasValue || !min.HasValue || !max.HasValue)
            return null;

        return value.Value >= min.Value && value.Value <= max.Value;
    }

    /// <summary>
    ///     Checks if a long value is between the specified minimum and maximum (inclusive).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (inclusive).</param>
    /// <returns>True if value is between min and max; otherwise false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static bool? IsBetween(long? value, long? min, long? max)
    {
        if (!value.HasValue || !min.HasValue || !max.HasValue)
            return null;

        return value.Value >= min.Value && value.Value <= max.Value;
    }

    /// <summary>
    ///     Checks if a decimal value is between the specified minimum and maximum (inclusive).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (inclusive).</param>
    /// <returns>True if value is between min and max; otherwise false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static bool? IsBetween(decimal? value, decimal? min, decimal? max)
    {
        if (!value.HasValue || !min.HasValue || !max.HasValue)
            return null;

        return value.Value >= min.Value && value.Value <= max.Value;
    }

    /// <summary>
    ///     Checks if a double value is between the specified minimum and maximum (inclusive).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (inclusive).</param>
    /// <returns>True if value is between min and max; otherwise false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static bool? IsBetween(double? value, double? min, double? max)
    {
        if (!value.HasValue || !min.HasValue || !max.HasValue)
            return null;

        return value.Value >= min.Value && value.Value <= max.Value;
    }

    /// <summary>
    ///     Checks if an integer value is between the specified minimum and maximum (exclusive).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum value (exclusive).</param>
    /// <param name="max">The maximum value (exclusive).</param>
    /// <returns>True if value is strictly between min and max; otherwise false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static bool? IsBetweenExclusive(int? value, int? min, int? max)
    {
        if (!value.HasValue || !min.HasValue || !max.HasValue)
            return null;

        return value.Value > min.Value && value.Value < max.Value;
    }

    /// <summary>
    ///     Checks if a decimal value is between the specified minimum and maximum (exclusive).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum value (exclusive).</param>
    /// <param name="max">The maximum value (exclusive).</param>
    /// <returns>True if value is strictly between min and max; otherwise false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static bool? IsBetweenExclusive(decimal? value, decimal? min, decimal? max)
    {
        if (!value.HasValue || !min.HasValue || !max.HasValue)
            return null;

        return value.Value > min.Value && value.Value < max.Value;
    }
}