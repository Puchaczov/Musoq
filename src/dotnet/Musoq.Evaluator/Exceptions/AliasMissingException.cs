#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a required alias is missing.
/// </summary>
public class AliasMissingException : Exception, IDiagnosticException
{
    public static string CreateMethodCallMessage(string methodCall)
    {
        return $"Method call '{methodCall}' must be qualified with a source alias when more than one schema is used. Prefix it with the alias that owns the method implementation, for example 'a.{methodCall}' or 'b.{methodCall}'. For aggregates, the alias chooses the schema library implementation. If the expression is already aliased in SELECT, prefer that alias in ORDER BY instead of repeating the aggregate.";
    }

    /// <summary>
    ///     Initializes a new instance with node.
    /// </summary>
    public AliasMissingException(AccessMethodNode node)
        : base(CreateMethodCallMessage(node.ToString()))
    {
        Code = DiagnosticCode.MQ3022_MissingAlias;
    }

    /// <summary>
    ///     Initializes a new instance with message and span.
    /// </summary>
    public AliasMissingException(string message, TextSpan span)
        : base(message)
    {
        Code = DiagnosticCode.MQ3022_MissingAlias;
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
