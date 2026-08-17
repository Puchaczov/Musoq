using System.Linq;
using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a method cannot be resolved.
/// </summary>
public class CannotResolveMethodException : Exception, IDiagnosticException
{

    public CannotResolveMethodException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = DiagnosticCode.MQ3088_NoMatchingCallableOverload;
    }

    public CannotResolveMethodException()
    {
        Code = DiagnosticCode.MQ3088_NoMatchingCallableOverload;
    }
    /// <summary>
    ///     Initializes a new instance with a message.
    /// </summary>
    public CannotResolveMethodException(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ3088_NoMatchingCallableOverload;
    }

    /// <summary>
    ///     Initializes a new instance with a message and span.
    /// </summary>
    public CannotResolveMethodException(string message, TextSpan span)
        : base(message)
    {
        Code = DiagnosticCode.MQ3088_NoMatchingCallableOverload;
        Span = span;
    }

    /// <summary>
    ///     Initializes a new instance with a message, diagnostic code, and span.
    /// </summary>
    public CannotResolveMethodException(string message, DiagnosticCode code, TextSpan span)
        : base(message)
    {
        Code = code;
        Span = span;
        Arguments = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>
    ///     Initializes an exception with a structured callable-resolution payload.
    /// </summary>
    internal CannotResolveMethodException(
        string message,
        DiagnosticCode code,
        TextSpan span,
        IReadOnlyDictionary<string, string>? arguments)
        : base(message)
    {
        Code = code;
        Span = span;
        Arguments = arguments is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(arguments, StringComparer.Ordinal);
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
    ///     Gets stable facts describing the failed callable resolution.
    /// </summary>
    public IReadOnlyDictionary<string, string> Arguments { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    ///     Converts this exception to a Diagnostic instance.
    /// </summary>
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        var diagnostic = Diagnostic.Error(Code, Message, span);
        foreach (var (name, value) in Arguments)
            diagnostic = diagnostic.WithArgument(name, value);

        return diagnostic;
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
        ArgumentNullException.ThrowIfNull(args);
        var types = args.Length > 0
            ? string.Join(", ", args.Select(f => f.ReturnType?.ToString() ?? "null"))
            : string.Empty;

        return new CannotResolveMethodException(
            $"Method {methodName} with argument types {types} cannot be resolved");
    }
}
