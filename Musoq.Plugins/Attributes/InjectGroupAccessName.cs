using System;

namespace Musoq.Plugins.Attributes;

/// <summary>
/// Represents a method argument that is a group access name.
/// </summary>
public sealed class InjectGroupAccessName : InjectTypeAttribute
{
    /// <summary>
    /// Returns the type of the argument that will be injected.
    /// </summary>
    public override Type InjectType => typeof(string);
}