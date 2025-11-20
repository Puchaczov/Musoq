using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks;
using Musoq.Benchmarks.Components;

#if DEBUG
    BenchmarkRunner.Run<JoinBenchmark>(new DebugInProcessConfig());
#else
    BenchmarkRunner.Run<JoinBenchmark>();
#endif