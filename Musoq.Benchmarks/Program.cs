using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks;

#if DEBUG
BenchmarkRunner.Run<JoinBenchmark>(new DebugInProcessConfig());
#else
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif