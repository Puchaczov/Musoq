using System;

namespace Musoq.Plugins.Attributes;

/// <summary>
///     Specifies the category of a bindable method for documentation and discoverability.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class MethodCategoryAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MethodCategoryAttribute" /> class.
    /// </summary>
    /// <param name="category">The category of the method.</param>
    public MethodCategoryAttribute(string category)
    {
        Category = category;
    }

    /// <summary>
    ///     Gets the category of the method.
    /// </summary>
    public string Category { get; }
}
