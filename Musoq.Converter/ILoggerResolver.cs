using Microsoft.Extensions.Logging;

namespace Musoq.Converter;

public interface ILoggerResolver
{
    public ILogger ResolveLogger();

    public ILogger<T> ResolveLogger<T>();
}