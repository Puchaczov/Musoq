using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks.Components;

var commandArgs = Environment.GetCommandLineArgs();
var isExtendedBenchmarks = commandArgs.Contains("--extended");
var isCompilationBenchmarks = commandArgs.Contains("--compilation");
var isParsingBenchmarks = commandArgs.Contains("--parsing");
var isCodeGenBenchmarks = commandArgs.Contains("--codegen");

// Run benchmarks normally
#if DEBUG
if (isParsingBenchmarks)
{
    BenchmarkRunner.Run<ParsingPerformanceBenchmark>(
        new DebugInProcessConfig().AddFilter(
            new NameFilter(name => name.Contains(nameof(ParsingPerformanceBenchmark.ParseLongSelectQuery)))
        )
    );
}
else if (isCodeGenBenchmarks)
{
    BenchmarkRunner.Run<CodeGenerationProfilingBenchmark>(
        new DebugInProcessConfig().AddFilter(
            new NameFilter(name => name.Contains(nameof(CodeGenerationProfilingBenchmark.GenerateAndCompileCode)))
        )
    );
}
else if (isCompilationBenchmarks)
{
    BenchmarkRunner.Run<CompilationBenchmark>(
        new DebugInProcessConfig().AddFilter(
            new NameFilter(name => name.Contains(nameof(CompilationBenchmark.CompileSimpleQuery_Profiles)))
        )
    );
}
else if (isExtendedBenchmarks)
{
    BenchmarkRunner.Run<ExtendedExecutionBenchmark>(
        new DebugInProcessConfig().AddFilter(
            new NameFilter(name => name.Contains(nameof(ExtendedExecutionBenchmark.SimpleSelect_Profiles)))
        )
    );
}
else
{
    BenchmarkRunner.Run<ExecutionBenchmark>(
        new DebugInProcessConfig().AddFilter(
            new NameFilter(name => name.Contains(nameof(ExecutionBenchmark.ComputeSimpleSelect_WithParallelization_10MbOfData_Profiles))))
    );
}
#else
if (isParsingBenchmarks)
{
    BenchmarkRunner.Run<ParsingPerformanceBenchmark>();
}
else if (isCodeGenBenchmarks)
{
    BenchmarkRunner.Run<CodeGenerationProfilingBenchmark>();
}
else if (isCompilationBenchmarks)
{
    BenchmarkRunner.Run<CompilationBenchmark>();
}
else if (isExtendedBenchmarks)
{
    BenchmarkRunner.Run<ExtendedExecutionBenchmark>();
}
else
{
    BenchmarkRunner.Run<ExecutionBenchmark>();
}
#endif