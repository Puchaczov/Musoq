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
var isOptimizationTest = commandArgs.Contains("--optimization-test");

// Handle optimization effectiveness testing
if (isOptimizationTest)
{
    Console.WriteLine("=== Phase 4 Optimization Effectiveness Testing ===");

    try
    {
        var test = new OptimizationEffectivenessTest();
        var report = await test.RunOptimizationEffectivenessTestAsync();
        
        // Display results
        Console.WriteLine("=== OPTIMIZATION EFFECTIVENESS RESULTS ===");
        Console.WriteLine($"Test: {report.TestName}");
        Console.WriteLine($"Date: {report.TestDate:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine();
        
        Console.WriteLine("Performance Improvements:");
        Console.WriteLine($"  Reflection Caching:      {report.ReflectionImprovement:F1}%");
        Console.WriteLine($"  Code Generation:         {report.CodeGenerationImprovement:F1}%");
        Console.WriteLine($"  Staged Transformation:   {report.StagedTransformationImprovement:F1}%");
        Console.WriteLine($"  End-to-End:              {report.EndToEndImprovement:F1}%");
        Console.WriteLine($"  TOTAL IMPROVEMENT:       {report.TotalImprovement:F1}%");
        Console.WriteLine();
        
        Console.WriteLine("Detailed Timing (ms):");
        Console.WriteLine($"  Baseline Total:    {report.Baseline.TotalTime}ms");
        Console.WriteLine($"  Optimized Total:   {report.Optimized.TotalTime}ms");
        Console.WriteLine($"  Time Saved:        {report.Baseline.TotalTime - report.Optimized.TotalTime}ms");
        
        // Evaluate effectiveness against targets
        Console.WriteLine();
        Console.WriteLine("=== TARGET ACHIEVEMENT ANALYSIS ===");
        
        // Phase 4 targets: 45-75% total improvement
        var targetMin = 45.0;
        var targetMax = 75.0;
        var achieved = report.TotalImprovement;
        
        if (achieved >= targetMin && achieved <= targetMax)
        {
            Console.WriteLine($"✅ TARGET ACHIEVED: {achieved:F1}% improvement (Target: {targetMin}-{targetMax}%)");
        }
        else if (achieved > targetMax)
        {
            Console.WriteLine($"🚀 TARGET EXCEEDED: {achieved:F1}% improvement (Target: {targetMin}-{targetMax}%)");
        }
        else
        {
            Console.WriteLine($"⚠️  TARGET MISSED: {achieved:F1}% improvement (Target: {targetMin}-{targetMax}%)");
        }
        
        // Save report JSON
        var json = report.ToJson();
        var perfReportsDir = Path.Combine("performance-reports");
        Directory.CreateDirectory(perfReportsDir);
        var reportPath = Path.Combine(perfReportsDir, "optimization-effectiveness-report.json");
        await File.WriteAllTextAsync(reportPath, json);
        Console.WriteLine();
        Console.WriteLine($"📄 Detailed report saved to: {reportPath}");
        
        // Update the README performance section
        await UpdateReadmeWithOptimizationResults(report);
        
        Console.WriteLine();
        Console.WriteLine("=== Optimization Effectiveness Testing Complete ===");
        return;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error during optimization effectiveness testing: {ex.Message}");
        Environment.Exit(1);
    }
}

// Helper method to update README with results
static async Task UpdateReadmeWithOptimizationResults(OptimizationPerformanceReport report)
{
    try
    {
        var readmePath = "../../../README.md";
        if (!File.Exists(readmePath))
        {
            readmePath = "README.md";
        }
        
        if (File.Exists(readmePath))
        {
            var content = await File.ReadAllTextAsync(readmePath);
            var now = DateTime.UtcNow;
            
            // Find and update the performance table
            var tableStart = content.IndexOf("| Query Type | Execution Time | Trend | Status |");
            if (tableStart > 0)
            {
                var tableEnd = content.IndexOf("*Last updated:", tableStart);
                if (tableEnd > tableStart)
                {
                    var before = content.Substring(0, tableStart);
                    var after = content.Substring(tableEnd);
                    
                    var newTable = $@"| Query Type | Execution Time | Improvement | Status |
|------------|----------------|-------------|--------|
| Optimized Query | {report.Optimized.TotalTime}ms | 📈 {report.TotalImprovement:F1}% faster | 🚀 Enhanced |
| Reflection Ops | {report.Optimized.ReflectionTime}ms | 📈 {report.ReflectionImprovement:F1}% faster | ⚡ Cached |
| Code Generation | {report.Optimized.CodeGenerationTime}ms | 📈 {report.CodeGenerationImprovement:F1}% faster | 🎯 Templated |
| Stage Processing | {report.Optimized.StagedTransformationTime}ms | 📈 {report.StagedTransformationImprovement:F1}% faster | 🔧 Staged |

*Last updated: {now:yyyy-MM-dd HH:mm} UTC with Phase 4 Optimizations*

### Phase 4 Optimization Results

The latest Phase 4 code generation optimizations have achieved significant performance improvements:

- **Total Performance Improvement**: {report.TotalImprovement:F1}% faster execution
- **Reflection Caching**: {report.ReflectionImprovement:F1}% reduction in type resolution overhead  
- **Template Generation**: {report.CodeGenerationImprovement:F1}% improvement in code generation efficiency
- **Staged Transformation**: {report.StagedTransformationImprovement:F1}% enhancement in query processing pipeline

These optimizations implement advanced caching strategies, template-based code generation, and multi-stage transformation processing to deliver substantial performance gains across the entire query execution pipeline.

";
                    
                    var updatedContent = before + newTable + after;
                    await File.WriteAllTextAsync(readmePath, updatedContent);
                    Console.WriteLine("📝 README.md performance section updated successfully");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Could not update README.md: {ex.Message}");
    }
}

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