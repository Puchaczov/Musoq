using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Optimization;

namespace Musoq.Evaluator.Tests.Optimization;

[TestClass]
public class OptimizationManagerTests
{
    [TestMethod]
    public void TypeCacheManager_CachesTypesCorrectly()
    {
        // Arrange
        TypeCacheManager.ClearCaches();
        
        // Act
        var stringType1 = TypeCacheManager.GetCachedType("System.String");
        var stringType2 = TypeCacheManager.GetCachedType("System.String");
        
        // Assert
        Assert.AreEqual(stringType1, stringType2);
        
        var stats = TypeCacheManager.GetStatistics();
        Assert.AreEqual(1, stats.TypeCacheMisses); // First call was a miss
        Assert.AreEqual(1, stats.TypeCacheHits);   // Second call was a hit
        Assert.AreEqual(0.5, stats.TypeCacheHitRatio); // 50% hit ratio
    }

    [TestMethod]
    public void TypeCacheManager_CachesCastableTypeNames()
    {
        // Arrange
        TypeCacheManager.ClearCaches();
        
        // Act
        var castableName1 = TypeCacheManager.GetCachedCastableTypeName(typeof(string));
        var castableName2 = TypeCacheManager.GetCachedCastableTypeName(typeof(int));
        var castableName3 = TypeCacheManager.GetCachedCastableTypeName(typeof(string)); // Should be cached
        
        // Assert
        Assert.AreEqual("string", castableName1);
        Assert.AreEqual("int", castableName2);
        Assert.AreEqual("string", castableName3);
        
        var stats = TypeCacheManager.GetStatistics();
        Assert.AreEqual(2, stats.CastableTypeCacheSize); // Two distinct types cached
    }

    [TestMethod]
    public void OptimizationManager_AnalyzesSimpleQuery()
    {
        // Arrange
        var optimizationManager = new OptimizationManager();
        var input = new QueryAnalysisInput
        {
            QueryId = "test_query_1",
            Pattern = new QueryPattern
            {
                HasJoins = false,
                HasAggregations = false,
                HasComplexFiltering = false,
                ComplexityScore = 2,
                RequiredFields = new[] { "Name", "Age" },
                RequiredTypes = new[] { typeof(string), typeof(int) }
            },
            Context = new QueryAnalysisContext
            {
                HasFiltering = false,
                HasProjections = true,
                HasJoins = false,
                HasAggregations = false,
                ComplexityScore = 2
            }
        };

        // Act
        var plan = optimizationManager.AnalyzeQuery(input);

        // Assert
        Assert.IsNotNull(plan);
        Assert.AreEqual("test_query_1", plan.QueryId);
        Assert.IsTrue(plan.EnabledOptimizations.Contains(OptimizationType.ReflectionCaching));
        Assert.IsTrue(plan.EnabledOptimizations.Contains(OptimizationType.TemplateGeneration));
        Assert.AreEqual(OptimizationLevel.Intermediate, plan.OptimizationLevel);
        Assert.IsTrue(plan.EstimatedImprovement > 0);
    }

    [TestMethod]
    public void OptimizationManager_SelectsCorrectOptimizationsForComplexQuery()
    {
        // Arrange
        var optimizationManager = new OptimizationManager();
        var input = new QueryAnalysisInput
        {
            QueryId = "complex_query_1",
            Pattern = new QueryPattern
            {
                HasJoins = true,
                HasAggregations = true,
                HasComplexFiltering = true,
                ComplexityScore = 8,
                RequiredFields = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" }, // 10 fields
                RequiredTypes = new[] { typeof(string), typeof(int) }
            },
            Context = new QueryAnalysisContext
            {
                HasFiltering = true,
                HasProjections = true,
                HasJoins = true,
                HasAggregations = true,
                ComplexityScore = 8
            }
        };

        // Act
        var plan = optimizationManager.AnalyzeQuery(input);

        // Assert
        Assert.IsNotNull(plan);
        Assert.IsTrue(plan.EnabledOptimizations.Contains(OptimizationType.ReflectionCaching));
        Assert.IsTrue(plan.EnabledOptimizations.Contains(OptimizationType.StagedTransformation));
        Assert.IsTrue(plan.EnabledOptimizations.Contains(OptimizationType.ExpressionTreeCompilation));
        Assert.AreEqual(OptimizationLevel.Advanced, plan.OptimizationLevel);
        Assert.IsTrue(plan.EstimatedImprovement >= 0.6); // Should be high improvement for complex query
    }

