using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Gets the random integer value
    /// </summary>
    /// <returns>Random integer</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Math)]
    [NonDeterministic]
    public int Rand() => RandomNumberGenerator.GetInt32(int.MaxValue);

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

        return min.Value == max.Value ? min.Value : RandomNumberGenerator.GetInt32(min.Value, max.Value);
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
}
