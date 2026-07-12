using System.Threading;

namespace Musoq.Converter.Build;

internal sealed class TemporaryExecutionTargetCatalogOverride(Action restore) : IDisposable
{
    private int _disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        restore();
    }
}
