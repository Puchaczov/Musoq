using BenchmarkDotNet.Attributes;
using Musoq.Parser.Lexing;
using System.Diagnostics;
using System.Text;

namespace Musoq.Benchmarks.Components;

[MemoryDiagnoser]
[SimpleJob]
public class ParsingPerformanceBenchmark
{
    private string _longSelectQuery = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _longSelectQuery = GenerateLongSelectQuery();
        
        // Debug output for profiling
        Console.WriteLine($"Generated query with {_longSelectQuery.Length:N0} characters");
        Console.WriteLine($"Query contains {_longSelectQuery.Split('\n').Length:N0} lines");
    }

    [Benchmark]
    public void ParseLongSelectQuery()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var lexer = new Lexer(_longSelectQuery, true);
            var parser = new Musoq.Parser.Parser(lexer);
            var tree = parser.ComposeAll();
            stopwatch.Stop();
            
            // Debug output for profiling generated code efficiency
            Console.WriteLine($"Parsing completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"AST nodes created: {CountAstNodes(tree):N0}");
            Console.WriteLine($"Memory usage: {GC.GetTotalMemory(false):N0} bytes");
            
            // Force GC to measure allocation impact
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
            Console.WriteLine($"Parsing failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
            throw;
        }
    }

    private static string GenerateLongSelectQuery()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("SELECT");
        
        // Generate 1000+ lines of complex CASE statements for parsing performance testing
        for (int i = 0; i < 500; i++)
        {
            sb.AppendLine($"    CASE");
            sb.AppendLine($"        WHEN Field{i} = 'Value{i}_A' THEN 'Result{i}_1'");
            sb.AppendLine($"        WHEN Field{i} = 'Value{i}_B' THEN 'Result{i}_2'");
            sb.AppendLine($"        WHEN Field{i} = 'Value{i}_C' THEN 'Result{i}_3'");
            sb.AppendLine($"        WHEN Field{i} = 'Value{i}_D' THEN 'Result{i}_4'");
            sb.AppendLine($"        WHEN Field{i} = 'Value{i}_E' THEN 'Result{i}_5'");
            sb.AppendLine($"        WHEN Field{i} LIKE 'Pattern{i}%' THEN CONCAT('Dynamic{i}_', Field{i})");
            sb.AppendLine($"        WHEN Field{i} IN ('List{i}_1', 'List{i}_2', 'List{i}_3') THEN 'InList{i}'");
            sb.AppendLine($"        WHEN Field{i} IS NULL THEN 'Null{i}'");
            sb.AppendLine($"        ELSE CONCAT('Default{i}_', COALESCE(Field{i}, 'Unknown'))");
            sb.AppendLine($"    END AS ProcessedField{i}{(i < 499 ? "," : "")}");
        }
        
        sb.AppendLine("FROM #test.data()");
        sb.AppendLine("WHERE Id > 0");
        sb.AppendLine("GROUP BY Id");
        sb.AppendLine("HAVING COUNT(*) > 1");
        sb.AppendLine("ORDER BY Id");
        
        return sb.ToString();
    }
    
    private static int CountAstNodes(object node)
    {
        if (node == null) return 0;
        
        int count = 1;
        var properties = node.GetType().GetProperties();
        
        foreach (var prop in properties)
        {
            var value = prop.GetValue(node);
            if (value == null) continue;
            
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                foreach (var item in enumerable)
                {
                    count += CountAstNodes(item);
                }
            }
            else if (prop.PropertyType.Namespace?.StartsWith("Musoq") == true)
            {
                count += CountAstNodes(value);
            }
        }
        
        return count;
    }
}