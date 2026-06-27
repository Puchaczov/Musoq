namespace Musoq.Schema.Diagnostics;

public readonly struct SourceDiagnosticScope : IDisposable
{
    private readonly IDisposable? _scope;

    internal SourceDiagnosticScope(IDisposable? scope)
    {
        _scope = scope;
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}
