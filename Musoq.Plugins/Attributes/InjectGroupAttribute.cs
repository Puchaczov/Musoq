using System;

namespace Musoq.Plugins.Attributes;

/// <summary>
///     Represents a method argument where the group value is injected.
/// </summary>
public sealed class InjectGroupAttribute : InjectTypeAttribute
{
    /// <summary>
    ///     Returns the type of the argument that will be injected.
    /// </summary>
    public override Type InjectType => typeof(Group);
}
