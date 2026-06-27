using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Benchmarks;

public sealed class OptimizationBenchmarkLibrary : LibraryBase
{
    [BindableMethod]
    public decimal ExpensiveCompute(int value)
    {
        decimal result = value;

        for (var index = 0; index < 80; index++)
            result = result * 1.07m + (decimal)Math.Sin(index + value % 13);

        return Math.Round(result, 2);
    }

    [BindableMethod]
    public string? ExpensiveNormalize(string? value)
    {
        if (value == null)
            return null;

        var result = value;

        for (var index = 0; index < 40; index++)
            result = result.ToUpperInvariant().ToLowerInvariant();

        return result.ToUpperInvariant();
    }
}
