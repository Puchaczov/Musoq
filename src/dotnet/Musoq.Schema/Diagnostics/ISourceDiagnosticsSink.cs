namespace Musoq.Schema.Diagnostics;

public interface ISourceDiagnosticsSink
{
    IDisposable Measure(string name, SourceDiagnosticOperation operation);

    void AddRowsProduced(long count);

    void AddBytesRead(long bytes);

    void AddMetric(string name, long value);
}
