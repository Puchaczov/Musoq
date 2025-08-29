using Musoq.Benchmarks.CodeGeneration;

namespace Musoq.Benchmarks.Programs;

/// <summary>
/// Entry point for running code generation performance analysis
/// </summary>
public class CodeGenerationAnalysisProgram
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Musoq Code Generation Performance Analysis");
        Console.WriteLine("=========================================");
        Console.WriteLine();
        
        try
        {
            var runner = new PerformanceAnalysisRunner();
            var report = await runner.RunCompleteAnalysis();
            
            // Save report to file
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"Musoq_Performance_Analysis_{timestamp}.md";
            var filePath = Path.Combine(Environment.CurrentDirectory, fileName);
            
            await File.WriteAllTextAsync(filePath, report);
            
            Console.WriteLine();
            Console.WriteLine($"Report saved to: {filePath}");
            Console.WriteLine();
            Console.WriteLine("Report Preview:");
            Console.WriteLine(new string('=', 50));
            
            // Show first part of report
            var lines = report.Split('\n');
            foreach (var line in lines.Take(50))
            {
                Console.WriteLine(line);
            }
            
            if (lines.Length > 50)
            {
                Console.WriteLine("... (see full report in file)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running analysis: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}