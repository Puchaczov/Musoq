using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks;
using Musoq.Benchmarks.Components;

#if DEBUG
    BenchmarkRunner.Run<ConversionBenchmark>(new DebugInProcessConfig());
#else
    BenchmarkRunner.Run<ConversionBenchmark>();
#endif