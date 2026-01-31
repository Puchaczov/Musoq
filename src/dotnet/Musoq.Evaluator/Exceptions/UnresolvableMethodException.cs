#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a method cannot be resolved during query evaluation.
/// </summary>
public class UnresolvableMethodException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance of UnresolvableMethodException.
    /// </summary>
    public UnresolvableMethodException(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ3004_UnknownFunction;
    }

    /// <summary>
    ///     Initializes a new instance of UnresolvableMethodException with method information.
    /// </summary>
    public UnresolvableMethodException(string methodName, string[] argumentTypes, TextSpan span)
        : base($"Cannot resolve method '{methodName}' with arguments ({string.Join(", ", argumentTypes)})")
    {
        MethodName = methodName;
        ArgumentTypes = argumentTypes;
        Code = DiagnosticCode.MQ3004_UnknownFunction;
        Span = span;
    }

    /// <summary>
    ///     Gets the name of the unresolvable method.
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    ///     Gets the argument types that were used in the call.
    /// </summary>
    public string[]? ArgumentTypes { get; }

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
