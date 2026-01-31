#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a GROUP BY index is out of range.
/// </summary>
public class FieldLinkIndexOutOfRangeException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance with index and group count.
    /// </summary>
    public FieldLinkIndexOutOfRangeException(int index, int groups)
        : base($"There is no group selected by '{index}' value. Max allowed group index for this query is {groups}")
    {
        Index = index;
        MaxGroups = groups;
        Code = DiagnosticCode.MQ3024_GroupByIndexOutOfRange;
    }

    /// <summary>
    ///     Initializes a new instance with index, group count, and span.
    /// </summary>
    public FieldLinkIndexOutOfRangeException(int index, int groups, TextSpan span)
        : base($"There is no group selected by '{index}' value. Max allowed group index for this query is {groups}")
    {
        Index = index;
        MaxGroups = groups;
        Code = DiagnosticCode.MQ3024_GroupByIndexOutOfRange;
        Span = span;
    }

    /// <summary>
    ///     Gets the invalid index.
    /// </summary>
    public int Index { get; }

    /// <summary>
    ///     Gets the maximum allowed group index.
    /// </summary>
    public int MaxGroups { get; }

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
