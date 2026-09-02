using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

internal sealed class EnumDescriptorMismatchException : Exception, IDiagnosticException
{
    public EnumDescriptorMismatchException(string columnName, TextSpan span, string? detail = null)
        : base(CreateMessage(columnName, detail))
    {
        ColumnName = columnName;
        Span = span;
    }

    public string ColumnName { get; }

    public DiagnosticCode Code => DiagnosticCode.MQ3115_EnumDescriptorMismatch;

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty)
            .WithArgument("column", ColumnName);
    }

    private static string CreateMessage(string columnName, string? detail)
    {
        var message = $"Enum descriptor for column '{columnName}' does not match the compiled source contract.";
        return string.IsNullOrWhiteSpace(detail) ? message : $"{message} {detail}";
    }
}
