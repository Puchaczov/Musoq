using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
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
