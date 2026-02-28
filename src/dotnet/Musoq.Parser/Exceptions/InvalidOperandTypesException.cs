using System;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Exceptions;

/// <summary>
///     Exception thrown when an operator is used with unsupported operand types.
/// </summary>
public class InvalidOperandTypesException : Exception, IDiagnosticException
{
    public InvalidOperandTypesException(Type leftType, Type rightType)
        : base($"Invalid operand types for operator: '{leftType.Name}' and '{rightType.Name}'.")
    {
        LeftType = leftType ?? throw new ArgumentNullException(nameof(leftType));
        RightType = rightType ?? throw new ArgumentNullException(nameof(rightType));
        Code = DiagnosticCode.MQ3007_InvalidOperandTypes;
    }

    public Type LeftType { get; }

    public Type RightType { get; }

    public DiagnosticCode Code { get; }

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty);
    }
}
