#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when set operators have mismatched column types.
/// </summary>
public class SetOperatorMustHaveSameTypesOfColumnsException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance with left and right field nodes.
    /// </summary>
    public SetOperatorMustHaveSameTypesOfColumnsException(FieldNode left, FieldNode right)
        : base(
            $"Set operator must have the same types of columns in both queries. Left column expression is {left} and right column expression is {right}")
    {
        Code = DiagnosticCode.MQ3020_SetOperatorColumnTypes;
    }

    /// <summary>
    ///     Initializes a new instance with message and span.
    /// </summary>
    public SetOperatorMustHaveSameTypesOfColumnsException(string message, TextSpan span)
        : base(message)
    {
        Code = DiagnosticCode.MQ3020_SetOperatorColumnTypes;
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
