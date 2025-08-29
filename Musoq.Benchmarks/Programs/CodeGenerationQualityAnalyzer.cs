using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Benchmarks.Programs;

/// <summary>
/// Analyzes the quality and efficiency of generated C# code from Musoq queries
/// </summary>
public class CodeGenerationQualityAnalyzer
{
    /// <summary>
    /// Analyzes the quality of generated C# code and returns detailed metrics
    /// </summary>
    public CodeQualityReport AnalyzeGeneratedCode(string generatedCode, string originalQuery)
    {
        if (string.IsNullOrWhiteSpace(generatedCode))
            throw new ArgumentException("Generated code cannot be null or empty", nameof(generatedCode));
        
        var syntaxTree = CSharpSyntaxTree.ParseText(generatedCode);
        var root = syntaxTree.GetRoot();
        
        return new CodeQualityReport
        {
            OriginalQuery = originalQuery,
            GeneratedCode = generatedCode,
            LinesOfCode = CountLines(generatedCode),
            NonEmptyLinesOfCode = CountNonEmptyLines(generatedCode),
            ReflectionCallCount = CountReflectionCalls(generatedCode),
            ObjectAllocationCount = CountObjectAllocations(root),
            MethodCount = CountMethods(root),
            CyclomaticComplexity = CalculateCyclomaticComplexity(root),
            CodePatterns = AnalyzeCodePatterns(root),
            OptimizationOpportunities = IdentifyOptimizationOpportunities(generatedCode, root),
            CodeEfficiencyScore = 0 // Will be calculated after other metrics
        };
    }
    
    private int CountLines(string code)
    {
        return code.Split('\n').Length;
    }
    
    private int CountNonEmptyLines(string code)
    {
        return code.Split('\n').Count(line => !string.IsNullOrWhiteSpace(line));
    }
    
    private int CountReflectionCalls(string code)
    {
        var reflectionPatterns = new[]
        {
            @"typeof\s*\(",
            @"\.GetType\s*\(",
            @"\.GetMethod\s*\(",
            @"\.GetProperty\s*\(",
            @"\.GetField\s*\(",
            @"Activator\.CreateInstance",
            @"Assembly\..*",
            @"MethodInfo",
            @"PropertyInfo",
            @"FieldInfo",
            @"Type\..*"
        };
        
        return reflectionPatterns.Sum(pattern => Regex.Matches(code, pattern).Count);
    }
    
    private int CountObjectAllocations(SyntaxNode root)
    {
        var allocations = 0;
        
        // Count object creation expressions
        allocations += root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Count();
        
        // Count array creation expressions
        allocations += root.DescendantNodes().OfType<ArrayCreationExpressionSyntax>().Count();
        
        // Count implicit array creation expressions
        allocations += root.DescendantNodes().OfType<ImplicitArrayCreationExpressionSyntax>().Count();
        
        // Count anonymous object creation expressions
        allocations += root.DescendantNodes().OfType<AnonymousObjectCreationExpressionSyntax>().Count();
        
        return allocations;
    }
    
    private int CountMethods(SyntaxNode root)
    {
        return root.DescendantNodes().OfType<MethodDeclarationSyntax>().Count();
    }
    
    private int CalculateCyclomaticComplexity(SyntaxNode root)
    {
        var complexity = 1; // Base complexity
        
        // Add complexity for decision points
        complexity += root.DescendantNodes().OfType<IfStatementSyntax>().Count();
        complexity += root.DescendantNodes().OfType<WhileStatementSyntax>().Count();
        complexity += root.DescendantNodes().OfType<ForStatementSyntax>().Count();
        complexity += root.DescendantNodes().OfType<ForEachStatementSyntax>().Count();
        complexity += root.DescendantNodes().OfType<SwitchStatementSyntax>().Count();
        complexity += root.DescendantNodes().OfType<ConditionalExpressionSyntax>().Count();
        
        // Add complexity for case labels in switch statements
        complexity += root.DescendantNodes().OfType<CaseSwitchLabelSyntax>().Count();
        
        // Add complexity for logical operators in conditions
        var logicalOperators = root.DescendantNodes().OfType<BinaryExpressionSyntax>()
            .Where(be => be.IsKind(SyntaxKind.LogicalAndExpression) || be.IsKind(SyntaxKind.LogicalOrExpression));
        complexity += logicalOperators.Count();
        
        return complexity;
    }
    
