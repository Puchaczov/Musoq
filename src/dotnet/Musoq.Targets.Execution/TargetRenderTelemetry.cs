using System;
using System.Threading;

namespace Musoq.Targets.Execution;

/// <summary>
/// Optional target-neutral render instrumentation. The converter owns the
/// recorder; targets only see an ambient phase callback.
/// </summary>
internal static class TargetRenderTelemetry
{
    private static readonly AsyncLocal<Func<string, IDisposable>?> Current = new();
    private static readonly IDisposable Noop = new NoopScope();

    internal static IDisposable Push(Func<string, IDisposable>? beginPhase)
    {
        var previous = Current.Value;
        if (beginPhase is null && previous is null)
            return Noop;

        Current.Value = beginPhase;
        return new Scope(previous);
    }

    internal static IDisposable BeginPhase(string name)
    {
        return Current.Value?.Invoke(name) ?? Noop;
    }

    private sealed class Scope(Func<string, IDisposable>? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Current.Value = previous;
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
