using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Exceptions;

/// <summary>
///     Exception thrown when an operator is used with unsupported operand types.
/// </summary>
public class InvalidOperandTypesException : Exception, IDiagnosticException
{
    public InvalidOperandTypesException(Type leftType, Type rightType)
        : base(CreateMessage(leftType, rightType))
    {
        LeftType = leftType;
        RightType = rightType;
    }

    public InvalidOperandTypesException(string message, Exception innerException)
        : base(message, innerException)
    {
        LeftType = typeof(object);
        RightType = typeof(object);
    }

    public InvalidOperandTypesException(string message)
        : base(message)
    {
        LeftType = typeof(object);
        RightType = typeof(object);
    }

    public InvalidOperandTypesException()
    {
        LeftType = typeof(object);
        RightType = typeof(object);
    }

    public Type LeftType { get; }

    public Type RightType { get; }

    public DiagnosticCode Code { get; } = DiagnosticCode.MQ3007_InvalidOperandTypes;

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty);
    }

    private static string CreateMessage(Type leftType, Type rightType)
    {
        ArgumentNullException.ThrowIfNull(leftType);
        ArgumentNullException.ThrowIfNull(rightType);
        return $"Invalid operand types for operator: '{leftType.Name}' and '{rightType.Name}'.";
    }
}
