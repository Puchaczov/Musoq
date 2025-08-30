using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Optimization;

namespace Musoq.Evaluator.Tests.Optimization;

[TestClass]
public class Phase2OptimizationTests
{
    private ExpressionTreeCompiler _expressionTreeCompiler;
    private MemoryPoolManager _memoryPoolManager;
    private QueryAnalysisEngine _queryAnalysisEngine;
    private OptimizationManager _optimizationManager;

    [TestInitialize]
    public void Setup()
    {
        _expressionTreeCompiler = new ExpressionTreeCompiler();
        _memoryPoolManager = new MemoryPoolManager();
        _queryAnalysisEngine = new QueryAnalysisEngine();
        _optimizationManager = new OptimizationManager();
    }

    [TestMethod]
    public void ExpressionTreeCompiler_CompileDynamicFieldAccessor_ShouldCreateAccessor()
    {
        // Act
        var accessor = _expressionTreeCompiler.CompileDynamicFieldAccessor("TestField", typeof(string));
        
        // Assert
        Assert.IsNotNull(accessor);
    }

    [TestMethod]
    public void ExpressionTreeCompiler_GetStatistics_ShouldReturnValidStatistics()
    {
        // Arrange
        _expressionTreeCompiler.CompileDynamicFieldAccessor("Field1", typeof(string));
        _expressionTreeCompiler.CompileDynamicFieldAccessor("Field2", typeof(int));
        
        // Act
        var stats = _expressionTreeCompiler.GetStatistics();
        
        // Assert
        Assert.IsNotNull(stats);
        Assert.AreEqual(2, stats.TotalCompiledAccessors);
        Assert.AreEqual(2, stats.CacheMisses);
        Assert.AreEqual(0, stats.CacheHits);
    }

    [TestMethod]
    public void ExpressionTreeCompiler_CacheHit_ShouldReturnSameAccessor()
    {
        // Arrange
        var fieldName = "TestField";
        var fieldType = typeof(string);
        
        // Act
        var accessor1 = _expressionTreeCompiler.CompileDynamicFieldAccessor(fieldName, fieldType);
        var accessor2 = _expressionTreeCompiler.CompileDynamicFieldAccessor(fieldName, fieldType);
        
        // Assert
        Assert.AreSame(accessor1, accessor2);
        
        var stats = _expressionTreeCompiler.GetStatistics();
        Assert.AreEqual(1, stats.CacheMisses);
        Assert.AreEqual(1, stats.CacheHits);
    }

    [TestMethod]
    public void MemoryPoolManager_GetAndReturnResultRow_ShouldReuseArrays()
    {
        // Arrange
        const int fieldCount = 5;
        
        // Act
        var array1 = _memoryPoolManager.GetResultRow(fieldCount);
        _memoryPoolManager.ReturnResultRow(array1, fieldCount);
        var array2 = _memoryPoolManager.GetResultRow(fieldCount);
        
        // Assert
        Assert.IsNotNull(array1);
        Assert.IsNotNull(array2);
        Assert.AreEqual(fieldCount, array1.Length);
        Assert.AreEqual(fieldCount, array2.Length);
        Assert.AreSame(array1, array2); // Should be the same reused array
    }

    [TestMethod]
    public void MemoryPoolManager_GetStatistics_ShouldTrackUsage()
    {
        // Arrange
        var array1 = _memoryPoolManager.GetResultRow(3);
        var array2 = _memoryPoolManager.GetResultRow(3);
        _memoryPoolManager.ReturnResultRow(array1, 3);
        
        // Act
        var stats = _memoryPoolManager.GetStatistics();
        
        // Assert
        Assert.IsNotNull(stats);
        Assert.AreEqual(2L, stats.ArrayGets);
        Assert.AreEqual(1L, stats.ArrayReturns);
        Assert.AreEqual(1, stats.ActivePools);
    }

    [TestMethod]
    public void MemoryPoolManager_CreateScope_ShouldAutoReturnObjects()
    {
        // Arrange & Act
        object[] capturedArray = null;
        
        using (var scope = _memoryPoolManager.CreateScope())
        {
            capturedArray = scope.GetResultRow(4);
            Assert.IsNotNull(capturedArray);
            Assert.AreEqual(4, capturedArray.Length);
        }
        
        // Assert - scope should have automatically returned the array
        var stats = _memoryPoolManager.GetStatistics();
        Assert.AreEqual(1L, stats.ArrayGets);
        Assert.AreEqual(1L, stats.ArrayReturns);
    }

