using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
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
    public double? Sqrt(decimal? x) => ApplyNullable(x, v => Math.Sqrt(Convert.ToDouble(v)));

    /// <summary>
    ///     Computes the sqrt of a given value
    /// </summary>
    /// <param name="x">The x</param>
    /// <returns>Sqrt of a value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public double? Sqrt(double? x) => ApplyNullable(x, Math.Sqrt);

    /// <summary>
    ///     Computes the sqrt of a given value
    /// </summary>
    /// <param name="x">The x</param>
    /// <returns>Sqrt of a value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    public double? Sqrt(long? x) => ApplyNullable(x, v => Math.Sqrt(v));
}
