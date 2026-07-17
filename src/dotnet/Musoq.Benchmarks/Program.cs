using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks;
using Musoq.Benchmarks.Performance;

if (args is ["compare-reports", .. var comparisonArgs])
    return BenchmarkComparisonCommand.Run(comparisonArgs, Console.Out, Console.Error);

#if DEBUG
BenchmarkRunner.Run<JoinBenchmark>(new DebugInProcessConfig());
#else
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif

return 0;