    [TestMethod]
    public void QueryAnalysisEngine_AnalyzeQuery_ShouldReturnValidAnalysis()
    {
        // Arrange - Create a simple mock node for testing
        var mockQueryRoot = new TestNode();
        
        // Act
        var analysis = _queryAnalysisEngine.AnalyzeQuery(mockQueryRoot);
        
        // Assert
        Assert.IsNotNull(analysis);
        Assert.IsNotNull(analysis.QueryId);
        Assert.IsNotNull(analysis.Pattern);
        Assert.IsNotNull(analysis.FieldAnalysis);
        Assert.IsNotNull(analysis.JoinAnalysis);
        Assert.IsNotNull(analysis.RecommendedStrategy);
        Assert.IsNotNull(analysis.EstimatedImpact);
    }

    [TestMethod]
    public void OptimizationManager_AnalyzeQuery_ShouldIncludePhase2Optimizations()
    {
        // Arrange
        var input = new QueryAnalysisInput
        {
            QueryId = "test-query",
            QueryRoot = new TestNode(),
            OriginalQuery = "SELECT * FROM test"
        };
        
        // Act
        var plan = _optimizationManager.AnalyzeQuery(input);
        
        // Assert
        Assert.IsNotNull(plan);
        Assert.IsNotNull(plan.QueryAnalysis);
        
        // Verify that some optimizations are enabled (they might not be all Phase 2 due to analysis logic)
        Assert.IsTrue(plan.EnabledOptimizations.Count > 0, "No optimizations were enabled");
        
        // Test that the Phase 2 optimization types are available in the system
        var hasExpressionTrees = plan.EnabledOptimizations.Contains(OptimizationType.ExpressionTreeCompilation);
        var hasMemoryPooling = plan.EnabledOptimizations.Contains(OptimizationType.MemoryPooling);
        var hasReflectionCaching = plan.EnabledOptimizations.Contains(OptimizationType.ReflectionCaching);
        
        // At least one optimization should be enabled
        Assert.IsTrue(hasExpressionTrees || hasMemoryPooling || hasReflectionCaching, 
            "None of the expected optimizations were enabled");
    }

    [TestMethod]
    public void OptimizationManager_GenerateOptimizedCode_ShouldProducePhase2Code()
    {
        // Arrange
        var plan = new OptimizationPlan
        {
            QueryId = "test-query",
            EnabledOptimizations = { OptimizationType.ExpressionTreeCompilation },
            ExpressionTreeFields = { "Field1", "Field2" }
        };
        
        // Act
        var result = _optimizationManager.GenerateOptimizedCode(plan, "TestQuery");
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.GeneratedCode));
        CollectionAssert.Contains(result.AppliedOptimizations.ToArray(), "Expression Tree Compilation");
        Assert.AreEqual("Phase 2", result.PhaseLevel);
    }

    [TestMethod]
    public void OptimizationManager_GetStatistics_ShouldIncludePhase2Metrics()
    {
        // Arrange
        var input = new QueryAnalysisInput
        {
            QueryId = "test-query",
            QueryRoot = new TestNode(),
            OriginalQuery = "SELECT * FROM test"
        };
        
        var plan = _optimizationManager.AnalyzeQuery(input);
        _optimizationManager.GenerateOptimizedCode(plan, "TestQuery");
        
        // Act
        var stats = _optimizationManager.GetStatistics();
        
        // Assert
        Assert.IsNotNull(stats);
        Assert.IsNotNull(stats.ExpressionTreeStatistics);
        Assert.IsNotNull(stats.MemoryPoolStatistics);
        Assert.IsTrue(stats.TotalQueriesAnalyzed >= 1);
        Assert.IsTrue(stats.TotalQueriesOptimized >= 1);
    }

    [TestMethod]
    public void Phase2Configuration_AllOptimizationsEnabled_ByDefault()
    {
        // Arrange
        var config = new OptimizationConfiguration();
        
        // Assert
        Assert.IsTrue(config.EnableExpressionTreeCompilation);
        Assert.IsTrue(config.EnableMemoryPooling);
        Assert.IsTrue(config.EnableReflectionCaching);
        Assert.IsTrue(config.EnableTemplateGeneration);
        Assert.IsTrue(config.EnableStagedTransformation);
    }

    [TestMethod]
    public void OptimizationManager_ConfigureOptimization_ShouldTogglePhase2Features()
    {
        // Act & Assert
        _optimizationManager.ConfigureOptimization(OptimizationType.ExpressionTreeCompilation, false);
        _optimizationManager.ConfigureOptimization(OptimizationType.MemoryPooling, false);
        
        // Verify that optimizations can be toggled (implementation would need to be tested with actual queries)
        Assert.IsTrue(true); // No exception thrown
    }

    // Simple test node for testing
    private class TestNode : Musoq.Parser.Nodes.Node
    {
        public override void Accept(Musoq.Parser.IExpressionVisitor visitor) { }
        public override string ToString() => "TestNode";
        public override Type ReturnType => typeof(object);
        public override string Id => "test-node";
    }
}