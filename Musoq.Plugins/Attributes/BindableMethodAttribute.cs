using System;

namespace Musoq.Plugins.Attributes;

/// <summary>
///     Represents a method that can be bound to the query.
/// </summary>
public class BindableMethodAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BindableMethodAttribute" /> class.
    ///     Creates a public bindable method (IsInternal = false).
    /// </summary>
    public BindableMethodAttribute()
    {
        IsInternal = false;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BindableMethodAttribute" /> class.
    ///     This constructor is internal to allow Musoq.Plugins to mark methods as internal.
    /// </summary>
    /// <param name="isInternal">Indicates whether this method is internal and should not be shown in DESC FUNCTIONS output.</param>
    internal BindableMethodAttribute(bool isInternal)
    {
        IsInternal = isInternal;
    }

    /// <summary>
    ///     Gets a value indicating whether this method is internal and should not be shown in DESC FUNCTIONS output.
    ///     Internal methods are used by the query engine for runtime type conversions and operations.
    /// </summary>
    internal bool IsInternal { get; }
}