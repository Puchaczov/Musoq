using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadataExpressionContextTests : MethodsMetadataTestBase
{
    private MethodsMetadata _methodsMetadata = CreateMethodsMetadataFor<TestClass>();

    [TestInitialize]
    public void Initialize()
    {
        _methodsMetadata = CreateMethodsMetadataFor<TestClass>();
    }

    [TestMethod]
    public void TryGetMethod_WhereClause_SimpleFilter()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("WhereFilter", [typeof(int)], null, out var method),
            "Should resolve basic where filter"
        );

        method = RequireResolved(method);
        Assert.AreEqual(typeof(bool), method.ReturnType, "Where clause method should return bool");
    }

    [TestMethod]
    public void TryGetMethod_WhereClause_WithEntityInjection()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("WhereFilterWithEntity", [typeof(int)], typeof(TestEntity), out _),
            "Should resolve where filter with entity"
        );

        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("WhereFilterWithEntity", [typeof(int)], typeof(string), out _),
            "Should not resolve where filter with wrong entity type"
        );
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

        sumMethod = RequireResolved(sumMethod);
        countMethod = RequireResolved(countMethod);
        Assert.IsTrue(Attribute.IsDefined(sumMethod, typeof(AggregateFunctionAttribute)),
            "Sum should have AggregateFunctionAttribute");
        Assert.IsTrue(Attribute.IsDefined(countMethod, typeof(AggregateFunctionAttribute)),
            "Count should have AggregateFunctionAttribute");
    }

    [TestMethod]
    public void TryGetMethod_StatsContext()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("StatsMethod", [typeof(int)], typeof(TestEntity), out var method),
            "Should resolve with stats injection"
        );

        method = RequireResolved(method);
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

        regularMethod = RequireResolved(regularMethod);
        aggMethod = RequireResolved(aggMethod);
        Assert.IsFalse(Attribute.IsDefined(regularMethod, typeof(AggregateFunctionAttribute)),
            "Regular method should not have AggregateFunctionAttribute");
        Assert.IsTrue(Attribute.IsDefined(aggMethod, typeof(AggregateFunctionAttribute)),
            "Aggregation method should have AggregateFunctionAttribute");
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

    private interface ITestEntity;

    private sealed class TestEntity : ITestEntity;

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private sealed class TestClass
    {
        public bool WhereFilter(int value)
        {
            return true;
        }

        public bool WhereFilterWithEntity([InjectSpecificSource(typeof(ITestEntity))] ITestEntity entity, int value)
        {
            return true;
        }

        [AggregateFunction(typeof(MetadataAggregateKernel))]
        public int Sum(string name, decimal value)
        {
            return 0;
        }

        [AggregateFunction(typeof(MetadataAggregateKernel))]
        public int Count(string name)
        {
            return 0;
        }

        public decimal StatsMethod([InjectQueryStats] object stats, int value)
        {
            return 0m;
        }

        public string Overloaded(int value)
        {
            return "";
        }

        [AggregateFunction(typeof(MetadataAggregateKernel))]
        public int Overloaded(string name, int value)
        {
            return 0;
        }
    }
}
