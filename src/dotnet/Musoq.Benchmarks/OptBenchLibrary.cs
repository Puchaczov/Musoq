using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Benchmarks;

public class OptBenchLibrary : LibraryBase
{
    /// <summary>
    ///     Simulates an expensive computation (e.g., complex math, parsing, etc.)
    /// </summary>
    [BindableMethod]
    public decimal ExpensiveCompute(int value)
    {
        decimal result = value;
        for (var i = 0; i < 100; i++) result = result * 1.1m + (decimal)Math.Sin(i);
        return Math.Round(result, 2);
    }

    /// <summary>
    ///     Simulates an expensive string transformation
    /// </summary>
    [BindableMethod]
    public string? StringTransform(string? input)
    {
        if (input == null) return null;


        var result = input;
        for (var i = 0; i < 50; i++) result = result.ToUpper().ToLower();
        return result.ToUpperInvariant() + "_transformed";
    }
}
