using Microsoft.Extensions.Logging;
using Musoq.Converter;

namespace Musoq.Playground;

public class MyLoggerResolver : ILoggerResolver
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
