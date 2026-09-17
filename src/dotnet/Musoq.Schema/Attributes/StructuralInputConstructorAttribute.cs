namespace Musoq.Schema.Attributes;

/// <summary>
/// Marks the single constructor Core may use when a structural value is adapted
/// to a provider input type.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class StructuralInputConstructorAttribute : Attribute;