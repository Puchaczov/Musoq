using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateLibraryExecutionPerformanceSamples() =>
    [
        NumericPerformanceSample<byte>("K87_NativeByteKernelState", (byte)1, (byte)2),
        NumericPerformanceSample<sbyte>("K88_NativeSByteKernelState", (sbyte)1, (sbyte)2),
        NumericPerformanceSample<short>("K89_NativeInt16KernelState", (short)1, (short)2),
        NumericPerformanceSample<ushort>("K90_NativeUInt16KernelState", (ushort)1, (ushort)2),
        NumericPerformanceSample<uint>("K91_NativeUInt32KernelState", 1u, 2u),
        NumericPerformanceSample<long>("K92_NativeInt64KernelState", 1L, 2L),
        NumericPerformanceSample<ulong>("K93_NativeUInt64KernelState", 1UL, 2UL),
        NumericPerformanceSample<float>("K94_NativeSingleKernelState", 1f, 2f)
    ];

    private static GeneratedCodeSample NumericPerformanceSample<T>(string name, T first, T second) where T : struct => new()
    {
        Name = name, FileName = name + ".cs",
        Query = "select Sum(Value) as Total, Avg(Value) as Average, Min(Value) as Minimum, " +
            "Max(Value) as Maximum, Count(Value) as Present from #schema.first()",
        Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
        CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false),
        CreateSchemaProvider = () => LibraryExecutionFixture.CreateProvider(
            new LibraryExecutionPerformanceQueryTests.NumericInput<T>[] { new(first), new(second), new(null) })
    };
}
