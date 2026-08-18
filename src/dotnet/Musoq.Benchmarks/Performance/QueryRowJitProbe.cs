namespace Musoq.Benchmarks.Performance;

internal static class QueryRowJitProbe
{
    public static int Run(TextWriter output)
    {
        var benchmark = new QueryScopedSourceMaterializationBenchmark { FieldCount = 8 };
        try
        {
            benchmark.Setup();
            var checksum = 0;
            for (var iteration = 0; iteration < 256; iteration++)
                checksum ^= benchmark.QueryScopedNumericStructMaterialization();
            output.WriteLine($"Query-row JIT probe checksum: {checksum}.");
            return 0;
        }
        finally
        {
            benchmark.Cleanup();
        }
    }
}
