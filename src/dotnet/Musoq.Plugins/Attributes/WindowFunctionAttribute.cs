using System;

namespace Musoq.Plugins.Attributes;

/// <summary>
///     Marks a method as a window function factory.
///     The engine calls this method to obtain a fresh <see cref="IWindowFunction{TInput, TResult}"/>
///     instance per window function call site per query.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class WindowFunctionAttribute : BindableMethodAttribute
{
    /// <summary>
    ///     The SQL function name used with the OVER clause.
    ///     When null, the method name is used as the SQL name.
    ///     Use this to map a C# factory method name to a different SQL function name
    ///     (e.g., when the desired SQL name collides with an existing aggregate method).
    /// </summary>
    public string? Name { get; set; }
}
