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
    private interface IEntity { }
    private interface IAggregatable { }
    private class Entity : IEntity { }

    private class TestClass
    {
        [AggregationMethod]
        public void ComplexAggregation(
            string name,
            [InjectSpecificSource(typeof(IEntity))] IEntity entity,
            decimal value,
            string format = "F2",
            params string[] tags)
        { }

        [AggregationGetMethod]
        public decimal AggregateData(string name) { return 0m; }

        public TResult ProcessData<TSource, TResult>(
            [InjectSpecificSource(typeof(IEntity))] IEntity entity,
            TSource source,
            Func<TSource, TResult> transformer)
            where TSource : struct
            where TResult : class
        { return null; }

        public void ProcessCollection<T>(IEnumerable<T> items) { }
        public void ProcessCollection(IEnumerable<int> items) { }
        public void ProcessCollection(int[] items) { }
        public void ProcessCollection(params object[] items) { }

        public string HandleNullableData(int? value, string format = null) { return string.Empty; }
        public string HandleNullableData(DateTime? value, string format = null) { return string.Empty; }
        public string HandleNullableData(object value, string format = null) { return string.Empty; }

        public void ComplexInjection<T>(
            [InjectSpecificSource(typeof(IEntity))] IEntity entity,
            [InjectGroup] object group,
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
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "ComplexAggregation",
                [typeof(string), typeof(decimal), typeof(string), typeof(string), typeof(string)],
                typeof(Entity),
                out var method),
            "Should resolve with all parameters"
        );

        var parameters = method.GetParameters();
        Assert.HasCount(5, parameters);
        Assert.IsNotNull(parameters[^1].GetCustomAttribute<ParamArrayAttribute>());
        Assert.IsTrue(Attribute.IsDefined(method, typeof(AggregationMethodAttribute)));
    }

    [TestMethod]
    public void TryGetMethod_ComplexAggregation_WithOptionalParameters()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "ComplexAggregation",
                [typeof(string), typeof(decimal)],
                typeof(Entity),
                out _),
            "Should resolve without optional parameters"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "ComplexAggregation",
                [typeof(string), typeof(decimal), typeof(NullNode.NullType)],
                typeof(Entity),
                out _),
            "Should resolve with null optional parameter"
        );
    }

    [TestMethod]
    public void TryGetMethod_ProcessCollection_Overloads()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "ProcessCollection",
                [typeof(int[])],
                null,
                out var arrayMethod),
            "Should resolve array method"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "ProcessCollection",
                [typeof(IEnumerable<string>)],
                null,
                out var genericMethod),
            "Should resolve generic method"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "ProcessCollection",
                [typeof(object), typeof(object)],
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
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "HandleNullableData",
                [typeof(NullNode.NullType), typeof(NullNode.NullType)],
                null,
                out _),
            "Should resolve with explicit nulls"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "HandleNullableData",
                [typeof(int?), typeof(string)],
                null,
                out _),
            "Should resolve with specific nullable type"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod(
                "HandleNullableData",
                [typeof(int), typeof(string)],
                null,
                out _),
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