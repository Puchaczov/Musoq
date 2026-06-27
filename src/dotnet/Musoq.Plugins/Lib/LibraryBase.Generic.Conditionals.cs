using System.Collections.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Chose a or b value based on the expression result
    /// </summary>
    /// <param name="expressionResult">The expression result</param>
    /// <param name="a">The A parameter</param>
    /// <param name="b">The B parameter</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Value a or b</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T If<T>(bool expressionResult, T a, T b)
    {
        if (expressionResult)
            return a;

        return b;
    }

    /// <summary>
    ///     Returns null if the two values are equal, otherwise returns the first value.
    ///     Useful for replacing specific values with null in queries.
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <param name="compareValue">The value to compare against</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>Null if values are equal, otherwise the first value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T? NullIf<T>(T? value, T? compareValue)
    {
        if (value == null && compareValue == null)
            return default;

        if (value == null || compareValue == null)
            return value;

        return EqualityComparer<T>.Default.Equals(value, compareValue) ? default : value;
    }

    /// <summary>
    ///     Returns the first value if it is not null, otherwise returns the second value.
    ///     SQL-style IFNULL/ISNULL function.
    /// </summary>
    /// <param name="value">The value to check for null</param>
    /// <param name="defaultValue">The value to return if the first is null</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>The first value if not null, otherwise the second value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T? IfNull<T>(T? value, T? defaultValue)
    {
        return value ?? defaultValue;
    }

    /// <summary>
    ///     Returns the default value of a type if the value is null, otherwise returns the value.
    /// </summary>
    /// <param name="value">The value to check for null</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>The value if not null, otherwise default(T)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public T? DefaultIfNull<T>(T? value)
    {
        return value ?? default;
    }

    /// <summary>
    ///     Determines whether the value is null.
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>True if the value is null, otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public bool IsNull<T>(T? value)
    {
        return value == null;
    }

    /// <summary>
    ///     Determines whether the value is not null.
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>True if the value is not null, otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public bool IsNotNull<T>(T? value)
    {
        return value != null;
    }
}
