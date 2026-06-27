using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
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
    public static decimal? Sin(decimal? value) => ApplyNullable(value, v => (decimal)Math.Sin((double)v));

    /// <summary>
    ///     Calculates sine of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Sine of a value.</returns>
    public static double? Sin(double? value) => ApplyNullable(value, Math.Sin);

    /// <summary>
    ///     Calculates sine of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Sine of a value.</returns>
    public static float? Sin(float? value) => ApplyNullable(value, v => (float)Math.Sin(v));

    /// <summary>
    ///     Calculates cosine of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Cosine of a value.</returns>
    public static decimal? Cos(decimal? value) => ApplyNullable(value, v => (decimal)Math.Cos((double)v));

    /// <summary>
    ///     Calculates cosine of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Cosine of a value.</returns>
    public static double? Cos(double? value) => ApplyNullable(value, Math.Cos);

    /// <summary>
    ///     Calculates cosine of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Cosine of a value.</returns>
    public static float? Cos(float? value) => ApplyNullable(value, v => (float)Math.Cos(v));

    /// <summary>
    ///     Calculates tangent of a value.
    /// </summary>
    /// <param name="value">The value in radians.</param>
    /// <returns>Tangent of a value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static decimal? Tan(decimal? value) => ApplyNullable(value, v => (decimal)Math.Tan((double)v));

    /// <summary>
    ///     Calculates tangent of a value.
    /// </summary>
    /// <param name="value">The value in radians.</param>
    /// <returns>Tangent of a value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static double? Tan(double? value) => ApplyNullable(value, Math.Tan);

    /// <summary>
    ///     Calculates e raised to the specified power.
    /// </summary>
    /// <param name="value">The exponent.</param>
    /// <returns>e raised to the power of value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static decimal? Exp(decimal? value) => ApplyNullable(value, v => (decimal)Math.Exp((double)v));

    /// <summary>
    ///     Calculates e raised to the specified power.
    /// </summary>
    /// <param name="value">The exponent.</param>
    /// <returns>e raised to the power of value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static double? Exp(double? value) => ApplyNullable(value, Math.Exp);

    /// <summary>
    ///     Calculates the natural logarithm (base e) of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Natural logarithm of a value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static decimal? Ln(decimal? value) => ApplyNullable(value, v => (decimal)Math.Log((double)v));

    /// <summary>
    ///     Calculates the natural logarithm (base e) of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Natural logarithm of a value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static double? Ln(double? value) => ApplyNullable(value, Math.Log);

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
    public static double? Log10(double? value) => ApplyNullable(value, Math.Log10);

    /// <summary>
    ///     Calculates the base-2 logarithm of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Base-2 logarithm of a value.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public static double? Log2(double? value) => ApplyNullable(value, Math.Log2);
}
