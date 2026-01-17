namespace Musoq.Plugins.Lib.RuntimeOperators;

/// <summary>
///     Interface for runtime operator implementations that handle dynamic type operations on object columns.
///     Provides arithmetic, comparison, and equality operations with automatic type conversion.
/// </summary>
internal interface IRuntimeOperators
{
    /// <summary>
    ///     Applies addition operator (left + right) with type-preserving conversion.
    /// </summary>
    object? Add(object? left, object? right);

    /// <summary>
    ///     Applies subtraction operator (left - right) with type-preserving conversion.
    /// </summary>
    object? Subtract(object? left, object? right);

    /// <summary>
    ///     Applies multiplication operator (left * right) with type-preserving conversion.
    /// </summary>
    object? Multiply(object? left, object? right);

    /// <summary>
    ///     Applies division operator (left / right) with type-preserving conversion.
    /// </summary>
    object? Divide(object? left, object? right);

    /// <summary>
    ///     Applies modulo operator (left % right) with type-preserving conversion.
    /// </summary>
    object? Modulo(object? left, object? right);

    /// <summary>
    ///     Applies greater than comparison (left > right) with automatic type conversion.
    /// </summary>
    bool? GreaterThan(object? left, object? right);

    /// <summary>
    ///     Applies less than comparison (left &lt; right) with automatic type conversion.
    /// </summary>
    bool? LessThan(object? left, object? right);

    /// <summary>
    ///     Applies greater than or equal comparison (left >= right) with automatic type conversion.
    /// </summary>
    bool? GreaterThanOrEqual(object? left, object? right);

    /// <summary>
    ///     Applies less than or equal comparison (left &lt;= right) with automatic type conversion.
    /// </summary>
    bool? LessThanOrEqual(object? left, object? right);

    /// <summary>
    ///     Applies equality comparison (left == right) with automatic type conversion.
    /// </summary>
    bool? Equal(object? left, object? right);

    /// <summary>
    ///     Applies inequality comparison (left != right) with automatic type conversion.
    /// </summary>
    bool? NotEqual(object? left, object? right);
}