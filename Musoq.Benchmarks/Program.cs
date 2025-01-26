using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks.Components;

#if DEBUG
    BenchmarkRunner.Run<ExecutionBenchmark>(
        new DebugInProcessConfig().AddFilter(
            new NameFilter(name => name.Contains(nameof(ExecutionBenchmark.ComputeSimpleSelect_WithParallelization_10MbOfData_Profiles))))
    );
#else
    BenchmarkRunner.Run<ExecutionBenchmark>();
#endif