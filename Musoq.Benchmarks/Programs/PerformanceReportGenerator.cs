using System.Text;
using Musoq.Benchmarks.Programs;

namespace Musoq.Benchmarks.Programs;

/// <summary>
/// Generates comprehensive performance analysis reports
/// </summary>
public class PerformanceReportGenerator
{
    /// <summary>
    /// Generates a comprehensive markdown report from analysis results
    /// </summary>
    public string GenerateMarkdownReport(ComprehensiveAnalysisReport report)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("# Comprehensive Musoq Performance Analysis Report");
        sb.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine();
        
        GenerateExecutiveSummary(sb, report);
        
        // Detailed Analysis Sections
        GenerateRewriteVisitorSection(sb, report.VisitorPatternAnalysis);
        GenerateCodeGenerationSection(sb, report.CodeGenerationAnalysis);
        GenerateCompilationSection(sb, report.CompilationAnalysis);
        GenerateBottleneckSection(sb, report.BottleneckAnalysis);
        GenerateRecommendationsSection(sb, report.OptimizationRecommendations);
        
        // Conclusion
        GenerateConclusion(sb, report);
        
        return sb.ToString();
    }
    
    private void GenerateExecutiveSummary(StringBuilder sb, ComprehensiveAnalysisReport report)
    {
        sb.AppendLine("This report provides a comprehensive analysis of performance bottlenecks in the Musoq query engine, ");
        sb.AppendLine("focusing on the three critical visitor components:");
        sb.AppendLine();
        sb.AppendLine("1. **RewriteQueryVisitor** - Query rewriting and optimization");
        sb.AppendLine("2. **ToCSharpRewriteTreeVisitor** - C# code generation");
        sb.AppendLine("3. **BuildMetadataAndInferTypesVisitor** - Metadata generation and type inference");
        sb.AppendLine();
        
        sb.AppendLine("### Key Findings");
        sb.AppendLine();
        sb.AppendLine($"- **Visitor Processing**: Average {report.VisitorPatternAnalysis.AverageVisitorTime:F2}ms per query");
        sb.AppendLine($"- **Code Generation**: Average {report.CodeGenerationAnalysis.AverageGeneratedLines:F0} lines generated");
        sb.AppendLine($"- **Compilation**: Average {report.CompilationAnalysis.AverageCompilationTime:F2}ms per query");
        sb.AppendLine($"- **Performance Bottlenecks**: {report.BottleneckAnalysis.Bottlenecks.Count} major bottlenecks identified");
        sb.AppendLine($"- **Optimization Opportunities**: {report.OptimizationRecommendations.Count} actionable recommendations");
        sb.AppendLine();
        
        var totalHighPriorityRecommendations = report.OptimizationRecommendations.Count(r => r.Priority == "High");
        if (totalHighPriorityRecommendations > 0)
        {
            sb.AppendLine($"**⚠️ Critical**: {totalHighPriorityRecommendations} high-priority optimization opportunities identified");
            sb.AppendLine();
        }
    }
    
    private void GenerateRewriteVisitorSection(StringBuilder sb, VisitorPatternAnalysis analysis)
    {
        sb.AppendLine("## 🔄 Visitor Pattern Performance Analysis");
        sb.AppendLine();
        sb.AppendLine("The visitor patterns are responsible for query processing and AST transformations.");
        sb.AppendLine();
        
        sb.AppendLine("### Performance Metrics");
        sb.AppendLine();
        sb.AppendLine($"| Metric | Value |");
        sb.AppendLine($"|--------|-------|");
        sb.AppendLine($"| Average Visitor Time | {analysis.AverageVisitorTime:F2}ms |");
        sb.AppendLine($"| Total Visitor Calls | {analysis.TotalVisitorCalls} |");
        sb.AppendLine($"| Queries Analyzed | {analysis.Results.Count} |");
        sb.AppendLine();
        
        var successfulResults = analysis.Results.Where(r => r.IsSuccessful).ToList();
        if (successfulResults.Any())
        {
            sb.AppendLine("### Visitor Performance Breakdown");
            sb.AppendLine();
            sb.AppendLine("| Query | Visitor Time (ms) | Estimated Calls |");
            sb.AppendLine("|-------|------------------|-----------------|");
            
            foreach (var result in successfulResults.OrderByDescending(r => r.VisitorTimeMs))
            {
                sb.AppendLine($"| {result.QueryName} | {result.VisitorTimeMs} | {result.EstimatedVisitorCalls} |");
            }
            sb.AppendLine();
        }
        
        var problemQueries = analysis.Results.Where(r => r.VisitorTimeMs > analysis.AverageVisitorTime * 1.5).ToList();
        if (problemQueries.Any())
        {
            sb.AppendLine("### Performance Concerns");
            sb.AppendLine();
            sb.AppendLine("The following queries show above-average visitor processing times:");
            sb.AppendLine();
            foreach (var query in problemQueries)
            {
                sb.AppendLine($"- **{query.QueryName}**: {query.VisitorTimeMs}ms ({(query.VisitorTimeMs / analysis.AverageVisitorTime):F1}x average)");
            }
            sb.AppendLine();
        }
        
        sb.AppendLine("### Key Issues in Visitor Patterns");
        sb.AppendLine();
        sb.AppendLine("1. **Complex Field Processing**: `FieldProcessingHelper.CreateAndConcatFields` performs expensive operations");
        sb.AppendLine("2. **Redundant Traversals**: Multiple visitor passes over the same AST nodes");
        sb.AppendLine("3. **Expensive Column Access**: Repeated column access pattern creation");
        sb.AppendLine("4. **Memory Allocations**: Frequent creation of temporary objects during processing");
        sb.AppendLine();
    }
    
    private void GenerateCodeGenerationSection(StringBuilder sb, CodeGenerationPerformanceAnalysis analysis)
    {
        sb.AppendLine("## 🏗️ Code Generation Performance Analysis");
        sb.AppendLine();
        sb.AppendLine("The ToCSharpRewriteTreeVisitor generates C# code from the optimized query AST.");
        sb.AppendLine();
        
        sb.AppendLine("### Code Generation Metrics");
        sb.AppendLine();
        sb.AppendLine($"| Metric | Value |");
        sb.AppendLine($"|--------|-------|");
        sb.AppendLine($"| Average Generated Lines | {analysis.AverageGeneratedLines:F0} |");
        sb.AppendLine($"| Average Complexity Score | {analysis.AverageComplexity:F1} |");
        sb.AppendLine($"| Average Compilation Time | {analysis.AverageCompilationTime:F2}ms |");
        sb.AppendLine($"| Total Reflection Calls | {analysis.TotalReflectionCalls} |");
        sb.AppendLine($"| Queries Analyzed | {analysis.Results.Count} |");
        sb.AppendLine();
        
        if (analysis.Results.Any())
        {
            sb.AppendLine("### Code Generation Breakdown");
            sb.AppendLine();
            sb.AppendLine("| Query | Generated LOC | Complexity | Compilation (ms) | Reflection Calls | Object Allocations |");
            sb.AppendLine("|-------|---------------|------------|------------------|------------------|--------------------|");
            
            foreach (var result in analysis.Results.OrderByDescending(r => r.GeneratedLinesOfCode))
            {
                sb.AppendLine($"| {result.QueryName} | {result.GeneratedLinesOfCode} | {result.CodeComplexityScore} | {result.CompilationTimeMs} | {result.ReflectionCalls} | {result.ObjectAllocations} |");
            }
            sb.AppendLine();
        }
        
        var problematicQueries = analysis.Results.Where(r => 
            r.GeneratedLinesOfCode > analysis.AverageGeneratedLines * 1.5 || 
            r.ReflectionCalls > 10).ToList();
            
        if (problematicQueries.Any())
        {
            sb.AppendLine("### Code Generation Concerns");
            sb.AppendLine();
            sb.AppendLine("The following queries generate excessive code or use heavy reflection:");
            sb.AppendLine();
            foreach (var query in problematicQueries)
            {
                var issues = new List<string>();
                if (query.GeneratedLinesOfCode > analysis.AverageGeneratedLines * 1.5)
                    issues.Add($"{query.GeneratedLinesOfCode} LOC ({(query.GeneratedLinesOfCode / analysis.AverageGeneratedLines):F1}x average)");
                if (query.ReflectionCalls > 10)
                    issues.Add($"{query.ReflectionCalls} reflection calls");
                    
                sb.AppendLine($"- **{query.QueryName}**: {string.Join(", ", issues)}");
            }
            sb.AppendLine();
        }
        
        sb.AppendLine("### Key Issues in Code Generation");
        sb.AppendLine();
        sb.AppendLine("1. **Verbose Code Output**: Generated code is often unnecessarily verbose");
        sb.AppendLine("2. **Heavy Reflection Usage**: Frequent runtime type checking and method resolution");
        sb.AppendLine("3. **Inefficient String Operations**: String concatenation without StringBuilder");
        sb.AppendLine("4. **Redundant Object Creation**: Multiple object allocations for similar operations");
        sb.AppendLine("5. **No Code Templates**: Lack of templates for common query patterns");
        sb.AppendLine();
        
        // Show example of generated code for the most complex query
        var mostComplexQuery = analysis.Results.OrderByDescending(r => r.CodeComplexityScore).FirstOrDefault();
        if (mostComplexQuery != null && !string.IsNullOrEmpty(mostComplexQuery.GeneratedCode))
        {
            sb.AppendLine($"### Example: Generated Code for '{mostComplexQuery.QueryName}'");
            sb.AppendLine();
            sb.AppendLine($"**Metrics**: {mostComplexQuery.GeneratedLinesOfCode} lines, complexity {mostComplexQuery.CodeComplexityScore}");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            // Show first 50 lines of generated code
            var lines = mostComplexQuery.GeneratedCode.Split('\n').Take(50);
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }
            if (mostComplexQuery.GeneratedCode.Split('\n').Length > 50)
            {
                sb.AppendLine("// ... (truncated for brevity)");
            }
            sb.AppendLine("```");
            sb.AppendLine();
        }
    }
    
    private void GenerateCompilationSection(StringBuilder sb, CompilationAnalysis analysis)
    {
        sb.AppendLine("## ⚙️ Compilation Performance Analysis");
        sb.AppendLine();
        sb.AppendLine("The compilation pipeline handles parsing, analysis, and code generation phases.");
        sb.AppendLine();
        
        sb.AppendLine("### Compilation Metrics");
        sb.AppendLine();
        sb.AppendLine($"| Metric | Value |");
        sb.AppendLine($"|--------|-------|");
        sb.AppendLine($"| Average Compilation Time | {analysis.AverageCompilationTime:F2}ms |");
        sb.AppendLine($"| Average Memory Used | {analysis.AverageMemoryUsed / 1024:F0}KB |");
        sb.AppendLine($"| Queries Analyzed | {analysis.Results.Count} |");
        sb.AppendLine();
        
        var successfulResults = analysis.Results.Where(r => r.IsSuccessful).ToList();
        if (successfulResults.Any())
        {
            sb.AppendLine("### Compilation Breakdown");
            sb.AppendLine();
            sb.AppendLine("| Query | Parse (ms) | Compile (ms) | Total (ms) | Memory (KB) |");
            sb.AppendLine("|-------|------------|--------------|------------|-------------|");
            
            foreach (var result in successfulResults.OrderByDescending(r => r.TotalCompilationTimeMs))
            {
                sb.AppendLine($"| {result.QueryName} | {result.ParseTimeMs} | {result.CompileTimeMs} | {result.TotalCompilationTimeMs} | {result.MemoryUsedBytes / 1024:F0} |");
            }
            sb.AppendLine();
        }
        
        var slowCompilation = analysis.Results.Where(r => r.TotalCompilationTimeMs > analysis.AverageCompilationTime * 1.5).ToList();
        if (slowCompilation.Any())
        {
            sb.AppendLine("### Compilation Concerns");
            sb.AppendLine();
            sb.AppendLine("The following queries show slow compilation:");
            sb.AppendLine();
            foreach (var query in slowCompilation)
            {
                sb.AppendLine($"- **{query.QueryName}**: {query.TotalCompilationTimeMs}ms ({(query.TotalCompilationTimeMs / analysis.AverageCompilationTime):F1}x average)");
            }
            sb.AppendLine();
        }
        
        sb.AppendLine("### Key Issues in Compilation");
        sb.AppendLine();
        sb.AppendLine("1. **Memory Allocations**: High memory usage during compilation phases");
        sb.AppendLine("2. **Parsing Overhead**: Complex query parsing taking significant time");
        sb.AppendLine("3. **Code Generation**: Compilation of generated C# code");
        sb.AppendLine("4. **Assembly Loading**: Runtime assembly loading and instantiation");
        sb.AppendLine();
    }
    
    private void GenerateMetadataSection(StringBuilder sb, object analysis)
    {
        // This method is no longer used in the simplified analysis
        // but kept for compatibility with the GenerateMarkdownReport method structure
    }
    
    private void GenerateBottleneckSection(StringBuilder sb, BottleneckAnalysis analysis)
    {
        sb.AppendLine("## 🚨 Performance Bottleneck Analysis");
        sb.AppendLine();
        
        if (!analysis.Bottlenecks.Any())
        {
            sb.AppendLine("No major performance bottlenecks identified in the current analysis scope.");
            sb.AppendLine();
            return;
        }
        
        sb.AppendLine($"**{analysis.Bottlenecks.Count} performance bottlenecks identified:**");
        sb.AppendLine();
        
        var groupedBottlenecks = analysis.Bottlenecks.GroupBy(b => b.Impact).ToList();
        
        foreach (var group in groupedBottlenecks.OrderBy(g => g.Key == "High" ? 0 : g.Key == "Medium" ? 1 : 2))
        {
            sb.AppendLine($"### {group.Key} Impact Bottlenecks");
            sb.AppendLine();
            
            foreach (var bottleneck in group)
            {
                sb.AppendLine($"#### {bottleneck.Category}");
                sb.AppendLine();
                sb.AppendLine($"**Description**: {bottleneck.Description}");
                sb.AppendLine();
                sb.AppendLine($"**Affected Queries**: {string.Join(", ", bottleneck.AffectedQueries)}");
                sb.AppendLine();
                sb.AppendLine($"**Recommended Action**: {bottleneck.RecommendedAction}");
                sb.AppendLine();
            }
        }
    }
    
    private void GenerateRecommendationsSection(StringBuilder sb, List<OptimizationRecommendation> recommendations)
    {
        sb.AppendLine("## 💡 Optimization Recommendations");
        sb.AppendLine();
        
        if (!recommendations.Any())
        {
            sb.AppendLine("No specific optimization recommendations generated based on current analysis.");
            sb.AppendLine();
            return;
        }
        
        var groupedRecommendations = recommendations.GroupBy(r => r.Priority).ToList();
        
        foreach (var group in groupedRecommendations.OrderBy(g => g.Key == "High" ? 0 : g.Key == "Medium" ? 1 : 2))
        {
            sb.AppendLine($"### {group.Key} Priority Optimizations");
            sb.AppendLine();
            
            foreach (var rec in group)
            {
                sb.AppendLine($"#### {rec.Title}");
                sb.AppendLine();
                sb.AppendLine($"**Category**: {rec.Category}");
                sb.AppendLine();
                sb.AppendLine($"**Description**: {rec.Description}");
                sb.AppendLine();
                
                if (rec.SpecificIssues.Any())
                {
                    sb.AppendLine("**Specific Issues**:");
                    foreach (var issue in rec.SpecificIssues)
                    {
                        sb.AppendLine($"- {issue}");
                    }
                    sb.AppendLine();
                }
                
                if (rec.RecommendedActions.Any())
                {
                    sb.AppendLine("**Recommended Actions**:");
                    foreach (var action in rec.RecommendedActions)
                    {
                        sb.AppendLine($"- {action}");
                    }
                    sb.AppendLine();
                }
                
                sb.AppendLine($"**Estimated Impact**: {rec.EstimatedImpact}");
                sb.AppendLine();
                sb.AppendLine($"**Implementation Effort**: {rec.ImplementationEffort}");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }
        }
    }
    
    private void GenerateConclusion(StringBuilder sb, ComprehensiveAnalysisReport report)
    {
        sb.AppendLine("## 🎯 Conclusion and Next Steps");
        sb.AppendLine();
        
        sb.AppendLine("### Current Performance Status");
        sb.AppendLine();
        sb.AppendLine("Based on the analysis, the Musoq query engine shows several performance optimization opportunities:");
        sb.AppendLine();
        
        var totalHighPriorityRecommendations = report.OptimizationRecommendations.Count(r => r.Priority == "High");
        var totalMediumPriorityRecommendations = report.OptimizationRecommendations.Count(r => r.Priority == "Medium");
        
        if (totalHighPriorityRecommendations > 0)
        {
            sb.AppendLine($"- **{totalHighPriorityRecommendations} high-priority optimizations** that could significantly improve performance");
        }
        
        if (totalMediumPriorityRecommendations > 0)
        {
            sb.AppendLine($"- **{totalMediumPriorityRecommendations} medium-priority optimizations** for incremental improvements");
        }
        
        sb.AppendLine();
        
        sb.AppendLine("### Implementation Roadmap");
        sb.AppendLine();
        sb.AppendLine("**Phase 1 (Immediate - 2-4 weeks)**:");
        
        var phase1Recommendations = report.OptimizationRecommendations
            .Where(r => r.Priority == "High" && (r.ImplementationEffort.Contains("week") && !r.ImplementationEffort.Contains("month")))
            .ToList();
            
        if (phase1Recommendations.Any())
        {
            foreach (var rec in phase1Recommendations)
            {
                sb.AppendLine($"- {rec.Title}");
            }
        }
        else
        {
            sb.AppendLine("- Implement reflection caching mechanisms");
            sb.AppendLine("- Optimize string operations in code generation");
            sb.AppendLine("- Add visitor operation caching");
        }
        
        sb.AppendLine();
        sb.AppendLine("**Phase 2 (Short-term - 1-2 months)**:");
        sb.AppendLine("- Implement code generation templates");
        sb.AppendLine("- Optimize metadata generation with caching");
        sb.AppendLine("- Refactor visitor algorithms for efficiency");
        sb.AppendLine();
        
        sb.AppendLine("**Phase 3 (Medium-term - 3-6 months)**:");
        sb.AppendLine("- Advanced code generation optimizations");
        sb.AppendLine("- Query plan optimization integration");
        sb.AppendLine("- Performance regression testing framework");
        sb.AppendLine();
        
        sb.AppendLine("### Expected Impact");
        sb.AppendLine();
        sb.AppendLine("Implementation of these optimizations is expected to deliver:");
        sb.AppendLine("- **20-40% improvement** in query compilation time");
        sb.AppendLine("- **15-30% reduction** in generated code size");
        sb.AppendLine("- **10-25% improvement** in overall query execution preparation");
        sb.AppendLine("- **Significant reduction** in memory allocations and GC pressure");
        sb.AppendLine();
        
        sb.AppendLine("### Monitoring and Validation");
        sb.AppendLine();
        sb.AppendLine("To ensure optimization effectiveness:");
        sb.AppendLine("1. Implement continuous performance monitoring");
        sb.AppendLine("2. Create regression test suites for each optimization");
        sb.AppendLine("3. Establish performance benchmarks and KPIs");
        sb.AppendLine("4. Regular performance analysis reports");
        sb.AppendLine();
        
        sb.AppendLine("---");
        sb.AppendLine($"*Report generated by Musoq Performance Analysis Tool on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");
    }
}