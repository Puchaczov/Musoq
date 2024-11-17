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
        public bool WhereFilter(int value) { return true; }
        public bool WhereFilterWithEntity([InjectSpecificSource(typeof(ITestEntity))] ITestEntity entity, int value) { return true; }
        
        [AggregationGetMethod]
        public decimal GetValue(string name) { return 0m; }
        
        public decimal GetValueWithGroup(string name, [InjectGroup] object group) { return 0m; }
        
        [AggregationMethod]
        public void Sum(string name, decimal value) { }
        
        [AggregationMethod]
        public void Count(string name) { }
        
        [AggregationGetMethod]
        public decimal MixedContextMethod(int value) { return 0m; }
        
        public decimal StatsMethod([InjectQueryStats] object stats, int value) { return 0m; }
        
        public string Overloaded(int value) { return ""; }
        
        [AggregationMethod]
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
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("WhereFilter", [typeof(int)], null, out var method),
            "Should resolve basic where filter"
        );
        
        Assert.AreEqual(typeof(bool), method.ReturnType, "Where clause method should return bool");
    }

    [TestMethod]
    public void TryGetMethod_WhereClause_WithEntityInjection()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("WhereFilterWithEntity", [typeof(int)], typeof(TestEntity), out var method),
            "Should resolve where filter with entity"
        );
        
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("WhereFilterWithEntity", [typeof(int)], typeof(string), out _),
            "Should not resolve where filter with wrong entity type"
        );
    }

    [TestMethod]
    public void TryGetMethod_GroupClause_AggregationMethods()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GetValue", [typeof(string)], null, out var method),
            "Should resolve group value getter"
        );
        
        Assert.IsTrue(Attribute.IsDefined(method, typeof(AggregationGetMethodAttribute)),
            "Method should have AggregationGetMethod attribute");
    }

    [TestMethod]
    public void TryGetMethod_GroupClause_WithGroupInjection()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GetValueWithGroup", [typeof(string)], null, out var method),
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
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Sum", [typeof(string), typeof(decimal)], null, out var sumMethod),
            "Should resolve Sum aggregation method"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Count", [typeof(string)], null, out var countMethod),
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
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedContextMethod", [typeof(int)], null, out var method),
            "Should resolve in mixed context"
        );
        
        Assert.IsTrue(Attribute.IsDefined(method, typeof(AggregationGetMethodAttribute)),
            "Mixed context method should have AggregationGetMethod attribute");
    }

    [TestMethod]
    public void TryGetMethod_StatsContext()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("StatsMethod", [typeof(int)], typeof(TestEntity), out var method),
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
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Overloaded", [typeof(int)], null, out var regularMethod),
            "Should resolve regular overload"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Overloaded", [typeof(string), typeof(int)], null, out var aggMethod),
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
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("Sum", [typeof(decimal)], null, out _),
            "Should not resolve aggregation method with wrong parameters"
        );
        
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("WhereFilter", [typeof(object), typeof(int)], null, out _),
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