    [TestMethod]
    public void CodeGenerationTemplates_GeneratesSimpleSelectTemplate()
    {
        // Arrange
        var className = "TestQuery";
        var sourceExpression = "dataSource";
        var fieldExpressions = new[] { "row[\"Name\"]", "row[\"Age\"]" };

        // Act
        var code = CodeGenerationTemplates.SimpleSelectTemplate(className, sourceExpression, fieldExpressions);

        // Assert
        Assert.IsTrue(code.Contains("public class TestQuery : IRunnable"));
        Assert.IsTrue(code.Contains("Table Run(CancellationToken token)"));
        Assert.IsTrue(code.Contains("row[\"Name\"]"));
        Assert.IsTrue(code.Contains("row[\"Age\"]"));
        Assert.IsTrue(code.Contains("new Table(\"QueryResult\", results)"));
    }

    [TestMethod]
    public void StagedTransformationManager_CreatesCorrectPlanForComplexQuery()
    {
        // Arrange
        var manager = new StagedTransformationManager();
        var context = new QueryAnalysisContext
        {
            HasFiltering = true,
            HasProjections = true,
            HasJoins = true,
            HasAggregations = true,
            ComplexityScore = 6
        };

        // Act
        var plan = manager.AnalyzeAndCreatePlan(context);

        // Assert
        Assert.IsNotNull(plan);
        Assert.IsTrue(plan.RequiresStaging);
        Assert.AreEqual(4, plan.Stages.Count); // Filter, Projection, Join, Aggregation
        Assert.IsTrue(plan.EstimatedPerformanceGain > 0);
        
        // Check stage types
        Assert.IsTrue(plan.Stages.Any(s => s.Type == StageType.Filter));
        Assert.IsTrue(plan.Stages.Any(s => s.Type == StageType.Projection));
        Assert.IsTrue(plan.Stages.Any(s => s.Type == StageType.Join));
        Assert.IsTrue(plan.Stages.Any(s => s.Type == StageType.Aggregation));
    }

    [TestMethod]
    public void FieldAccessTemplate_GeneratesOptimizedAccess()
    {
        // Act
        var stringAccess = CodeGenerationTemplates.FieldAccessTemplate("Name", typeof(string));
        var intAccess = CodeGenerationTemplates.FieldAccessTemplate("Age", typeof(int));
        var customAccess = CodeGenerationTemplates.FieldAccessTemplate("CustomField", typeof(DateTime));

        // Assert
        Assert.AreEqual("row[\"Name\"] as string", stringAccess);
        Assert.AreEqual("Convert.ToInt32(row[\"Age\"])", intAccess);
        Assert.AreEqual("Convert.ToDateTime(row[\"CustomField\"])", customAccess);
    }

    [TestMethod]
    public void OptimizationManager_GetStatistics()
    {
        // Arrange
        var optimizationManager = new OptimizationManager();
        TypeCacheManager.ClearCaches();

        // Perform some operations to generate statistics
        TypeCacheManager.GetCachedType("System.String");
        TypeCacheManager.GetCachedType("System.Int32");
        TypeCacheManager.GetCachedType("System.String"); // Cache hit

        // Act
        var stats = optimizationManager.GetStatistics();

        // Assert
        Assert.IsNotNull(stats);
        Assert.IsNotNull(stats.CacheStatistics);
        Assert.AreEqual(2, stats.CacheStatistics.TypeCacheSize);
        Assert.AreEqual(1, stats.CacheStatistics.TypeCacheHits);
        Assert.AreEqual(2, stats.CacheStatistics.TypeCacheMisses);
    }

    [TestMethod]
    public void OptimizationManager_CanConfigureOptimizations()
    {
        // Arrange
        var optimizationManager = new OptimizationManager();

        // Act
        optimizationManager.ConfigureOptimization(OptimizationType.StagedTransformation, false);

        // Test with a complex query that would normally use staged transformation
        var input = new QueryAnalysisInput
        {
            QueryId = "test_disabled_staging",
            Pattern = new QueryPattern
            {
                HasJoins = true,
                HasAggregations = true,
                ComplexityScore = 10
            },
            Context = new QueryAnalysisContext
            {
                HasJoins = true,
                HasAggregations = true,
                ComplexityScore = 10
            }
        };

        var plan = optimizationManager.AnalyzeQuery(input);

        // Assert
        Assert.IsFalse(plan.EnabledOptimizations.Contains(OptimizationType.StagedTransformation));
    }
}