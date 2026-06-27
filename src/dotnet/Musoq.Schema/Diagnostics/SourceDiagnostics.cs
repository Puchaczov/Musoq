namespace Musoq.Schema.Diagnostics;

public sealed class SourceDiagnostics
{
    private readonly ISourceDiagnosticsSink? _sink;

    public SourceDiagnostics(ISourceDiagnosticsSink? sink)
    {
        _sink = sink;
    }

    public static SourceDiagnostics None { get; } = new(null);

    public bool IsEnabled => _sink != null;

    public SourceDiagnosticScope Measure(
        string name,
        SourceDiagnosticOperation operation = SourceDiagnosticOperation.Other)
    {
        return _sink == null
            ? default
            : new SourceDiagnosticScope(_sink.Measure(name, operation));
    }

    public void AddRowsProduced(long count)
    {
        _sink?.AddRowsProduced(count);
    }

    public void AddBytesRead(long bytes)
    {
        _sink?.AddBytesRead(bytes);
    }

    public void AddMetric(string name, long value)
    {
        _sink?.AddMetric(name, value);
    }
}
