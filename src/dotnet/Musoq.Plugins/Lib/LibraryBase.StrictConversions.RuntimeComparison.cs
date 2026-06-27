using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Runtime greater than operator that handles object &gt; object with automatic type conversion.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalGreaterThanOperator(object? left, object? right)
    {
        return RuntimeOperators.GreaterThan(left, right);
    }

    /// <summary>
    ///     Runtime less than operator that handles object &lt; object with automatic type conversion.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalLessThanOperator(object? left, object? right)
    {
        return RuntimeOperators.LessThan(left, right);
    }

    /// <summary>
    ///     Runtime greater than or equal operator that handles object &gt;= object with automatic type conversion.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalGreaterThanOrEqualOperator(object? left, object? right)
    {
        return RuntimeOperators.GreaterThanOrEqual(left, right);
    }

    /// <summary>
    ///     Runtime less than or equal operator that handles object &lt;= object with automatic type conversion.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalLessThanOrEqualOperator(object? left, object? right)
    {
        return RuntimeOperators.LessThanOrEqual(left, right);
    }

    /// <summary>
    ///     Runtime equality operator that handles object == object with automatic type conversion.
    ///     For strings, tries numeric conversion first. If both convert successfully, compares numerically.
    ///     Otherwise compares as strings.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalEqualOperator(object? left, object? right)
    {
        return RuntimeOperators.Equal(left, right);
    }

    /// <summary>
    ///     Runtime inequality operator that handles object != object with automatic type conversion.
    ///     For strings, tries numeric conversion first. If both convert successfully, compares numerically.
    ///     Otherwise compares as strings.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalNotEqualOperator(object? left, object? right)
    {
        return RuntimeOperators.NotEqual(left, right);
    }

    /// <summary>
    ///     Runtime null-safe distinct predicate for object operands.
    /// </summary>
    [BindableMethod(true)]
    public bool InternalIsDistinctFromOperator(object? left, object? right)
    {
        if (left is null)
            return right is not null;

        if (right is null)
            return true;

        return RuntimeOperators.NotEqual(left, right) == true;
    }

    /// <summary>
    ///     Runtime null-safe not-distinct predicate for object operands.
    /// </summary>
    [BindableMethod(true)]
    public bool InternalIsNotDistinctFromOperator(object? left, object? right)
    {
        if (left is null)
            return right is null;

        if (right is null)
            return false;

        return RuntimeOperators.Equal(left, right) == true;
    }
}
