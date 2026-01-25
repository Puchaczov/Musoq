using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadataMethodOrderingTests
{
    private MethodsMetadata _methodsMetadata;

    [TestInitialize]
    public void Initialize()
    {
        _methodsMetadata = new TestMethodsMetadata();
    }

    [TestMethod]
    public void TryGetMethod_NumericTypes_ExactMatch()
    {
        var result = _methodsMetadata.TryGetMethod("NumericMethod", [typeof(int)], null, out var method);
        Assert.IsTrue(result);
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_NumericTypes_ImplicitConversion()
    {
        var result = _methodsMetadata.TryGetMethod("NumericMethod", [typeof(short)], null, out var method);
        Assert.IsTrue(result);
        Assert.AreEqual(typeof(short), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_Generic_PreferNonGeneric()
    {
        var result = _methodsMetadata.TryGetMethod("GenericMethod", [typeof(int)], null, out var method);
        Assert.IsTrue(result);
        Assert.IsFalse(method.IsGenericMethod);
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);

        result = _methodsMetadata.TryGetMethod("GenericMethod", [typeof(DateTime)], null, out method);
        Assert.IsTrue(result);
        Assert.IsTrue(method.IsGenericMethod);
    }

    [TestMethod]
    public void TryGetMethod_MixedParams_MostSpecificMatch()
    {
        var result =
            _methodsMetadata.TryGetMethod("MixedParams", [typeof(int), typeof(Specific)], null, out var method);
        Assert.IsTrue(result);
        var parameters = method.GetParameters();
        Assert.AreEqual(typeof(int), parameters[0].ParameterType);
        Assert.AreEqual(typeof(Specific), parameters[1].ParameterType);

        result = _methodsMetadata.TryGetMethod("MixedParams", [typeof(int), typeof(Base)], null, out method);
        Assert.IsTrue(result);
        parameters = method.GetParameters();
        Assert.AreEqual(typeof(int), parameters[0].ParameterType);
        Assert.AreEqual(typeof(Base), parameters[1].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_OptionalParams_PreferFewerParameters()
    {
        var result =
            _methodsMetadata.TryGetMethod("OptionalParams", [typeof(int), typeof(string)], null, out var method);
        Assert.IsTrue(result);
        Assert.HasCount(2, method.GetParameters());

        result = _methodsMetadata.TryGetMethod("OptionalParams", [typeof(int), typeof(string), typeof(int)], null,
            out method);
        Assert.IsTrue(result);
        Assert.HasCount(3, method.GetParameters());
    }

    [TestMethod]
    public void TryGetMethod_Context_PreferContextSpecific()
    {
        var result = _methodsMetadata.TryGetMethod("ContextMethod", [typeof(int)], null, out var method);
        Assert.IsTrue(result);
        Assert.IsFalse(Attribute.IsDefined(method, typeof(AggregationMethodAttribute)));

        result = _methodsMetadata.TryGetMethod("ContextMethod", [typeof(string), typeof(int)], null, out method);
        Assert.IsTrue(result);
        Assert.IsTrue(Attribute.IsDefined(method, typeof(AggregationMethodAttribute)));
    }

    [TestMethod]
    public void TryGetMethod_AmbiguousCall_ShouldResolveCorrectly()
    {
        var result =
            _methodsMetadata.TryGetMethod("MixedParams", [typeof(int), typeof(MoreSpecific)], null, out var method);
        Assert.IsTrue(result);
        var parameters = method.GetParameters();

        Assert.AreEqual(typeof(int), parameters[0].ParameterType);
        Assert.AreEqual(typeof(Specific), parameters[1].ParameterType);
    }

    private interface IBase
    {
    }

    private interface ISpecific : IBase
    {
    }

    private class Base : IBase
    {
    }

    private class Specific : Base, ISpecific
    {
    }

    private class MoreSpecific : Specific
    {
    }

    private class TestClass
    {
        public void NumericMethod(int x)
        {
        }

        public void NumericMethod(long x)
        {
        }

        public void NumericMethod(short x)
        {
        }

        public string InheritanceMethod([InjectSpecificSource(typeof(IBase))] IBase x)
        {
            return "IBase";
        }

        public string InheritanceMethod([InjectSpecificSource(typeof(ISpecific))] ISpecific x)
        {
            return "ISpecific";
        }

        public string InheritanceMethod([InjectSpecificSource(typeof(Base))] Base x)
        {
            return "Base";
        }

        public string InheritanceMethod([InjectSpecificSource(typeof(Specific))] Specific x)
        {
            return "Specific";
        }

        public void GenericMethod<T>(T value)
        {
        }

        public void GenericMethod(int value)
        {
        }

        public void GenericMethod(string value)
        {
        }

        public string MixedParams(int x, Base b)
        {
            return "int,Base";
        }

        public string MixedParams(long x, Specific s)
        {
            return "long,Specific";
        }

        public string MixedParams(int x, Specific s)
        {
            return "int,Specific";
        }

        public string OptionalParams(int x, string s = "default")
        {
            return "optional";
        }

        // ReSharper disable once MethodOverloadWithOptionalParameter
        public string OptionalParams(int x, string s, int y = 42)
        {
            return "more_params";
        }

        [AggregationMethod]
        public void ContextMethod(string name, int value)
        {
        }

        public void ContextMethod(int value)
        {
        }
    }

    private class TestMethodsMetadata : MethodsMetadata
    {
        public TestMethodsMetadata()
        {
            var testClass = typeof(TestClass);
            foreach (var method in testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                                        BindingFlags.DeclaredOnly)) RegisterMethod(method);
        }

        private new void RegisterMethod(MethodInfo methodInfo)
        {
            base.RegisterMethod(methodInfo);
        }
    }
}
