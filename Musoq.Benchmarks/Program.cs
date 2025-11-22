using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks;
using Musoq.Benchmarks.Components;

#if DEBUG
    BenchmarkRunner.Run<JoinBenchmark>(new DebugInProcessConfig());
#else
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif