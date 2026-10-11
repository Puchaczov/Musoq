namespace Musoq.Plugins.Attributes;

/// <summary>Declares scheduling defaults for methods authored on this library type.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LibraryExecutionDefaultsAttribute : Attribute
{
    /// <summary>Gets or sets defaults for scalar declarations on this exact library type.</summary>
    public FunctionExecutionPermissions ScalarPermissions { get; set; }
    /// <summary>Gets or sets defaults for aggregate declarations on this exact library type.</summary>
    public FunctionExecutionPermissions AggregatePermissions { get; set; }
    /// <summary>Gets or sets defaults for window declarations on this exact library type.</summary>
    public FunctionExecutionPermissions WindowPermissions { get; set; }
}
