namespace Musoq.Plugins.Attributes;

/// <summary>
///     Represents a method that can be bound to the query.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class BindableMethodAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BindableMethodAttribute" /> class.
    ///     Creates a public bindable method (IsInternal = false).
    /// </summary>
    public BindableMethodAttribute()
    {
        Visibility = FunctionVisibility.Public;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BindableMethodAttribute" /> class.
    ///     This constructor is internal to allow Musoq.Plugins to mark methods as internal.
    /// </summary>
    /// <param name="isInternal">Indicates whether this method is internal and should not be shown in DESC FUNCTIONS output.</param>
    internal BindableMethodAttribute(bool isInternal)
    {
        Visibility = isInternal ? FunctionVisibility.CompilerOnly : FunctionVisibility.Public;
    }

    /// <summary>
    ///     Gets a value indicating whether this method is internal and should not be shown in DESC FUNCTIONS output.
    ///     Internal methods are used by the query engine for runtime type conversions and operations.
    /// </summary>
    internal bool IsInternal => Visibility == FunctionVisibility.CompilerOnly;

    /// <summary>Gets or sets the SQL visibility of this declaration.</summary>
    public FunctionVisibility Visibility { get; set; }

    /// <summary>Gets or sets explicitly permitted execution strategies.</summary>
    public FunctionExecutionPermissions ExecutionPermissions
    {
        get => _executionPermissions;
        set
        {
            _executionPermissions = value;
            HasExplicitExecutionPermissions = true;
        }
    }

    private FunctionExecutionPermissions _executionPermissions;
    internal bool HasExplicitExecutionPermissions { get; private set; }
}
