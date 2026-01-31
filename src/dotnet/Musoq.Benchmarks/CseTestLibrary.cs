using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Benchmarks;

/// <summary>
///     Library with intentionally expensive methods to measure CSE impact.
/// </summary>
public class CseTestLibrary : LibraryBase
{
    /// <summary>
    ///     Simulates an expensive computation that should be cached by CSE.
    ///     Uses CPU-intensive operations to make the performance difference measurable.
    /// </summary>
    [BindableMethod]
    public int ExpensiveMethod(int value)
    {
        double result = value;
        for (var i = 0; i < 500; i++) result = Math.Sqrt(result * result + i) + Math.Sin(i) * Math.Cos(i);
        return (int)result;
    }

    /// <summary>
    ///     Another expensive method for testing multiple CSE candidates.
    /// </summary>
    [BindableMethod]
    public string ExpensiveStringMethod(string value)
    {
        var result = value;
        for (var i = 0; i < 100; i++) result = result.ToUpper().ToLower();
        return result.ToUpper();
    }

    /// <summary>
    ///     Cheap method for comparison (should not benefit much from CSE).
    /// </summary>
    [BindableMethod]
    public int CheapMethod(int value)
    {
        return value * 2;
    }
}
