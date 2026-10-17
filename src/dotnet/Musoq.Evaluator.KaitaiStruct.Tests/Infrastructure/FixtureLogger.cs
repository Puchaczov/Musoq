using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Converter;

namespace Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;

internal sealed class FixtureLogger : ILoggerResolver
{
    public ILogger ResolveLogger() => NullLogger.Instance;
    public ILogger<T> ResolveLogger<T>() => NullLogger<T>.Instance;
}
