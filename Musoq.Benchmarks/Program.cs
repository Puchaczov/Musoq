using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Tests;
using Musoq.Benchmarks.CodeGeneration;

var commandArgs = Environment.GetCommandLineArgs();
var isExtendedBenchmarks = commandArgs.Contains("--extended");
var isCompilationBenchmarks = commandArgs.Contains("--compilation");
var isParsingBenchmarks = commandArgs.Contains("--parsing");
var isCodeGenBenchmarks = commandArgs.Contains("--codegen");
var isAnalysisBenchmarks = commandArgs.Contains("--analysis");
var isAnalysisTest = commandArgs.Contains("--test");

// Handle custom analysis
if (isAnalysisBenchmarks)
{
    Console.WriteLine("Running Code Generation Performance Analysis...");
    var runner = new PerformanceAnalysisRunner();
    var report = await runner.RunCompleteAnalysis();
    
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    var fileName = $"Musoq_Performance_Analysis_{timestamp}.md";
    var filePath = Path.Combine(Environment.CurrentDirectory, fileName);
    
    await File.WriteAllTextAsync(filePath, report);
    Console.WriteLine($"Report saved to: {filePath}");
    return;
}

if (isAnalysisTest)
{
    Console.WriteLine("Running basic analysis test...");
    await CodeGenerationAnalysisTest.TestBasicAnalysis();
    return;
}

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