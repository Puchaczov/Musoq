#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when an object does not implement an indexer but indexer access was attempted.
/// </summary>
public class ObjectDoesNotImplementIndexerException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance of ObjectDoesNotImplementIndexerException.
    /// </summary>
    public ObjectDoesNotImplementIndexerException(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ3018_NoIndexer;
    }

    /// <summary>
    ///     Initializes a new instance with diagnostic information.
    /// </summary>
    public ObjectDoesNotImplementIndexerException(string message, TextSpan span)
        : base(message)
    {
        Code = DiagnosticCode.MQ3018_NoIndexer;
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
