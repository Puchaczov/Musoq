using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
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
}
