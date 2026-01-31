#nullable enable
using System;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a method cannot be resolved.
/// </summary>
public class CannotResolveMethodException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance with a message.
    /// </summary>
    public CannotResolveMethodException(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ3029_UnresolvableMethod;
    }

    /// <summary>
    ///     Initializes a new instance with a message and span.
    /// </summary>
    public CannotResolveMethodException(string message, TextSpan span)
        : base(message)
    {
        Code = DiagnosticCode.MQ3029_UnresolvableMethod;
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

    /// <summary>
    ///     Creates an exception for null arguments.
    /// </summary>
    public static CannotResolveMethodException CreateForNullArguments(string methodName)
    {
        return new CannotResolveMethodException($"Method {methodName} cannot be resolved because of null arguments");
    }

    /// <summary>
    ///     Creates an exception for unmatched method name or arguments.
    /// </summary>
    public static CannotResolveMethodException CreateForCannotMatchMethodNameOrArguments(string methodName, Node[] args)
    {
        var types = args.Length > 0
            ? string.Join(", ", args.Select(f => f.ReturnType?.ToString() ?? "null"))
            : string.Empty;

        return new CannotResolveMethodException(
            $"Method {methodName} with argument types {types} cannot be resolved");
    }
}
