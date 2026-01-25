using System;

namespace Musoq.Converter.Exceptions;

/// <summary>
///     Exception thrown when AST validation fails during the conversion process.
///     Provides detailed information about what went wrong during AST processing.
/// </summary>
public class AstValidationException : InvalidOperationException
{
    public AstValidationException(string nodeType, string validationContext, string message)
        : base(message)
    {
        NodeType = nodeType;
        ValidationContext = validationContext;
    }

    public AstValidationException(string nodeType, string validationContext, string message, Exception innerException)
        : base(message, innerException)
    {
        NodeType = nodeType;
        ValidationContext = validationContext;
    }

    public string NodeType { get; }
    public string ValidationContext { get; }

    public static AstValidationException ForNullNode(string expectedNodeType, string context)
    {
        return new AstValidationException(
            expectedNodeType,
            context,
            $"Expected a {expectedNodeType} node but found null in {context}. " +
            "This indicates a problem with the SQL query structure. Please check your query syntax."
        );
    }

    public static AstValidationException ForInvalidNodeStructure(string nodeType, string context, string issue)
    {
        return new AstValidationException(
            nodeType,
            context,
            $"The {nodeType} node has an invalid structure in {context}: {issue}. " +
            "Please check the corresponding part of your SQL query."
        );
    }

    public static AstValidationException ForUnsupportedNode(string nodeType, string context)
    {
        return new AstValidationException(
            nodeType,
            context,
            $"The {nodeType} node is not supported in {context}. " +
            "Please review the SQL query documentation for supported syntax."
        );
    }
}
