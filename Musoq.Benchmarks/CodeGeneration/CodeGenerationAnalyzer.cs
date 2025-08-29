using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Benchmarks.Components;

namespace Musoq.Benchmarks.CodeGeneration;

/// <summary>
/// Utility for analyzing generated C# code from Musoq queries
/// </summary>
public class CodeGenerationAnalyzer
{
    private readonly ISchemaProvider _schemaProvider;
    
    public CodeGenerationAnalyzer(ISchemaProvider schemaProvider)
    {
        _schemaProvider = schemaProvider;
    }
    
    /// <summary>
    /// Analyzes a query and returns detailed information about the generated code
    /// </summary>
    public QueryAnalysisResult AnalyzeQuery(string query, string name = "")
    {
        var assemblyName = $"AnalyzedQuery_{Guid.NewGuid().ToString("N")[..8]}";
        
        try
        {
            // Get build items with generated code
            var buildItems = InstanceCreator.CreateForAnalyze(query, assemblyName, _schemaProvider, new BenchmarkLoggerResolver());
            
            // Extract generated C# code
            var generatedCode = ExtractGeneratedCode(buildItems);
            
            // Compile for execution to measure performance
            var compiledQuery = InstanceCreator.CompileForExecution(query, assemblyName + "_exec", _schemaProvider, new BenchmarkLoggerResolver());
            
            // Analyze the generated code
            var codeMetrics = AnalyzeCodeMetrics(generatedCode);
            var performanceMetrics = MeasurePerformance(compiledQuery);
            
            return new QueryAnalysisResult
            {
                QueryName = name,
                OriginalQuery = query,
                GeneratedCode = generatedCode,
                CodeMetrics = codeMetrics,
                PerformanceMetrics = performanceMetrics,
                AssemblyName = assemblyName
            };
        }
        catch (Exception ex)
        {
            return new QueryAnalysisResult
            {
                QueryName = name,
                OriginalQuery = query,
                Error = ex.Message,
                AssemblyName = assemblyName
            };
        }
    }
    
    /// <summary>
    /// Extracts the generated C# code from BuildItems
    /// </summary>
    private static string ExtractGeneratedCode(BuildItems buildItems)
    {
        if (buildItems.Compilation?.SyntaxTrees == null || !buildItems.Compilation.SyntaxTrees.Any())
            return "No generated code available";
            
        var builder = new StringBuilder();
        using var writer = new StringWriter(builder);
        
        foreach (var syntaxTree in buildItems.Compilation.SyntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            root.WriteTo(writer);
        }
        
        return builder.ToString();
    }
    
    /// <summary>
    /// Analyzes code metrics from generated C# code
    /// </summary>
    private static CodeMetrics AnalyzeCodeMetrics(string generatedCode)
    {
        if (string.IsNullOrEmpty(generatedCode))
        {
            return new CodeMetrics
            {
                TotalLines = 0,
                Error = "No code to analyze"
            };
        }
        
        var syntaxTree = CSharpSyntaxTree.ParseText(generatedCode);
        var root = syntaxTree.GetRoot();
        
        var lines = generatedCode.Split('\n');
        var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).Count();
        
        // Count different code constructs
        var methods = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().Count();
        var loops = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForEachStatementSyntax>().Count() +
                   root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>().Count() +
                   root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.WhileStatementSyntax>().Count();
        var conditionals = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().Count();
        var lambdas = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LambdaExpressionSyntax>().Count();
        var linq = CountLinqOperations(generatedCode);
        var allocations = CountObjectAllocations(root);
        var stringOps = CountStringOperations(root);
        var reflectionCalls = CountReflectionCalls(generatedCode);
        
