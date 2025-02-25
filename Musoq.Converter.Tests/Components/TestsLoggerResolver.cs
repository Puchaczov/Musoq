using Microsoft.Extensions.Logging;
using Moq;

namespace Musoq.Converter.Tests.Components;

public class TestsLoggerResolver : ILoggerResolver
{
    public ILogger ResolveLogger()
    {
        var logger = new Mock<ILogger>();
        
        return logger.Object;
    }

    public ILogger<T> ResolveLogger<T>()
    {
        var logger = new Mock<ILogger<T>>();
        
        return logger.Object;
    }
}