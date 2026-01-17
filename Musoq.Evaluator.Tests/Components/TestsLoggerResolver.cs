using Microsoft.Extensions.Logging;
using Moq;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests.Components;

public class TestsLoggerResolver : ILoggerResolver
{
    public ILogger ResolveLogger()
    {
        var loggerMock = new Mock<ILogger>();

        return loggerMock.Object;
    }

    public ILogger<T> ResolveLogger<T>()
    {
        var loggerMock = new Mock<ILogger<T>>();

        return loggerMock.Object;
    }
}