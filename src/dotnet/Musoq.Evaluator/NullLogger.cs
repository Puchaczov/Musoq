using System;
using Microsoft.Extensions.Logging;

namespace Musoq.Evaluator;

/// <summary>
///     Null implementation of ILogger for when no logging is configured.
/// </summary>
internal sealed class NullLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }
}
