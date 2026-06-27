namespace Musoq.Plugins.Attributes;

/// <summary>
///     Marks an aggregate declaration argument as a parent-prefix depth selector.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class AggregateParentAttribute : Attribute;
