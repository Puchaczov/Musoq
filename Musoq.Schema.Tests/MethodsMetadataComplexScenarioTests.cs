using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadataComplexScenarioTests
{
    // Type hierarchy for testing
    private interface IEntity { }
    private interface IAggregatable { }
    private class Entity : IEntity { }

    private class TestClass
    {
        // Complex aggregation with entity injection and optional params
        [AggregationMethodAttribute]
        public void ComplexAggregation(
            string name,
            [InjectSpecificSourceAttribute(typeof(IEntity))] IEntity entity,
            decimal value,
            string format = "F2",
            params string[] tags)
        { }

        [AggregationGetMethod]
        public decimal AggregateData(string name) { return 0m; }

        // Generic method with multiple constraints and injections
        public TResult ProcessData<TSource, TResult>(
            [InjectSpecificSourceAttribute(typeof(IEntity))] IEntity entity,
            TSource source,
            Func<TSource, TResult> transformer)
            where TSource : struct
            where TResult : class
        { return null; }

        // Multiple overloads with different generic/non-generic combinations
        public void ProcessCollection<T>(IEnumerable<T> items) { }
        public void ProcessCollection(IEnumerable<int> items) { }
        public void ProcessCollection(int[] items) { }
        public void ProcessCollection(params object[] items) { }

        // Complex nullable scenarios with multiple overloads
        public string HandleNullableData(int? value, string format = null) { return string.Empty; }
        public string HandleNullableData(DateTime? value, string format = null) { return string.Empty; }
        public string HandleNullableData(object value, string format = null) { return string.Empty; }

        // Mixed injection types with generics
        public void ComplexInjection<T>(
            [InjectSpecificSourceAttribute(typeof(IEntity))] IEntity entity,
            [InjectGroupAttribute] object group,
            T value,
            params string[] additionalData)
            where T : IEntity
        { }
    }

    private MethodsMetadata _methodsMetadata;

    [TestInitialize]
    public void Initialize()
    {
        _methodsMetadata = new TestMethodsMetadata();
    }

    [TestMethod]
    public void TryGetMethod_ComplexAggregation_WithAllParameters()
    {
        // Test full parameter set
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "ComplexAggregation",
                new[] { typeof(string), typeof(decimal), typeof(string), typeof(string), typeof(string) },
                typeof(Entity),
                out var method),
            "Should resolve with all parameters"
        );

        var parameters = method.GetParameters();
        Assert.AreEqual(5, parameters.Length);
        Assert.IsTrue(parameters[^1].GetCustomAttribute<ParamArrayAttribute>() != null);
        Assert.IsTrue(Attribute.IsDefined(method, typeof(AggregationMethodAttribute)));
    }

    [TestMethod]
    public void TryGetMethod_ComplexAggregation_WithOptionalParameters()
    {
        // Test without optional parameters
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "ComplexAggregation",
                new[] { typeof(string), typeof(decimal) },
                typeof(Entity),
                out var method),
            "Should resolve without optional parameters"
        );

        // Test with null for optional parameter
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "ComplexAggregation",
                new[] { typeof(string), typeof(decimal), typeof(NullNode.NullType) },
                typeof(Entity),
                out method),
            "Should resolve with null optional parameter"
        );
    }

    [TestMethod]
    public void TryGetMethod_ProcessCollection_Overloads()
    {
        // Test specific array type
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "ProcessCollection",
                new[] { typeof(int[]) },
                null,
                out var arrayMethod),
            "Should resolve array method"
        );

        // Test generic enumerable
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "ProcessCollection",
                new[] { typeof(IEnumerable<string>) },
                null,
                out var genericMethod),
            "Should resolve generic method"
        );

        // Test params array
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "ProcessCollection",
                new[] { typeof(object), typeof(object) },
                null,
                out var paramsMethod),
            "Should resolve params method"
        );

        Assert.AreNotEqual(arrayMethod, genericMethod);
        Assert.AreNotEqual(genericMethod, paramsMethod);
    }

    [TestMethod]
    public void TryGetMethod_HandleNullableData_ComplexResolution()
    {
        // Test with explicit null
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "HandleNullableData",
                new[] { typeof(NullNode.NullType), typeof(NullNode.NullType) },
                null,
                out var method),
            "Should resolve with explicit nulls"
        );

        // Test with specific nullable type
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "HandleNullableData",
                new[] { typeof(int?), typeof(string) },
                null,
                out method),
            "Should resolve with specific nullable type"
        );

        // Test with non-nullable value
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "HandleNullableData",
                new[] { typeof(int), typeof(string) },
                null,
                out method),
            "Should resolve with non-nullable value"
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