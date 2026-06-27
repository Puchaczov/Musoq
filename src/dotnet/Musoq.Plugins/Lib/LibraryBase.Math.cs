using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    private static TOut? ApplyNullable<TIn, TOut>(TIn? value, Func<TIn, TOut> operation)
        where TIn : struct
        where TOut : struct
    {
        if (!value.HasValue)
            return null;

        return operation(value.Value);
    }

    /// <summary>
    ///     Gets the absolute value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Absolute value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public decimal? Abs(decimal? value) => ApplyNullable(value, Math.Abs);

    /// <summary>
    ///     Gets the absolute value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Absolute value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public long? Abs(long? value) => ApplyNullable(value, Math.Abs);

    /// <summary>
    ///     Gets the absolute value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Absolute value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public int? Abs(int? value) => ApplyNullable(value, Math.Abs);

    /// <summary>
    ///     Gets the ceiling value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Ceiling value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public decimal? Ceil(decimal? value) => ApplyNullable(value, Math.Ceiling);

    /// <summary>
    ///     Gets the floor value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Floor value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public decimal? Floor(decimal? value) => ApplyNullable(value, Math.Floor);

    /// <summary>
    ///     Determine whether value is greater, equal or less that zero
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Is less, equal or greater value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public decimal? Sign(decimal? value)
    {
        return ComputeSign(value);
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
        return ComputeSign(value);
    }

    private static T? ComputeSign<T>(T? value) where T : struct, IComparable<T>
    {
        if (!value.HasValue)
            return null;

        var cmp = value.Value.CompareTo(default);

        if (cmp > 0)
            return (T)Convert.ChangeType(1, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        if (cmp == 0)
            return (T)Convert.ChangeType(0, typeof(T), System.Globalization.CultureInfo.InvariantCulture);

        return (T)Convert.ChangeType(-1, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
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
}
