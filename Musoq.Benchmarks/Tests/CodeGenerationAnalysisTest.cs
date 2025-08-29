using Musoq.Benchmarks.CodeGeneration;
using Musoq.Benchmarks.Schema;
using Musoq.Benchmarks.Schema.Country;

namespace Musoq.Benchmarks.Tests;

/// <summary>
/// Simple test to validate the code generation analysis works
/// </summary>
public class CodeGenerationAnalysisTest
{
    public static async Task TestBasicAnalysis()
    {
        Console.WriteLine("Testing Code Generation Analysis...");
        
        // Create analyzer
        var schemaProvider = new GenericSchemaProvider<CountryEntity, CountryEntityTable>(
            new Dictionary<string, IEnumerable<CountryEntity>>
            {
                {"#test", GenerateTestData()}
            },
            CountryEntity.KNameToIndexMap,
            CountryEntity.KIndexToObjectAccessMap
        );
        
        var analyzer = new CodeGenerationAnalyzer(schemaProvider);
        
        // Test a simple query
        var result = analyzer.AnalyzeQuery("SELECT City, Population FROM #test.Entities()", "Simple Test");
        
        Console.WriteLine($"Query Analysis Result:");
        Console.WriteLine($"  Success: {result.IsSuccessful}");
        if (result.IsSuccessful)
        {
            Console.WriteLine($"  Generated Code Lines: {result.CodeMetrics.NonEmptyLines}");
            Console.WriteLine($"  Execution Time: {result.PerformanceMetrics.ExecutionTimeMs}ms");
            Console.WriteLine($"  Memory Used: {result.PerformanceMetrics.MemoryUsedBytes} bytes");
            Console.WriteLine($"  Row Count: {result.PerformanceMetrics.RowCount}");
        }
        else
        {
            Console.WriteLine($"  Error: {result.Error}");
        }
    }
    
    private static IEnumerable<CountryEntity> GenerateTestData()
    {
        for (int i = 0; i < 10; i++)
        {
            yield return new CountryEntity($"City{i}", $"Country{i % 3}", 1000000 + i * 100000);
        }
    }
}