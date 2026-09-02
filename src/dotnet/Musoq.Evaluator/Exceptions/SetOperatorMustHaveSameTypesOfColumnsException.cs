using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when set operators have mismatched column types.
/// </summary>
public class SetOperatorMustHaveSameTypesOfColumnsException : Exception, IDiagnosticException
{

    public SetOperatorMustHaveSameTypesOfColumnsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SetOperatorMustHaveSameTypesOfColumnsException(string message)
        : base(message)
    {
    }

    public SetOperatorMustHaveSameTypesOfColumnsException()
    {
    }
    /// <summary>
    ///     Initializes a new instance with left and right field nodes.
    /// </summary>
    public SetOperatorMustHaveSameTypesOfColumnsException(FieldNode left, FieldNode right)
        : base(
            $"Set operator must have the same types of columns in both queries. Left column expression is {FormatColumnExpression(left)} and right column expression is {FormatColumnExpression(right)}")
    {
        Code = DiagnosticCode.MQ3020_SetOperatorColumnTypes;
    }

    private static string FormatColumnExpression(FieldNode field)
    {
        return field.Expression is AccessColumnNode
            ? field.FieldName
            : field.Expression.ToString();
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
