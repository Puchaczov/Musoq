using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Optimization;
using Microsoft.Extensions.Logging;

namespace Musoq.Evaluator.Tests.Optimization
{
    [TestClass]
    public class PerformanceValidationTests
    {
        [TestMethod]
        public void OptimizationManager_AnalysisEngine_ProducesCorrectOptimizationPlan()
        {
            // Arrange
            var optimizationManager = new OptimizationManager();
            var complexInput = new QueryAnalysisInput
            {
                QueryId = "performance_test_complex",
                Pattern = new QueryPattern
                {
                    HasJoins = true,
                    HasAggregations = true,
                    HasComplexFiltering = true,
                    ComplexityScore = 10,
                    RequiredFields = Enumerable.Range(0, 12).Select(i => $"Field{i}").ToArray(), // 12 fields
                    RequiredTypes = new[] { typeof(string), typeof(int), typeof(DateTime) }
                },
                Context = new QueryAnalysisContext
                {
                    HasFiltering = true,
                    HasProjections = true,
                    HasJoins = true,
                    HasAggregations = true,
                    ComplexityScore = 10
                }
            };

            // Act
            var plan = optimizationManager.AnalyzeQuery(complexInput);

            // Assert
            Assert.IsNotNull(plan, "Optimization plan should be generated");
            Assert.IsTrue(plan.EnabledOptimizations.Count >= 3, "Complex query should enable multiple optimizations");
            
            // Verify expected optimizations for complex query
            Assert.IsTrue(plan.EnabledOptimizations.Contains(OptimizationType.ExpressionTreeCompilation),
                "ExpressionTreeCompilation should be enabled for 12 fields");
            Assert.IsTrue(plan.EnabledOptimizations.Contains(OptimizationType.MemoryPooling),
                "MemoryPooling should be enabled for complex query with aggregations");
            Assert.IsTrue(plan.EnabledOptimizations.Contains(OptimizationType.StagedTransformation),
                "StagedTransformation should be enabled for complexity score 10");
            Assert.IsTrue(plan.EnabledOptimizations.Contains(OptimizationType.ReflectionCaching),
                "ReflectionCaching should always be enabled");

            Assert.AreEqual(OptimizationLevel.Advanced, plan.OptimizationLevel,
                "Complex query should use Advanced optimization level");
            Assert.IsTrue(plan.EstimatedImprovement >= 0.6,
                "Complex query should show high estimated improvement");

            Console.WriteLine($"Enabled optimizations: {string.Join(", ", plan.EnabledOptimizations)}");
            Console.WriteLine($"Optimization level: {plan.OptimizationLevel}");
            Console.WriteLine($"Estimated improvement: {plan.EstimatedImprovement:P1}");
        }

        [TestMethod]
        public void OptimizationManager_ConfigurationChanges_AffectOptimizationSelection()
        {
            // Arrange
            var optimizationManager = new OptimizationManager();
            var input = new QueryAnalysisInput
            {
                QueryId = "config_test",
                Pattern = new QueryPattern
                {
                    HasJoins = true,
                    HasAggregations = true,
                    ComplexityScore = 8,
                    RequiredFields = new[] { "A", "B", "C", "D", "E", "F" }, // 6 fields
                    RequiredTypes = new[] { typeof(string), typeof(int) }
                },
                Context = new QueryAnalysisContext
                {
                    HasJoins = true,
                    HasAggregations = true,
                    ComplexityScore = 8
                }
            };

            // Test with all optimizations enabled
            var planEnabled = optimizationManager.AnalyzeQuery(input);
            var enabledCount = planEnabled.EnabledOptimizations.Count;

            // Disable specific optimizations
            optimizationManager.ConfigureOptimization(OptimizationType.ExpressionTreeCompilation, false);
            optimizationManager.ConfigureOptimization(OptimizationType.MemoryPooling, false);

            var planDisabled = optimizationManager.AnalyzeQuery(input);
            var disabledCount = planDisabled.EnabledOptimizations.Count;

            // Assert
            Assert.IsTrue(enabledCount > disabledCount, 
                "Disabling optimizations should reduce the number of enabled optimizations");
            Assert.IsFalse(planDisabled.EnabledOptimizations.Contains(OptimizationType.ExpressionTreeCompilation),
                "ExpressionTreeCompilation should be disabled");
            Assert.IsFalse(planDisabled.EnabledOptimizations.Contains(OptimizationType.MemoryPooling),
                "MemoryPooling should be disabled");

            Console.WriteLine($"Enabled count: {enabledCount}, Disabled count: {disabledCount}");
        }

        [TestMethod]
        public void OptimizationStatistics_TrackPerformanceMetrics()
        {
            // Arrange
            var optimizationManager = new OptimizationManager();
            var input = new QueryAnalysisInput
            {
                QueryId = "stats_test",
                Pattern = new QueryPattern
                {
                    HasAggregations = true,
                    ComplexityScore = 6,
                    RequiredFields = new[] { "Field1", "Field2", "Field3", "Field4", "Field5", "Field6" },
                    RequiredTypes = new[] { typeof(string) }
                },
                Context = new QueryAnalysisContext
                {
                    HasAggregations = true,
                    ComplexityScore = 6
                }
            };

            // Act
            var plan = optimizationManager.AnalyzeQuery(input);
            var result = optimizationManager.GenerateOptimizedCode(plan, "TestClass");
            var statistics = optimizationManager.GetStatistics();

            // Assert
            Assert.IsNotNull(statistics, "Statistics should be available");
            Assert.IsTrue(statistics.TotalQueriesAnalyzed > 0, "Should track analyzed queries");
            Assert.IsTrue(statistics.TotalQueriesOptimized > 0, "Should track optimized queries");
            Assert.IsNotNull(result, "Should generate optimization result");
            Assert.IsTrue(result.AppliedOptimizations.Count > 0, "Should apply optimizations");

            Console.WriteLine($"Queries analyzed: {statistics.TotalQueriesAnalyzed}");
            Console.WriteLine($"Queries optimized: {statistics.TotalQueriesOptimized}");
            Console.WriteLine($"Applied optimizations: {string.Join(", ", result.AppliedOptimizations)}");
            Console.WriteLine($"Code quality score: {result.CodeQualityScore:F1}");
        }
    }
}