        return new CodeMetrics
        {
            TotalLines = lines.Length,
            NonEmptyLines = nonEmptyLines,
            Methods = methods,
            Loops = loops,
            Conditionals = conditionals,
            Lambdas = lambdas,
            LinqOperations = linq,
            ObjectAllocations = allocations,
            StringOperations = stringOps,
            ReflectionCalls = reflectionCalls,
            CodeComplexityScore = CalculateComplexityScore(methods, loops, conditionals, lambdas, linq)
        };
    }
    
    /// <summary>
    /// Measures basic performance metrics
    /// </summary>
    private static PerformanceMetrics MeasurePerformance(CompiledQuery compiledQuery)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(false);
        
        try
        {
            var results = compiledQuery.Run();
            var rowCount = results.Count();
            
            sw.Stop();
            var memoryAfter = GC.GetTotalMemory(false);
            
            return new PerformanceMetrics
            {
                ExecutionTimeMs = sw.ElapsedMilliseconds,
                MemoryUsedBytes = memoryAfter - memoryBefore,
                RowCount = rowCount
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new PerformanceMetrics
            {
                ExecutionTimeMs = sw.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }
    
    private static int CountLinqOperations(string code)
    {
        var linqMethods = new[] { ".Select(", ".Where(", ".GroupBy(", ".OrderBy(", ".Join(", ".Aggregate(", ".Sum(", ".Count(", ".Any(", ".All(", ".First(", ".Take(", ".Skip(" };
        return linqMethods.Sum(method => CountOccurrences(code, method));
    }
    
    private static int CountObjectAllocations(SyntaxNode root)
    {
        return root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ObjectCreationExpressionSyntax>().Count() +
               root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ArrayCreationExpressionSyntax>().Count() +
               root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AnonymousObjectCreationExpressionSyntax>().Count();
    }
    
    private static int CountStringOperations(SyntaxNode root)
    {
        var stringConcats = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax>()
            .Where(b => b.OperatorToken.IsKind(SyntaxKind.PlusToken))
            .Count();
            
        var stringInterpolations = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InterpolatedStringExpressionSyntax>()
            .Count();
            
        return stringConcats + stringInterpolations;
    }
    
    private static int CountReflectionCalls(string code)
    {
        var reflectionPatterns = new[] { "GetType(", "typeof(", ".GetMethod(", ".GetProperty(", ".GetField(", "Activator.CreateInstance", "Assembly.Load" };
        return reflectionPatterns.Sum(pattern => CountOccurrences(code, pattern));
    }
    
    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
    
    private static int CalculateComplexityScore(int methods, int loops, int conditionals, int lambdas, int linq)
    {
        // Simple complexity scoring algorithm
        return methods + (loops * 2) + conditionals + lambdas + linq;
    }
}

/// <summary>
/// Results of analyzing a query's generated code
/// </summary>
public class QueryAnalysisResult
{
    public string QueryName { get; set; } = "";
    public string OriginalQuery { get; set; } = "";
    public string GeneratedCode { get; set; } = "";
    public CodeMetrics CodeMetrics { get; set; } = new();
    public PerformanceMetrics PerformanceMetrics { get; set; } = new();
    public string AssemblyName { get; set; } = "";
    public string? Error { get; set; }
    
    public bool IsSuccessful => string.IsNullOrEmpty(Error);
}

/// <summary>
/// Metrics about the generated code structure
/// </summary>
public class CodeMetrics
{
    public int TotalLines { get; set; }
    public int NonEmptyLines { get; set; }
    public int Methods { get; set; }
    public int Loops { get; set; }
    public int Conditionals { get; set; }
    public int Lambdas { get; set; }
    public int LinqOperations { get; set; }
    public int ObjectAllocations { get; set; }
    public int StringOperations { get; set; }
    public int ReflectionCalls { get; set; }
    public int CodeComplexityScore { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Performance metrics from query execution
/// </summary>
public class PerformanceMetrics
{
    public long ExecutionTimeMs { get; set; }
    public long MemoryUsedBytes { get; set; }
    public int RowCount { get; set; }
    public string? Error { get; set; }
}