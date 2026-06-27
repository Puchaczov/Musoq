using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a GROUP BY ordinal is out of range.
/// </summary>
public class GroupByIndexOutOfRangeException : Exception, IDiagnosticException
{
    public GroupByIndexOutOfRangeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public GroupByIndexOutOfRangeException(string message)
        : base(message)
    {
    }

    public GroupByIndexOutOfRangeException()
    {
    }

    public GroupByIndexOutOfRangeException(int ordinal, int selectFields)
        : base($"GROUP BY position {ordinal} is out of range. SELECT projection contains {selectFields} field(s).")
    {
        Ordinal = ordinal;
        SelectFields = selectFields;
        Code = DiagnosticCode.MQ3024_GroupByIndexOutOfRange;
    }

    public GroupByIndexOutOfRangeException(int ordinal, int selectFields, TextSpan span)
        : base($"GROUP BY position {ordinal} is out of range. SELECT projection contains {selectFields} field(s).")
    {
        Ordinal = ordinal;
        SelectFields = selectFields;
        Code = DiagnosticCode.MQ3024_GroupByIndexOutOfRange;
        Span = span;
    }

    public int Ordinal { get; }

    public int SelectFields { get; }

    public DiagnosticCode Code { get; }

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        return Diagnostic.Error(Code, Message, span);
    }
}
