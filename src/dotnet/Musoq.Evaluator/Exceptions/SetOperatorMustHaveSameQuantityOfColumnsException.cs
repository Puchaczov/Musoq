#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when set operators have mismatched column counts.
/// </summary>
public class SetOperatorMustHaveSameQuantityOfColumnsException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance of SetOperatorMustHaveSameQuantityOfColumnsException.
    /// </summary>
    public SetOperatorMustHaveSameQuantityOfColumnsException()
        : base("Set operator must have the same quantity of columns in both queries")
    {
        Code = DiagnosticCode.MQ3019_SetOperatorColumnCount;
    }

    /// <summary>
    ///     Initializes a new instance with column counts.
    /// </summary>
    public SetOperatorMustHaveSameQuantityOfColumnsException(int leftCount, int rightCount, TextSpan span)
        : base(
            $"Set operator must have the same quantity of columns in both queries. Left has {leftCount} columns, right has {rightCount} columns.")
    {
        Code = DiagnosticCode.MQ3019_SetOperatorColumnCount;
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
