using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a set operator (UNION, EXCEPT, INTERSECT) is missing required key columns.
/// </summary>
public class SetOperatorMustHaveKeyColumnsException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance with the set operator name.
    /// </summary>
    public SetOperatorMustHaveKeyColumnsException(string setOperator)
        : base(
            $"{setOperator} operator must have keys. Set operators require key columns to determine how to combine rows.")
    {
        SetOperator = setOperator;
    }

    /// <summary>
    ///     Initializes a new instance with the set operator name and span.
    /// </summary>
    public SetOperatorMustHaveKeyColumnsException(string setOperator, TextSpan span)
        : base(
            $"{setOperator} operator must have keys. Set operators require key columns to determine how to combine rows.")
    {
        SetOperator = setOperator;
        Span = span;
    }

    /// <summary>
    ///     Gets the name of the set operator.
    /// </summary>
    public string SetOperator { get; }

    /// <summary>
    ///     Gets the diagnostic code for this exception.
    /// </summary>
    public DiagnosticCode Code => DiagnosticCode.MQ3031_SetOperatorMissingKeys;

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
