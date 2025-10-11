using System;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
/// Exception thrown when code generation fails during query compilation.
/// Provides specific guidance for C# code generation issues.
/// </summary>
public class CodeGenerationException : Exception
{
    /// <summary>
    /// The component that was generating code when the error occurred.
    /// </summary>
    public string Component { get; }

    /// <summary>
    /// The operation that was being performed during code generation.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Initializes a new instance of the CodeGenerationException class.
    /// </summary>
    /// <param name="component">The component generating code.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="message">The error message.</param>
    public CodeGenerationException(string component, string operation, string message)
        : base($"Code generation failed in '{component}' during '{operation}': {message}")
    {
        Component = component ?? "Unknown";
        Operation = operation ?? "Unknown";
    }

    /// <summary>
    /// Initializes a new instance of the CodeGenerationException class with an inner exception.
    /// </summary>
    /// <param name="component">The component generating code.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public CodeGenerationException(string component, string operation, string message, Exception innerException)
        : base($"Code generation failed in '{component}' during '{operation}': {message}", innerException)
    {
        Component = component ?? "Unknown";
        Operation = operation ?? "Unknown";
    }

    /// <summary>
    /// Creates a CodeGenerationException for missing context errors.
    /// </summary>
    /// <param name="component">The component generating code.</param>
    /// <param name="contextType">The type of context that is missing.</param>
    /// <returns>A configured CodeGenerationException instance.</returns>
    public static CodeGenerationException CreateForMissingContext(string component, string contextType)
    {
        return new CodeGenerationException(
            component,
            "Context Validation",
            $"Required {contextType} is missing. " +
            "This indicates an internal compilation error. " +
            "Please verify the query structure is valid."
        );
    }

    /// <summary>
    /// Creates a CodeGenerationException for unsupported operations.
    /// </summary>
    /// <param name="component">The component generating code.</param>
    /// <param name="operation">The unsupported operation.</param>
    /// <param name="suggestion">Optional suggestion for resolution.</param>
    /// <returns>A configured CodeGenerationException instance.</returns>
    public static CodeGenerationException CreateForUnsupportedOperation(string component, string operation, string suggestion = null)
    {
        var message = $"Operation '{operation}' is not supported in this context.";
        if (!string.IsNullOrEmpty(suggestion))
        {
            message += $" {suggestion}";
        }

        return new CodeGenerationException(component, "Operation Validation", message);
    }
}
