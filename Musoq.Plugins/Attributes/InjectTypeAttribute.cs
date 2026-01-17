using System;

namespace Musoq.Plugins.Attributes;

/// <summary>
///     Represents a method argument that should be injected.
/// </summary>
public abstract class InjectTypeAttribute : Attribute
{
    /// <inheritdoc />
    /// <summary>
    ///     Initialize object.
    /// </summary>
    internal InjectTypeAttribute()
    {
    }

    /// <summary>
    ///     Gets the type have to be injected when dynamic invocation performed.
    /// </summary>
    public abstract Type? InjectType { get; }
}