using System;

namespace Musoq.Plugins.Attributes;

/// <summary>
///     Attribute used to mark method parameter as a source of data.
/// </summary>
public class InjectSpecificSourceAttribute : InjectTypeAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InjectSpecificSourceAttribute" /> class.
    /// </summary>
    /// <param name="type">Type of the source.</param>
    public InjectSpecificSourceAttribute(Type type)
    {
        InjectType = type;
    }

    /// <summary>
    ///     Initialize object.
    /// </summary>
    public override Type InjectType { get; }
}