    private CodePatternAnalysis AnalyzeCodePatterns(SyntaxNode root)
    {
        return new CodePatternAnalysis
        {
            LoopCount = CountLoops(root),
            ConditionalCount = CountConditionals(root),
            LambdaExpressionCount = CountLambdaExpressions(root),
            LinqOperationCount = CountLinqOperations(root),
            StringOperationCount = CountStringOperations(root),
            CastOperationCount = CountCastOperations(root)
        };
    }
    
    private int CountLoops(SyntaxNode root)
    {
        return root.DescendantNodes().OfType<ForStatementSyntax>().Count() +
               root.DescendantNodes().OfType<ForEachStatementSyntax>().Count() +
               root.DescendantNodes().OfType<WhileStatementSyntax>().Count() +
               root.DescendantNodes().OfType<DoStatementSyntax>().Count();
    }
    
    private int CountConditionals(SyntaxNode root)
    {
        return root.DescendantNodes().OfType<IfStatementSyntax>().Count() +
               root.DescendantNodes().OfType<SwitchStatementSyntax>().Count() +
               root.DescendantNodes().OfType<ConditionalExpressionSyntax>().Count();
    }
    
    private int CountLambdaExpressions(SyntaxNode root)
    {
        return root.DescendantNodes().OfType<LambdaExpressionSyntax>().Count();
    }
    
    private int CountLinqOperations(SyntaxNode root)
    {
        var linqMethods = new[] { "Select", "Where", "GroupBy", "OrderBy", "OrderByDescending", "Join", "Aggregate", "Sum", "Count", "Average", "Min", "Max" };
        
        return root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Count(invocation =>
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    return linqMethods.Contains(memberAccess.Name.Identifier.ValueText);
                }
                return false;
            });
    }
    
    private int CountStringOperations(SyntaxNode root)
    {
        var stringMethods = new[] { "ToString", "Substring", "Replace", "Split", "Join", "Concat", "Format" };
        
        return root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Count(invocation =>
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    return stringMethods.Contains(memberAccess.Name.Identifier.ValueText);
                }
                return false;
            });
    }
    
    private int CountCastOperations(SyntaxNode root)
    {
        return root.DescendantNodes().OfType<CastExpressionSyntax>().Count();
        // AsExpressionSyntax is not available in the current context
        // + root.DescendantNodes().OfType<AsExpressionSyntax>().Count();
    }
    
    private List<OptimizationOpportunity> IdentifyOptimizationOpportunities(string code, SyntaxNode root)
    {
        var opportunities = new List<OptimizationOpportunity>();
        
        // Check for excessive reflection usage
        var reflectionCount = CountReflectionCalls(code);
        if (reflectionCount > 5)
        {
            opportunities.Add(new OptimizationOpportunity
            {
                Type = OptimizationType.ReduceReflection,
                Description = $"High reflection usage detected ({reflectionCount} calls). Consider compile-time type resolution.",
                Impact = OptimizationImpact.High,
                EstimatedImprovement = "30-50% reflection overhead reduction"
            });
        }
        
        // Check for excessive object allocations
        var allocationCount = CountObjectAllocations(root);
        if (allocationCount > 15)
        {
            opportunities.Add(new OptimizationOpportunity
            {
                Type = OptimizationType.ReduceAllocations,
                Description = $"High object allocation count ({allocationCount}). Consider object pooling or reuse patterns.",
                Impact = OptimizationImpact.Medium,
                EstimatedImprovement = "20-30% memory allocation reduction"
            });
        }
        
        // Check for excessive type casting
        var castCount = CountCastOperations(root);
        if (castCount > 8)
        {
            opportunities.Add(new OptimizationOpportunity
            {
                Type = OptimizationType.ReduceCasting,
                Description = $"High cast operation count ({castCount}). Consider generic type constraints or compile-time type resolution.",
                Impact = OptimizationImpact.Medium,
                EstimatedImprovement = "15-25% type operation overhead reduction"
            });
        }
        
        // Check for high cyclomatic complexity
        var complexity = CalculateCyclomaticComplexity(root);
        if (complexity > 20)
        {
            opportunities.Add(new OptimizationOpportunity
            {
                Type = OptimizationType.ReduceComplexity,
                Description = $"High cyclomatic complexity ({complexity}). Consider breaking into smaller methods or simplifying logic.",
                Impact = OptimizationImpact.Medium,
                EstimatedImprovement = "10-20% code maintainability improvement"
            });
        }
        
        // Check for template optimization opportunities
        var lineCount = CountNonEmptyLines(code);
        if (lineCount > 100)
        {
            opportunities.Add(new OptimizationOpportunity
            {
                Type = OptimizationType.UseTemplates,
                Description = $"Large generated code ({lineCount} lines). Consider template-based code generation.",
                Impact = OptimizationImpact.High,
                EstimatedImprovement = "30-50% code size reduction"
            });
        }
        
        return opportunities;
    }
}

