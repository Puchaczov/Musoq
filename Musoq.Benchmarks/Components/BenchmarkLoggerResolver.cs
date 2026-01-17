using Microsoft.Extensions.Logging;
using Musoq.Converter;

namespace Musoq.Benchmarks.Components;

public class BenchmarkLoggerResolver : ILoggerResolver
{
    public ILogger ResolveLogger()
    {
        return new EmptyLogger<object>();
    }

    public ILogger<T> ResolveLogger<T>()
    {
        return new EmptyLogger<T>();
    }

    private class EmptyLogger<T> : ILogger<T>
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }
    }
}