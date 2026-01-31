using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Benchmarks;

public class BenchmarkLibrary : LibraryBase
{
    [BindableMethod]
    public int HeavyComputation(int value)
    {
        double result = value;
        for (var i = 0; i < 1000; i++) result = Math.Sqrt(result * result + i + Math.Sin(i));
        return (int)result;
    }
}
