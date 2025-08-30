using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Benchmarks.Tests;
using System.Text.Json;

namespace Musoq.Benchmarks.Programs;

/// <summary>
/// Program to run optimization effectiveness tests and generate performance reports.
/// </summary>
public class OptimizationEffectivenessProgram
{
    public static async Task Main(string[] args)
    {
        var logger = new NullLogger<OptimizationEffectivenessProgram>();
            
        logger.LogInformation("=== Phase 4 Optimization Effectiveness Testing ===");
        
        try
        {
            var test = new OptimizationEffectivenessTest();
            var report = await test.RunOptimizationEffectivenessTestAsync();
            
            // Display results
            DisplayResults(report, logger);
            
            // Save detailed report
            await SavePerformanceReport(report, logger);
            
            // Update README performance section
            await UpdateReadmePerformanceSection(report, logger);
            
            logger.LogInformation("=== Optimization Effectiveness Testing Complete ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during optimization effectiveness testing");
            throw;
        }
    }
    
    private static void DisplayResults(OptimizationPerformanceReport report, ILogger logger)
    {
        logger.LogInformation("=== OPTIMIZATION EFFECTIVENESS RESULTS ===");
        logger.LogInformation($"Test: {report.TestName}");
        logger.LogInformation($"Date: {report.TestDate:yyyy-MM-dd HH:mm:ss} UTC");
        logger.LogInformation("");
        
        logger.LogInformation("Performance Improvements:");
        logger.LogInformation($"  Reflection Caching:      {report.ReflectionImprovement:F1}%");
        logger.LogInformation($"  Code Generation:         {report.CodeGenerationImprovement:F1}%");
        logger.LogInformation($"  Staged Transformation:   {report.StagedTransformationImprovement:F1}%");
        logger.LogInformation($"  End-to-End:              {report.EndToEndImprovement:F1}%");
        logger.LogInformation($"  TOTAL IMPROVEMENT:       {report.TotalImprovement:F1}%");
        logger.LogInformation("");
        
        logger.LogInformation("Detailed Timing (ms):");
        logger.LogInformation($"  Baseline Total:    {report.Baseline.TotalTime}ms");
        logger.LogInformation($"  Optimized Total:   {report.Optimized.TotalTime}ms");
        logger.LogInformation($"  Time Saved:        {report.Baseline.TotalTime - report.Optimized.TotalTime}ms");
        
        // Evaluate effectiveness against targets
        EvaluateTargetAchievement(report, logger);
    }
    
    private static void EvaluateTargetAchievement(OptimizationPerformanceReport report, ILogger logger)
    {
        logger.LogInformation("");
        logger.LogInformation("=== TARGET ACHIEVEMENT ANALYSIS ===");
        
        // Phase 4 targets: 45-75% total improvement
        var targetMin = 45.0;
        var targetMax = 75.0;
        var achieved = report.TotalImprovement;
        
        if (achieved >= targetMin && achieved <= targetMax)
        {
            logger.LogInformation($"✅ TARGET ACHIEVED: {achieved:F1}% improvement (Target: {targetMin}-{targetMax}%)");
        }
        else if (achieved > targetMax)
        {
            logger.LogInformation($"🚀 TARGET EXCEEDED: {achieved:F1}% improvement (Target: {targetMin}-{targetMax}%)");
        }
        else
        {
            logger.LogInformation($"⚠️  TARGET MISSED: {achieved:F1}% improvement (Target: {targetMin}-{targetMax}%)");
        }
        
        // Individual component targets
        logger.LogInformation("");
        logger.LogInformation("Component Target Analysis:");
        CheckComponentTarget("Reflection Caching", report.ReflectionImprovement, 30, 50, logger);
        CheckComponentTarget("Code Generation", report.CodeGenerationImprovement, 20, 30, logger);
        CheckComponentTarget("Staged Transformation", report.StagedTransformationImprovement, 15, 25, logger);
    }
    
    private static void CheckComponentTarget(string component, double actual, double minTarget, double maxTarget, ILogger logger)
    {
        var status = actual >= minTarget ? "✅" : "⚠️";
        logger.LogInformation($"  {status} {component}: {actual:F1}% (Target: {minTarget}-{maxTarget}%)");
    }
    
    private static async Task SavePerformanceReport(OptimizationPerformanceReport report, ILogger logger)
    {
        try
        {
            var reportsDir = Path.Combine("performance-reports");
            Directory.CreateDirectory(reportsDir);
            
            // Save detailed JSON report
            var jsonPath = Path.Combine(reportsDir, $"optimization-effectiveness-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            await File.WriteAllTextAsync(jsonPath, report.ToJson());
            
            // Save summary for README
            var summaryPath = Path.Combine(reportsDir, "optimization-effectiveness-summary.json");
            await File.WriteAllTextAsync(summaryPath, report.ToJson());
            
            logger.LogInformation($"Performance report saved to: {jsonPath}");
            logger.LogInformation($"Summary report saved to: {summaryPath}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving performance report");
        }
    }
    
    private static async Task UpdateReadmePerformanceSection(OptimizationPerformanceReport report, ILogger logger)
    {
        try
        {
            logger.LogInformation("Updating README.md performance section...");
            
            var readmePath = Path.Combine("..", "..", "..", "README.md");
            if (!File.Exists(readmePath))
            {
                readmePath = "README.md"; // Try current directory
            }
            
            if (File.Exists(readmePath))
            {
                var readmeContent = await File.ReadAllTextAsync(readmePath);
                var updatedContent = UpdatePerformanceSection(readmeContent, report);
                await File.WriteAllTextAsync(readmePath, updatedContent);
                
                logger.LogInformation("README.md performance section updated successfully");
            }
            else
            {
                logger.LogWarning("README.md not found - skipping update");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating README.md");
        }
    }
    
    private static string UpdatePerformanceSection(string readmeContent, OptimizationPerformanceReport report)
    {
        // Find and update the current performance summary table
        var currentTableStart = readmeContent.IndexOf("| Query Type | Execution Time | Trend | Status |");
        if (currentTableStart > 0)
        {
            var currentTableEnd = readmeContent.IndexOf("*Last updated:", currentTableStart);
            if (currentTableEnd > currentTableStart)
            {
                var beforeTable = readmeContent.Substring(0, currentTableStart);
                var afterTable = readmeContent.Substring(currentTableEnd);
                
                var newTable = GenerateUpdatedPerformanceTable(report);
                return beforeTable + newTable + afterTable;
            }
        }
        
        return readmeContent; // Return unchanged if table not found
    }
    
    private static string GenerateUpdatedPerformanceTable(OptimizationPerformanceReport report)
    {
        var now = DateTime.UtcNow;
        
        return $@"| Query Type | Execution Time | Improvement | Status |
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
    }
}