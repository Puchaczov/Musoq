namespace Musoq.Plugins;

/// <summary>Controls whether a declaration belongs to the public SQL language.</summary>
public enum FunctionVisibility
{
    /// <summary>The function is callable and discoverable from user SQL.</summary>
    Public,
    /// <summary>Only compiler-generated operations may bind the function.</summary>
    CompilerOnly
}
