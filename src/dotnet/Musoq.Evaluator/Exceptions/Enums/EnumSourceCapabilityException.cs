using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

internal sealed class EnumSourceCapabilityException : Exception, IDiagnosticException
{
    public EnumSourceCapabilityException(string sourceName, string columnName, TextSpan span)
        : base($"Source '{sourceName}' does not support logical scalar reads required by enum column '{columnName}'.")
    {
        SourceName = sourceName;
        ColumnName = columnName;
        Span = span;
    }

    public string SourceName { get; }

    public string ColumnName { get; }

    public DiagnosticCode Code => DiagnosticCode.MQ3114_EnumSourceCapabilityRequired;

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty)
            .WithArgument("source", SourceName)
            .WithArgument("column", ColumnName);
    }
}
