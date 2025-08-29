using Musoq.Benchmarks.Programs;

namespace Musoq.Benchmarks.Programs;

/// <summary>
/// Main program for running comprehensive performance analysis
/// </summary>
public class RunComprehensiveAnalysis
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Musoq Comprehensive Performance Analysis");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        try
        {
            // Run the comprehensive analysis
            var analyzer = new ComprehensivePerformanceAnalysis();
            var report = await analyzer.RunAnalysisAsync();

            // Generate the markdown report
            var reportGenerator = new PerformanceReportGenerator();
            var markdownReport = reportGenerator.GenerateMarkdownReport(report);

            // Save the report
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"Musoq_Comprehensive_Performance_Analysis_{timestamp}.md";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            await File.WriteAllTextAsync(filePath, markdownReport);

            Console.WriteLine();
            Console.WriteLine($"📄 Analysis complete! Report saved to: {fileName}");
            Console.WriteLine();

            // Display summary
            DisplaySummary(report);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during analysis: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private static void DisplaySummary(ComprehensiveAnalysisReport report)
    {
        Console.WriteLine("📊 ANALYSIS SUMMARY");
        Console.WriteLine("==================");
        Console.WriteLine();

        Console.WriteLine("🔄 Visitor Pattern Performance:");
        Console.WriteLine($"   • Average time: {report.VisitorPatternAnalysis.AverageVisitorTime:F2}ms");
        Console.WriteLine($"   • Total calls: {report.VisitorPatternAnalysis.TotalVisitorCalls}");
        Console.WriteLine($"   • Queries analyzed: {report.VisitorPatternAnalysis.Results.Count}");
        Console.WriteLine();

        Console.WriteLine("🏗️ Code Generation Performance:");
        Console.WriteLine($"   • Average generated lines: {report.CodeGenerationAnalysis.AverageGeneratedLines:F0}");
        Console.WriteLine($"   • Average compilation time: {report.CodeGenerationAnalysis.AverageCompilationTime:F2}ms");
        Console.WriteLine($"   • Total reflection calls: {report.CodeGenerationAnalysis.TotalReflectionCalls}");
        Console.WriteLine();

        Console.WriteLine("⚙️ Compilation Performance:");
        Console.WriteLine($"   • Average compilation time: {report.CompilationAnalysis.AverageCompilationTime:F2}ms");
        Console.WriteLine($"   • Average memory used: {report.CompilationAnalysis.AverageMemoryUsed / 1024:F0}KB");
        Console.WriteLine();

        Console.WriteLine("🚨 Performance Issues:");
        Console.WriteLine($"   • Bottlenecks identified: {report.BottleneckAnalysis.Bottlenecks.Count}");
        
        var highPriorityCount = report.OptimizationRecommendations.Count(r => r.Priority == "High");
        var mediumPriorityCount = report.OptimizationRecommendations.Count(r => r.Priority == "Medium");
        
        Console.WriteLine($"   • High-priority optimizations: {highPriorityCount}");
        Console.WriteLine($"   • Medium-priority optimizations: {mediumPriorityCount}");
        Console.WriteLine();

        if (highPriorityCount > 0)
        {
            Console.WriteLine("⚠️  HIGH PRIORITY RECOMMENDATIONS:");
            foreach (var rec in report.OptimizationRecommendations.Where(r => r.Priority == "High"))
            {
                Console.WriteLine($"   • {rec.Title}");
                Console.WriteLine($"     Impact: {rec.EstimatedImpact}");
                Console.WriteLine($"     Effort: {rec.ImplementationEffort}");
                Console.WriteLine();
            }
        }

        Console.WriteLine("🎯 NEXT STEPS:");
        Console.WriteLine("   1. Review the detailed report for specific optimization strategies");
        Console.WriteLine("   2. Prioritize high-impact, low-effort optimizations first");
        Console.WriteLine("   3. Implement reflection caching and code generation templates");
        Console.WriteLine("   4. Set up continuous performance monitoring");
        Console.WriteLine();
    }
}