#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a type cannot be found during query evaluation.
/// </summary>
public class TypeNotFoundException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance of TypeNotFoundException.
    /// </summary>
    public TypeNotFoundException(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ3005_TypeMismatch;
    }

    /// <summary>
    ///     Initializes a new instance of TypeNotFoundException with type information.
    /// </summary>
    public TypeNotFoundException(string typeName, string context, TextSpan span)
        : base($"Type '{typeName}' not found{(string.IsNullOrEmpty(context) ? "" : $": {context}")}")
    {
        TypeName = typeName;
        Code = DiagnosticCode.MQ3005_TypeMismatch;
        Span = span;
    }

    /// <summary>
    ///     Gets the name of the type that was not found.
    /// </summary>
    public string? TypeName { get; }

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
