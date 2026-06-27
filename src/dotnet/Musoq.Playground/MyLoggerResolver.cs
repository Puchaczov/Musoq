using Microsoft.Extensions.Logging;
using Musoq.Converter;

namespace Musoq.Playground;

internal sealed class MyLoggerResolver : ILoggerResolver
{
    public ILogger ResolveLogger()
    {
        return new NoOpLogger();
    }

    public ILogger<T> ResolveLogger<T>()
    {
        return new NoOpLogger<T>();
    }
}
