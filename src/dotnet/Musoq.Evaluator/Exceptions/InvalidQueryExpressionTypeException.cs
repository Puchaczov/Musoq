#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a query expression has an invalid (non-primitive) type.
/// </summary>
public class InvalidQueryExpressionTypeException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance with expression description, type, and context.
    /// </summary>
    public InvalidQueryExpressionTypeException(string expressionDescription, Type? invalidType, string context)
        : base(
            $"Expression '{expressionDescription}' has invalid type '{invalidType?.FullName ?? "null"}' in {context}. " +
            "Only primitive types (numeric, string, bool, char, DateTime, DateTimeOffset, Guid, TimeSpan, decimal, null) are allowed in query expressions.")
    {
        Code = DiagnosticCode.MQ3027_InvalidExpressionType;
    }

    /// <summary>
    ///     Initializes a new instance with field node, type, and context.
    /// </summary>
    public InvalidQueryExpressionTypeException(FieldNode field, Type? invalidType, string context)
        : base(
            $"Query output column '{field.FieldName}' has invalid type '{invalidType?.FullName ?? "null"}' in {context}. " +
            "Only primitive types (numeric, string, bool, char, DateTime, DateTimeOffset, Guid, TimeSpan, decimal, null) are allowed in query outputs.")
    {
        Code = DiagnosticCode.MQ3027_InvalidExpressionType;
    }

    /// <summary>
    ///     Initializes a new instance with message and span.
    /// </summary>
    public InvalidQueryExpressionTypeException(string message, TextSpan span)
        : base(message)
    {
        Code = DiagnosticCode.MQ3027_InvalidExpressionType;
        Span = span;
    }

    /// <summary>
    ///     Gets the diagnostic code for this exception.
    /// </summary>
    public DiagnosticCode Code { get; }

    /// <summary>
    ///     Gets the source location span where this error occurred.
    /// </summary>
    public TextSpan? Span { get; }

    /// <summary>
    ///     Converts this exception to a Diagnostic instance.
    /// </summary>
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        return Diagnostic.Error(Code, Message, span);
    }
}
