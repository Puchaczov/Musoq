using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Tests;
using Musoq.Benchmarks.CodeGeneration;
using Musoq.Benchmarks.Programs;

var commandArgs = Environment.GetCommandLineArgs();
var isExtendedBenchmarks = commandArgs.Contains("--extended");
var isCompilationBenchmarks = commandArgs.Contains("--compilation");
var isParsingBenchmarks = commandArgs.Contains("--parsing");
var isCodeGenBenchmarks = commandArgs.Contains("--codegen");
var isAnalysisBenchmarks = commandArgs.Contains("--analysis");
var isAnalysisTest = commandArgs.Contains("--test");
var isComprehensiveAnalysis = commandArgs.Contains("--comprehensive");
var isCodeGenerationOptimization = commandArgs.Contains("--code-generation-optimization");

// Handle code generation optimization analysis
if (isCodeGenerationOptimization)
{
    Console.WriteLine("Running Code Generation Optimization Analysis...");
    var analyzer = new CodeGenerationQualityAnalyzer();
    
    // Example analysis with simulated generated code
    var exampleCode = @"
using System;
using System.Collections.Generic;
using Musoq.Schema;

public class GeneratedQuery_Example
{
    public IEnumerable<object[]> Run()
    {
        var schema = provider.GetSchema(""test"");
        var usersTable = schema.GetTable(""users"");
        
        foreach (var usersRow in usersTable.Rows)
        {
            var nameValue = EvaluationHelper.GetValue(usersRow[""Name""], typeof(string));
            var ageValue = EvaluationHelper.GetValue(usersRow[""Age""], typeof(int));
            
            yield return new object[] { nameValue, ageValue };
        }
    }
}";
    
    var report = analyzer.AnalyzeGeneratedCode(exampleCode, "SELECT Name, Age FROM #test.users()");
    report.CalculateEfficiencyScore();
    
    Console.WriteLine("=== CODE GENERATION OPTIMIZATION ANALYSIS REPORT ===");
    Console.WriteLine($"Generated Lines of Code: {report.NonEmptyLinesOfCode}");
    Console.WriteLine($"Reflection Calls: {report.ReflectionCallCount}");
    Console.WriteLine($"Object Allocations: {report.ObjectAllocationCount}");
    Console.WriteLine($"Cyclomatic Complexity: {report.CyclomaticComplexity}");
    Console.WriteLine($"Code Efficiency Score: {report.CodeEfficiencyScore:F2}");
    
    Console.WriteLine("\n=== OPTIMIZATION OPPORTUNITIES ===");
    foreach (var opportunity in report.OptimizationOpportunities)
    {
        Console.WriteLine($"- {opportunity.Type}: {opportunity.Description}");
        Console.WriteLine($"  Impact: {opportunity.Impact}, Estimated Improvement: {opportunity.EstimatedImprovement}");
    }
    
    return;
}

// Handle legacy comprehensive analysis
if (isComprehensiveAnalysis)
{
    Console.WriteLine("Legacy comprehensive analysis is deprecated. Use --code-generation-optimization instead.");
    return;
}

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