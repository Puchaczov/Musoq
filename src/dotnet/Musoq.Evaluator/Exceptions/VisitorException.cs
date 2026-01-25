using System;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when visitor operations encounter validation or processing errors.
///     Provides specific guidance for AST processing and visitor pattern issues.
/// </summary>
public class VisitorException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the VisitorException class.
    /// </summary>
    /// <param name="visitorName">The name of the visitor that encountered the error.</param>
    /// <param name="operation">The operation that was being performed.</param>
    /// <param name="message">The error message.</param>
    public VisitorException(string visitorName, string operation, string message)
        : base($"Visitor '{visitorName}' failed during '{operation}': {message}")
    {
        VisitorName = visitorName ?? "Unknown";
        Operation = operation ?? "Unknown";
    }

    /// <summary>
    ///     Initializes a new instance of the VisitorException class with an inner exception.
    /// </summary>
    /// <param name="visitorName">The name of the visitor that encountered the error.</param>
    /// <param name="operation">The operation that was being performed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public VisitorException(string visitorName, string operation, string message, Exception innerException)
        : base($"Visitor '{visitorName}' failed during '{operation}': {message}", innerException)
    {
        VisitorName = visitorName ?? "Unknown";
        Operation = operation ?? "Unknown";
    }

    /// <summary>
    ///     The name of the visitor that encountered the error.
    /// </summary>
    public string VisitorName { get; }

    /// <summary>
    ///     The operation that was being performed when the error occurred.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    ///     Creates a VisitorException for stack underflow operations.
    /// </summary>
    /// <param name="visitorName">The name of the visitor.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="expectedItems">Number of items expected on the stack.</param>
    /// <param name="actualItems">Number of items actually on the stack.</param>
    /// <returns>A configured VisitorException instance.</returns>
    public static VisitorException CreateForStackUnderflow(string visitorName, string operation, int expectedItems,
        int actualItems)
    {
        return new VisitorException(
            visitorName,
            operation,
            $"Stack underflow detected. Expected at least {expectedItems} item(s) on the stack, but found {actualItems}. " +
            "This typically indicates an AST processing error or malformed query structure. " +
            "Please verify the query syntax and structure."
        );
    }

    /// <summary>
    ///     Creates a VisitorException for null node errors.
    /// </summary>
    /// <param name="visitorName">The name of the visitor.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="nodeType">The expected node type.</param>
    /// <returns>A configured VisitorException instance.</returns>
    public static VisitorException CreateForNullNode(string visitorName, string operation, string nodeType)
    {
        return new VisitorException(
            visitorName,
            operation,
            $"Expected '{nodeType}' node but received null. " +
            "This indicates an internal AST processing error. " +
            "Please verify the query structure and report this issue if it persists."
        );
    }

    /// <summary>
    ///     Creates a VisitorException for invalid node type errors.
    /// </summary>
    /// <param name="visitorName">The name of the visitor.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="expectedType">The expected node type.</param>
    /// <param name="actualType">The actual node type.</param>
    /// <returns>A configured VisitorException instance.</returns>
    public static VisitorException CreateForInvalidNodeType(string visitorName, string operation, string expectedType,
        string actualType)
    {
        return new VisitorException(
            visitorName,
            operation,
            $"Invalid node type. Expected '{expectedType}' but got '{actualType}'. " +
            "This indicates an AST structure mismatch. " +
            "Please verify the query syntax matches the expected pattern."
        );
    }

    /// <summary>
    ///     Creates a VisitorException for processing failures.
    /// </summary>
    /// <param name="visitorName">The name of the visitor.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="context">Additional context about the failure.</param>
    /// <param name="suggestion">Suggested resolution.</param>
    /// <returns>A configured VisitorException instance.</returns>
    public static VisitorException CreateForProcessingFailure(string visitorName, string operation, string context,
        string suggestion = null)
    {
        var message = $"Processing failed: {context}";
        if (!string.IsNullOrEmpty(suggestion)) message += $" {suggestion}";

        return new VisitorException(visitorName, operation, message);
    }
}
