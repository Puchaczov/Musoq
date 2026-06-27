using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class RuntimeV2RegressionLibrary : LibraryBase
{
    [BindableMethod]
    public int ExpensiveMethod(int value)
    {
        return value * 2;
    }

    [BindableMethod]
    public int ExpensiveCompute(int value)
    {
        return value * 3;
    }

    [BindableMethod]
    public int HeavyComputation(int value)
    {
        return value * 5;
    }

    [BindableMethod]
    public string StringTransform(string value)
    {
        return value.ToUpperInvariant();
    }
}
