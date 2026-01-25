using System;

namespace Musoq.Plugins.Attributes;

/// <summary>
///     Attribute that allows to specify the name of the property and its type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class DynamicObjectPropertyTypeHintAttribute : Attribute
{
    /// <summary>
    ///     Creates the attribute.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="type">The type.</param>
    public DynamicObjectPropertyTypeHintAttribute(string name, Type type)
    {
        Name = name;
        Type = type;
    }

    /// <summary>
    ///     Gets the name of the property.
    /// </summary>
    public string Name { get; }


    /// <summary>
    ///     Gets the type of the property.
    /// </summary>
    public Type Type { get; }
}
