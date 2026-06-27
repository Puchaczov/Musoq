namespace Musoq.Schema.Optimization;

public sealed record SourceContractDiagnostic(
    SourceContractDiagnosticSeverity Severity,
    string Message,
    string? Code = null)
{
    public string? Origin { get; init; }

    public string? ColumnName { get; init; }

    public string? ModifierKey { get; init; }

    public static SourceContractDiagnostic Info(string message, string? code = null)
    {
        return new SourceContractDiagnostic(SourceContractDiagnosticSeverity.Info, message, code);
    }

    public static SourceContractDiagnostic Warning(string message, string? code = null)
    {
        return new SourceContractDiagnostic(SourceContractDiagnosticSeverity.Warning, message, code);
    }

    public static SourceContractDiagnostic Error(string message, string? code = null)
    {
        return new SourceContractDiagnostic(SourceContractDiagnosticSeverity.Error, message, code);
    }
}
