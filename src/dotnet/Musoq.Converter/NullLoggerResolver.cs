using Microsoft.Extensions.Logging;

namespace Musoq.Converter;

internal sealed class NullLoggerResolver : ILoggerResolver
{
    public static readonly NullLoggerResolver Instance = new();

    private NullLoggerResolver()
    {
    }

    public ILogger ResolveLogger()
    {
        return NullLogger.Instance;
    }

    public ILogger<T> ResolveLogger<T>()
    {
        return NullLogger<T>.Instance;
    }

    private sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }

    private sealed class NullLogger<T> : ILogger<T>
    {
        public static readonly NullLogger<T> Instance = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}
