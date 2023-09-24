using System;

namespace Musoq.Plugins.Attributes;

/// <summary>
/// Attribute that allows to specify the name of the property and its default type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DynamicObjectPropertyDefaultTypeHintAttribute : Attribute
{
    /// <summary>
    /// Creates the attribute.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="type">The type.</param>
    public DynamicObjectPropertyDefaultTypeHintAttribute(string name, Type type)
    {
        Name = name;
        Type = type;
    }

    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    public string Name { get; }
    
    
    /// <summary>
    /// Gets the type of the property.
    /// </summary>
    public Type Type { get; }
}