using BenchmarkDotNet.Attributes;
using Musoq.Plugins;

namespace Musoq.Benchmarks;

[MemoryDiagnoser]
public class ConversionBenchmark
{
    private readonly LibraryBase _lib = new();
    private readonly object[] _testValues;

    public ConversionBenchmark()
    {
        _testValues = new object[]
        {
            (byte)100,
            (short)500,
            1000,
            5000L,
            100.5f,
            250.75,
            500.50m,
            "1500",
            true
        };
    }

    [Benchmark]
    public void ConvertToInt32Strict()
    {
        foreach (var value in _testValues)
        {
            _ = _lib.TryConvertToInt32Strict(value);
        }
    }

    [Benchmark]
    public void ConvertToInt64Strict()
    {
        foreach (var value in _testValues)
        {
            _ = _lib.TryConvertToInt64Strict(value);
        }
    }

    [Benchmark]
    public void ConvertToDecimalStrict()
    {
        foreach (var value in _testValues)
        {
            _ = _lib.TryConvertToDecimalStrict(value);
        }
    }

    [Benchmark]
    public void ConvertToInt32Comparison()
    {
        foreach (var value in _testValues)
        {
            _ = _lib.TryConvertToInt32Comparison(value);
        }
    }

    [Benchmark]
    public void ConvertToInt64Comparison()
    {
        foreach (var value in _testValues)
        {
            _ = _lib.TryConvertToInt64Comparison(value);
        }
    }

    [Benchmark]
    public void ConvertToDecimalComparison()
    {
        foreach (var value in _testValues)
        {
            _ = _lib.TryConvertToDecimalComparison(value);
        }
    }
}
