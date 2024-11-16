using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadataMethodOrderingTests
{
    // Type hierarchy for testing
    private interface IBase { }
    private interface ISpecific : IBase { }
    private class Base : IBase { }
    private class Specific : Base, ISpecific { }
    private class MoreSpecific : Specific { }

    private class TestClass
    {
        // Overloads with different numeric types
        public void NumericMethod(int x) { }
        public void NumericMethod(long x) { }
        public void NumericMethod(short x) { }
        
        // Overloads with inheritance
        public string InheritanceMethod([InjectSpecificSourceAttribute(typeof(IBase))] IBase x) { return "IBase"; }
        public string InheritanceMethod([InjectSpecificSourceAttribute(typeof(ISpecific))] ISpecific x) { return "ISpecific"; }
        public string InheritanceMethod([InjectSpecificSourceAttribute(typeof(Base))] Base x) { return "Base"; }
        public string InheritanceMethod([InjectSpecificSourceAttribute(typeof(Specific))] Specific x) { return "Specific"; }
        
        // Generic vs non-generic methods
        public void GenericMethod<T>(T value) { }
        public void GenericMethod(int value) { }
        public void GenericMethod(string value) { }
        
        // Multiple parameters with mixed types
        public string MixedParams(int x, Base b) { return "int,Base"; }
        public string MixedParams(long x, Specific s) { return "long,Specific"; }
        public string MixedParams(int x, Specific s) { return "int,Specific"; }
        
        // Optional parameters
        public string OptionalParams(int x, string s = "default") { return "optional"; }
        public string OptionalParams(int x, string s, int y = 42) { return "more_params"; }
        
        // Overloads with different contexts
        [AggregationMethodAttribute]
        public void ContextMethod(string name, int value) { }
        public void ContextMethod(int value) { }
    }

    private MethodsMetadata _methodsMetadata;

    [TestInitialize]
    public void Initialize()
    {
        _methodsMetadata = new TestMethodsMetadata();
    }

    [TestMethod]
    public void TryGetMethod_NumericTypes_ExactMatch()
    {
        var result = _methodsMetadata.TryGetMethod("NumericMethod", new[] { typeof(int) }, null, out var method);
        Assert.IsTrue(result);
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_NumericTypes_ImplicitConversion()
    {
        var result = _methodsMetadata.TryGetMethod("NumericMethod", new[] { typeof(short) }, null, out var method);
        Assert.IsTrue(result);
        // Should choose the short method due to exact match
        Assert.AreEqual(typeof(short), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_Generic_PreferNonGeneric()
    {
        // Should prefer non-generic int overload
        var result = _methodsMetadata.TryGetMethod("GenericMethod", new[] { typeof(int) }, null, out var method);
        Assert.IsTrue(result);
        Assert.IsFalse(method.IsGenericMethod);
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
        
        // Should use generic method for unsupported type
        result = _methodsMetadata.TryGetMethod("GenericMethod", new[] { typeof(DateTime) }, null, out method);
        Assert.IsTrue(result);
        Assert.IsTrue(method.IsGenericMethod);
    }

    [TestMethod]
    public void TryGetMethod_MixedParams_MostSpecificMatch()
    {
        // Should choose most specific parameter types
        var result = _methodsMetadata.TryGetMethod("MixedParams", new[] { typeof(int), typeof(Specific) }, null, out var method);
        Assert.IsTrue(result);
        var parameters = method.GetParameters();
        Assert.AreEqual(typeof(int), parameters[0].ParameterType);
        Assert.AreEqual(typeof(Specific), parameters[1].ParameterType);
        
        // Test with base type
        result = _methodsMetadata.TryGetMethod("MixedParams", new[] { typeof(int), typeof(Base) }, null, out method);
        Assert.IsTrue(result);
        parameters = method.GetParameters();
        Assert.AreEqual(typeof(int), parameters[0].ParameterType);
        Assert.AreEqual(typeof(Base), parameters[1].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_OptionalParams_PreferFewerParameters()
    {
        // Should prefer method with fewer parameters when optional params are available
        var result = _methodsMetadata.TryGetMethod("OptionalParams", new[] { typeof(int), typeof(string) }, null, out var method);
        Assert.IsTrue(result);
        Assert.AreEqual(2, method.GetParameters().Length);
        
        // Should still match method with more parameters when explicitly provided
        result = _methodsMetadata.TryGetMethod("OptionalParams", new[] { typeof(int), typeof(string), typeof(int) }, null, out method);
        Assert.IsTrue(result);
        Assert.AreEqual(3, method.GetParameters().Length);
    }

    [TestMethod]
    public void TryGetMethod_Context_PreferContextSpecific()
    {
        // Should prefer non-aggregation method in normal context
        var result = _methodsMetadata.TryGetMethod("ContextMethod", new[] { typeof(int) }, null, out var method);
        Assert.IsTrue(result);
        Assert.IsFalse(Attribute.IsDefined(method, typeof(AggregationMethodAttribute)));
        
        // Should choose aggregation method when parameters match
        result = _methodsMetadata.TryGetMethod("ContextMethod", new[] { typeof(string), typeof(int) }, null, out method);
        Assert.IsTrue(result);
        Assert.IsTrue(Attribute.IsDefined(method, typeof(AggregationMethodAttribute)));
    }

    [TestMethod]
    public void TryGetMethod_AmbiguousCall_ShouldResolveCorrectly()
    {
        // Test with ambiguous parameter types
        var result = _methodsMetadata.TryGetMethod("MixedParams", new[] { typeof(int), typeof(MoreSpecific) }, null, out var method);
        Assert.IsTrue(result);
        var parameters = method.GetParameters();
        // Should choose most specific overload
        Assert.AreEqual(typeof(int), parameters[0].ParameterType);
        Assert.AreEqual(typeof(Specific), parameters[1].ParameterType);
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