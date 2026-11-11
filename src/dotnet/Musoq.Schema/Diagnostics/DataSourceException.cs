using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Schema.Diagnostics;

/// <summary>A provider-authored public error with private debugging details retained separately.</summary>
public sealed class DataSourceException : Exception, IDiagnosticException
{
    public DataSourceException(DataSourceDiagnostic report, Exception? innerException = null)
        : base((report ?? throw new ArgumentNullException(nameof(report))).Message, innerException)
    {
        if (report.Severity != DataSourceDiagnosticSeverity.Error)
            throw new ArgumentException("Warnings must be reported without throwing an exception.", nameof(report));
        Report = report;
    }

    public DataSourceDiagnostic Report { get; }
    public DiagnosticCode Code => DiagnosticCode.MQ7020_ReportedDataSourceFailure;
    public TextSpan? Span => null;

    public Diagnostic ToDiagnostic(SourceText? sourceText = null) => Report.ToDiagnostic();
}
