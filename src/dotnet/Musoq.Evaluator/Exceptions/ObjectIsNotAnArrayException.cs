#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when an object is not an array but array access was attempted.
/// </summary>
public class ObjectIsNotAnArrayException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance of ObjectIsNotAnArrayException.
    /// </summary>
    public ObjectIsNotAnArrayException(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ3017_ObjectNotArray;
    }

    /// <summary>
    ///     Initializes a new instance with diagnostic information.
    /// </summary>
    public ObjectIsNotAnArrayException(string message, TextSpan span)
        : base(message)
    {
        Code = DiagnosticCode.MQ3017_ObjectNotArray;
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
