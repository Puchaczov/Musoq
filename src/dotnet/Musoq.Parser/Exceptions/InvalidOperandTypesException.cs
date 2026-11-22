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

    public InvalidOperandTypesException(Type leftType, Type rightType, string operatorName, TextSpan span)
        : base($"Operator '{operatorName}' cannot be applied to '{leftType.Name}' and '{rightType.Name}'.")
    {
        LeftType = leftType;
        RightType = rightType;
        OperatorName = operatorName;
        Span = span;
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

    private string? OperatorName { get; }

    public DiagnosticCode Code { get; } = DiagnosticCode.MQ3007_InvalidOperandTypes;

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var diagnostic = Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty);
        if (OperatorName is null)
            return diagnostic;

        if (sourceText is not null && Span is { IsEmpty: false } span)
            diagnostic = diagnostic.WithSourceContext(sourceText, span);

        diagnostic = diagnostic
            .WithArgument("operator", OperatorName)
            .WithArgument("leftType", LeftType.Name)
            .WithArgument("rightType", RightType.Name);
        if (LeftType != typeof(decimal) && RightType != typeof(decimal))
            return diagnostic;

        var floatingPointType = LeftType == typeof(decimal) ? RightType : LeftType;
        if (floatingPointType != typeof(float) && floatingPointType != typeof(double))
            return diagnostic;

        var castType = floatingPointType == typeof(float) ? "float" : "double";
        return diagnostic.WithSuggestedFix(
            DiagnosticAction.Suggestion(
                $"Cast one operand explicitly to the other type, for example using ::{castType}."
            )
        );
    }

    private static string CreateMessage(Type leftType, Type rightType)
    {
        ArgumentNullException.ThrowIfNull(leftType);
        ArgumentNullException.ThrowIfNull(rightType);
        return $"Invalid operand types for operator: '{leftType.Name}' and '{rightType.Name}'.";
    }
}
