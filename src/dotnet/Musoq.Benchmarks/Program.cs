using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks;
using Musoq.Benchmarks.Performance;

if (args is ["compare-reports", .. var comparisonArgs])
    return BenchmarkComparisonCommand.Run(comparisonArgs, Console.Out, Console.Error);
if (args is ["gate-recursive", .. var recursiveArgs])
    return RecursiveCteBenchmarkGateCommand.Run(recursiveArgs, Console.Out, Console.Error);
if (args is ["gate-query-rows", .. var queryRowArgs])
    return QueryRowQualificationGateCommand.Run(queryRowArgs, Console.Out, Console.Error);
if (args is ["gate-loop-invariant", .. var loopInvariantArgs])
    return LoopInvariantQualificationGateCommand.Run(loopInvariantArgs, Console.Out, Console.Error);
if (args is ["gate-stability-aware-reuse", .. var stabilityAwareReuseArgs])
    return StabilityAwareScalarReuseQualificationGateCommand.Run(
        stabilityAwareReuseArgs,
        Console.Out,
        Console.Error);
if (args is ["gate-stability-aware-reuse-families", .. var familyArgs])
    return StabilityAwareScalarReuseFamilyQualificationGateCommand.Run(
        familyArgs,
        Console.Out,
        Console.Error);
if (args is ["gate-enums", .. var enumArgs])
    return FirstClassEnumQualificationGateCommand.Run(enumArgs, Console.Out, Console.Error);
if (args is ["jit-query-row"])
    return QueryRowJitProbe.Run(Console.Out);

#if DEBUG
BenchmarkRunner.Run<JoinBenchmark>(new DebugInProcessConfig());
#else
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif

return 0;
