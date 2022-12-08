using System;

namespace Musoq.Plugins.Attributes
{
    /// <summary>
    /// This attribute is used to mark method as a function that should not be resolved as it's aggregate set method.
    /// </summary>
    public sealed class AggregateSetDoNotResolveAttribute : Attribute
    {
    }
}