using BenchmarkDotNet.Attributes;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Tests.Common;
using Musoq.Benchmarks.Schema.Country;
using Musoq.Benchmarks.Schema;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Musoq.Benchmarks.Components;

[MemoryDiagnoser]
[SimpleJob]
public class CodeGenerationProfilingBenchmark : BenchmarkBase
{
    private string _longSelectQuery = null!;
    private IDictionary<string, IEnumerable<CountryEntity>> _testData = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _longSelectQuery = GenerateLongSelectQuery();
        _testData = new Dictionary<string, IEnumerable<CountryEntity>>
        {
            {"#test", GenerateTestData()}
        };
        
        Console.WriteLine($"Setup complete. Query length: {_longSelectQuery.Length:N0} characters");
    }

    [Benchmark]
    public void GenerateAndCompileCode()
    {
        var stopwatch = Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(false);
        
        try
        {
            // Compile the query and measure code generation efficiency
            var compiledQuery = CreateForCountryWithOptions(
                _longSelectQuery,
                _testData,
                new CompilationOptions(ParallelizationMode.None));
            
            stopwatch.Stop();
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = memoryAfter - memoryBefore;
            
            // Profile the generated assembly
            var assembly = compiledQuery.GetType().Assembly;
            var types = assembly.GetTypes();
            var methods = types.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
            
            // Debug output for profiling
            Console.WriteLine($"Code generation completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Memory allocated: {memoryUsed:N0} bytes");
            Console.WriteLine($"Generated assembly location: {assembly.Location?.Length ?? 0:N0}");
            Console.WriteLine($"Generated types: {types.Length:N0}");
            Console.WriteLine($"Generated methods: {methods.Count():N0}");
            
            // Analyze method complexity
            AnalyzeGeneratedCode(assembly);
            
            // Test execution efficiency
            var executionStopwatch = Stopwatch.StartNew();
            var results = compiledQuery.Run().ToArray();
            executionStopwatch.Stop();
            
            Console.WriteLine($"Execution completed in {executionStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Results count: {results.Length:N0}");
            
            // Force GC to measure cleanup efficiency
            var beforeGC = GC.GetTotalMemory(false);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var afterGC = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory freed by GC: {beforeGC - afterGC:N0} bytes");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"Code generation failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
            throw;
        }
    }
    
    private static void AnalyzeGeneratedCode(Assembly assembly)
    {
        var types = assembly.GetTypes();
        var totalMethods = 0;
        var totalInstructions = 0;
        
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            totalMethods += methods.Length;
            
            foreach (var method in methods)
            {
                try
                {
                    var methodBody = method.GetMethodBody();
                    if (methodBody != null)
                    {
                        totalInstructions += methodBody.GetILAsByteArray()?.Length ?? 0;
                    }
                }
                catch
                {
                    // Skip methods that can't be analyzed
                }
            }
        }
        
        Console.WriteLine($"Code analysis - Average IL bytes per method: {(totalMethods > 0 ? totalInstructions / totalMethods : 0):N0}");
        Console.WriteLine($"Total IL instructions: {totalInstructions:N0} bytes");
    }

    private static IEnumerable<CountryEntity> GenerateTestData()
    {
        for (int i = 0; i < 100; i++)
        {
            yield return new CountryEntity($"City{i}", $"Country{i}", 1000000 + i * 10000);
        }
    }

    private static string GenerateLongSelectQuery()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("SELECT");
        
        // Generate many complex CASE statements that will result in complex generated code
        for (int i = 0; i < 200; i++)
        {
            sb.AppendLine($"    CASE");
            sb.AppendLine($"        WHEN Country = 'A{i}' THEN ");
            sb.AppendLine($"            CASE");
            sb.AppendLine($"                WHEN Population > {i} THEN 'High{i}'");
            sb.AppendLine($"                WHEN Population > {i / 2} THEN 'Medium{i}'");
            sb.AppendLine($"                ELSE 'Low{i}'");
            sb.AppendLine($"            END");
            sb.AppendLine($"        WHEN Country = 'B{i}' THEN ");
            sb.AppendLine($"            COALESCE(Country, 'Default{i}') + '_' + CAST(Population AS VARCHAR)");
            sb.AppendLine($"        WHEN Country LIKE 'Pattern{i}%' THEN ");
            sb.AppendLine($"            SUBSTRING(Country, 1, {i % 10 + 1}) + '_Truncated'");
            sb.AppendLine($"        WHEN Country IN ('List{i}_1', 'List{i}_2', 'List{i}_3', 'List{i}_4', 'List{i}_5') THEN ");
            sb.AppendLine($"            'InList{i}_' + CAST(LEN(Country) AS VARCHAR)");
            sb.AppendLine($"        WHEN Country IS NULL THEN ");
            sb.AppendLine($"            'Null{i}_' + CAST({i} AS VARCHAR)");
            sb.AppendLine($"        ELSE ");
            sb.AppendLine($"            CONCAT('Complex{i}_', Country, '_', Population, '_', {i})");
            sb.AppendLine($"    END AS ComplexField{i}{(i < 199 ? "," : "")}");
        }
        
        sb.AppendLine("FROM #test.Entities()");
        sb.AppendLine("WHERE Population > 0 AND Country IS NOT NULL");
        sb.AppendLine("GROUP BY City, Country");
        sb.AppendLine("HAVING COUNT(*) > 0 AND SUM(Population) > 100");
        sb.AppendLine("ORDER BY City, ComplexField1");
        
        return sb.ToString();
    }
}