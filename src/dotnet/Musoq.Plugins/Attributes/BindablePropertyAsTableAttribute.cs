namespace Musoq.Plugins.Attributes;

/// <summary>
///     Represents a property that should be treated as a table.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class BindablePropertyAsTableAttribute : Attribute;