public class CodeQualityReport
{
    public string OriginalQuery { get; set; } = string.Empty;
    public string GeneratedCode { get; set; } = string.Empty;
    public int LinesOfCode { get; set; }
    public int NonEmptyLinesOfCode { get; set; }
    public int ReflectionCallCount { get; set; }
    public int ObjectAllocationCount { get; set; }
    public int MethodCount { get; set; }
    public int CyclomaticComplexity { get; set; }
    public CodePatternAnalysis CodePatterns { get; set; } = new();
    public List<OptimizationOpportunity> OptimizationOpportunities { get; set; } = new();
    public double CodeEfficiencyScore { get; set; }
    
    public void CalculateEfficiencyScore()
    {
        // Calculate efficiency score based on various metrics
        // Lower reflection calls, allocations, and complexity = higher score
        var baseScore = 100.0;
        
        // Penalize excessive reflection (each call above 3 reduces score)
        if (ReflectionCallCount > 3)
            baseScore -= (ReflectionCallCount - 3) * 2.0;
        
        // Penalize excessive allocations (each allocation above 10 reduces score)
        if (ObjectAllocationCount > 10)
            baseScore -= (ObjectAllocationCount - 10) * 1.5;
        
        // Penalize high complexity (each point above 10 reduces score)
        if (CyclomaticComplexity > 10)
            baseScore -= (CyclomaticComplexity - 10) * 1.0;
        
        // Penalize verbose code (each line above 50 reduces score slightly)
        if (NonEmptyLinesOfCode > 50)
            baseScore -= (NonEmptyLinesOfCode - 50) * 0.2;
        
        CodeEfficiencyScore = Math.Max(0, baseScore);
    }
}

public class CodePatternAnalysis
{
    public int LoopCount { get; set; }
    public int ConditionalCount { get; set; }
    public int LambdaExpressionCount { get; set; }
    public int LinqOperationCount { get; set; }
    public int StringOperationCount { get; set; }
    public int CastOperationCount { get; set; }
}

public class OptimizationOpportunity
{
    public OptimizationType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public OptimizationImpact Impact { get; set; }
    public string EstimatedImprovement { get; set; } = string.Empty;
}

public enum OptimizationType
{
    ReduceReflection,
    ReduceAllocations,
    ReduceCasting,
    ReduceComplexity,
    UseTemplates,
    OptimizeLinq,
    CacheExpressions,
    UseObjectPooling
}

public enum OptimizationImpact
{
    Low,
    Medium,
    High,
    Critical
}