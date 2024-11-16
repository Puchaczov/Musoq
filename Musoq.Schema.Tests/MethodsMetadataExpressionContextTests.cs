using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadataExpressionContextTests
{
    private interface ITestEntity { }
    private class TestEntity : ITestEntity { }

    private class TestClass
    {
        // Where clause methods
        public bool WhereFilter(int value) { return true; }
        public bool WhereFilterWithEntity([InjectSpecificSourceAttribute(typeof(ITestEntity))] ITestEntity entity, int value) { return true; }
        
        // Group methods
        [AggregationGetMethod]
        public decimal GetValue(string name) { return 0m; }
        
        [AggregationGetMethod]
        public decimal GetValueWithGroup(string name, [InjectGroupAttribute] object group) { return 0m; }
        
        // Aggregation methods
        [AggregationMethodAttribute]
        public void Sum(string name, decimal value) { }
        
        [AggregationMethodAttribute]
        public void Count(string name) { }
        
        // Mixed methods (both Where and Select contexts)
        [AggregationGetMethod]
        public decimal MixedContextMethod(int value) { return 0m; }
        
        // Methods with specific statistics
        public decimal StatsMethod([InjectQueryStatsAttribute] object stats, int value) { return 0m; }
        
        // Overloaded methods for different contexts
        public string Overloaded(int value) { return ""; }
        
        [AggregationMethodAttribute]
        public void Overloaded(string name, int value) { }
    }

    private MethodsMetadata _methodsMetadata;

    [TestInitialize]
    public void Initialize()
    {
        _methodsMetadata = new TestMethodsMetadata();
    }

    [TestMethod]
    public void TryGetMethod_WhereClause_SimpleFilter()
    {
        // Basic where clause filter
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("WhereFilter", new[] { typeof(int) }, null, out var method),
            "Should resolve basic where filter"
        );
        
        Assert.AreEqual(typeof(bool), method.ReturnType, "Where clause method should return bool");
    }

    [TestMethod]
    public void TryGetMethod_WhereClause_WithEntityInjection()
    {
        // Where clause with entity injection
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("WhereFilterWithEntity", new[] { typeof(int) }, typeof(TestEntity), out var method),
            "Should resolve where filter with entity"
        );
        
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("WhereFilterWithEntity", new[] { typeof(int) }, typeof(string), out _),
            "Should not resolve where filter with wrong entity type"
        );
    }

    [TestMethod]
    public void TryGetMethod_GroupClause_AggregationMethods()
    {
        // Group context aggregation methods
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GetValue", new[] { typeof(string) }, null, out var method),
            "Should resolve group value getter"
        );
        
        Assert.IsTrue(Attribute.IsDefined(method, typeof(AggregationGetMethodAttribute)),
            "Method should have AggregationGetMethod attribute");
    }

    [TestMethod]
    public void TryGetMethod_GroupClause_WithGroupInjection()
    {
        // Group context with group injection
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GetValueWithGroup", new[] { typeof(string) }, null, out var method),
            "Should resolve group value getter with group injection"
        );
        
        var parameters = method.GetParameters();
        Assert.IsTrue(parameters.Length > 1 && 
                     Attribute.IsDefined(parameters[1], typeof(InjectGroupAttribute)),
            "Method should have InjectGroupAttribute parameter");
    }

    [TestMethod]
    public void TryGetMethod_AggregationMethods()
    {
        // Basic aggregation methods
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Sum", new[] { typeof(string), typeof(decimal) }, null, out var sumMethod),
            "Should resolve Sum aggregation method"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Count", new[] { typeof(string) }, null, out var countMethod),
            "Should resolve Count aggregation method"
        );
        
        Assert.IsTrue(Attribute.IsDefined(sumMethod, typeof(AggregationMethodAttribute)), 
            "Sum should have AggregationMethodAttribute");
        Assert.IsTrue(Attribute.IsDefined(countMethod, typeof(AggregationMethodAttribute)),
            "Count should have AggregationMethodAttribute");
    }

    [TestMethod]
    public void TryGetMethod_MixedContext()
    {
        // Method that works in both Where and Select contexts
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedContextMethod", new[] { typeof(int) }, null, out var method),
            "Should resolve in mixed context"
        );
        
        Assert.IsTrue(Attribute.IsDefined(method, typeof(AggregationGetMethodAttribute)),
            "Mixed context method should have AggregationGetMethod attribute");
    }

    [TestMethod]
    public void TryGetMethod_StatsContext()
    {
        // Methods with stats injection
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("StatsMethod", new[] { typeof(int) }, typeof(TestEntity), out var method),
            "Should resolve with stats injection"
        );
        
        var parameters = method.GetParameters();
        Assert.IsTrue(parameters.Length > 0 && 
                     Attribute.IsDefined(parameters[0], typeof(InjectQueryStatsAttribute)),
            "Method should have InjectQueryStatsAttribute parameter");
    }

    [TestMethod]
    public void TryGetMethod_OverloadResolution_DifferentContexts()
    {
        // Regular method call
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Overloaded", new[] { typeof(int) }, null, out var regularMethod),
            "Should resolve regular overload"
        );
        
        // Aggregation method call
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Overloaded", new[] { typeof(string), typeof(int) }, null, out var aggMethod),
            "Should resolve aggregation overload"
        );
        
        Assert.IsFalse(Attribute.IsDefined(regularMethod, typeof(AggregationMethodAttribute)),
            "Regular method should not have AggregationMethodAttribute");
        Assert.IsTrue(Attribute.IsDefined(aggMethod, typeof(AggregationMethodAttribute)),
            "Aggregation method should have AggregationMethodAttribute");
    }

    [TestMethod]
    public void TryGetMethod_InvalidContexts()
    {
        // Try to use aggregation method in non-aggregation context
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("Sum", new[] { typeof(decimal) }, null, out _),
            "Should not resolve aggregation method with wrong parameters"
        );
        
        // Try to use regular method with group injection
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("WhereFilter", new[] { typeof(object), typeof(int) }, null, out _),
            "Should not resolve regular method with group injection"
        );
    }

    private class TestMethodsMetadata : MethodsMetadata
    {
        public TestMethodsMetadata()
        {
            var testClass = typeof(TestClass);
            foreach (var method in testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                RegisterMethod(method);
            }
        }

        private new void RegisterMethod(MethodInfo methodInfo)
        {
            base.RegisterMethod(methodInfo);
        }
    }
}