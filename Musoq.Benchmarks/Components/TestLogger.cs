using Microsoft.Extensions.Logging;

namespace Musoq.Benchmarks.Components;

/// <summary>
/// Test logger implementation for performance analysis
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false; // Disable logging during performance analysis
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // No-op for performance analysis
    }
}