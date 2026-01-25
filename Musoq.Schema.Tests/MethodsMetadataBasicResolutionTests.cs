using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class BasicMethodResolutionTests
{
    private Type _entityType;

    private MethodsMetadata _methodsMetadata;

    [TestInitialize]
    public void Initialize()
    {
        _methodsMetadata = new TestMethodsMetadata();
        _entityType = null;
    }

    [TestMethod]
    public void TryGetMethod_NoParameters_ShouldResolveCorrectly()
    {
        var types = Array.Empty<Type>();

        var success = _methodsMetadata.TryGetMethod("NoParameters", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("NoParameters", method.Name);
        Assert.IsEmpty(method.GetParameters());
    }

    [TestMethod]
    public void TryGetMethod_SingleParameter_ShouldResolveCorrectly()
    {
        var types = new[] { typeof(int) };

        var success = _methodsMetadata.TryGetMethod("SingleParameter", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("SingleParameter", method.Name);
        Assert.HasCount(1, method.GetParameters());
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_TwoParameters_ShouldResolveCorrectly()
    {
        var types = new[] { typeof(int), typeof(string) };

        var success = _methodsMetadata.TryGetMethod("TwoParameters", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("TwoParameters", method.Name);
        Assert.HasCount(2, method.GetParameters());
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
        Assert.AreEqual(typeof(string), method.GetParameters()[1].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_Overloaded_ShouldResolveCorrectIntOverload()
    {
        var types = new[] { typeof(int) };

        var success = _methodsMetadata.TryGetMethod("Overloaded", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("Overloaded", method.Name);
        Assert.HasCount(1, method.GetParameters());
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_Overloaded_ShouldResolveCorrectStringOverload()
    {
        var types = new[] { typeof(string) };

        var success = _methodsMetadata.TryGetMethod("Overloaded", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("Overloaded", method.Name);
        Assert.HasCount(1, method.GetParameters());
        Assert.AreEqual(typeof(string), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_NonExistentMethod_ShouldReturnFalse()
    {
        var types = new[] { typeof(int) };

        var success = _methodsMetadata.TryGetMethod("NonExistentMethod", types, _entityType, out var method);

        Assert.IsFalse(success);
        Assert.IsNull(method);
    }

    [TestMethod]
    public void TryGetMethod_WrongParameterTypes_ShouldReturnFalse()
    {
        var types = new[] { typeof(DateTime) };

        var success = _methodsMetadata.TryGetMethod("SingleParameter", types, _entityType, out var method);

        Assert.IsFalse(success);
        Assert.IsNull(method);
    }

    private class TestClass
    {
        public void NoParameters()
        {
        }

        public void SingleParameter(int x)
        {
        }

        public void TwoParameters(int x, string y)
        {
        }

        public void Overloaded(int x)
        {
        }

        public void Overloaded(string x)
        {
        }

        public void Overloaded(int x, string y)
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
