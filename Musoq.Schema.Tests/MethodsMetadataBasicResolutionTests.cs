using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class BasicMethodResolutionTests
{
    private class TestClass
    {
        public void NoParameters() { }
        public void SingleParameter(int x) { }
        public void TwoParameters(int x, string y) { }
        public void Overloaded(int x) { }
        public void Overloaded(string x) { }
        public void Overloaded(int x, string y) { }
    }

    private MethodsMetadata _methodsMetadata;
    private Type _entityType;

    [TestInitialize]
    public void Initialize()
    {
        _methodsMetadata = new TestMethodsMetadata();
        _entityType = null;
    }

    [TestMethod]
    public void TryGetMethod_NoParameters_ShouldResolveCorrectly()
    {
        // Arrange
        var types = Array.Empty<Type>();

        // Act
        var success = _methodsMetadata.TryGetMethod("NoParameters", types, _entityType, out var method);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("NoParameters", method.Name);
        Assert.AreEqual(0, method.GetParameters().Length);
    }

    [TestMethod]
    public void TryGetMethod_SingleParameter_ShouldResolveCorrectly()
    {
        // Arrange
        var types = new[] { typeof(int) };

        // Act
        var success = _methodsMetadata.TryGetMethod("SingleParameter", types, _entityType, out var method);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("SingleParameter", method.Name);
        Assert.AreEqual(1, method.GetParameters().Length);
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_TwoParameters_ShouldResolveCorrectly()
    {
        // Arrange
        var types = new[] { typeof(int), typeof(string) };

        // Act
        var success = _methodsMetadata.TryGetMethod("TwoParameters", types, _entityType, out var method);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("TwoParameters", method.Name);
        Assert.AreEqual(2, method.GetParameters().Length);
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
        Assert.AreEqual(typeof(string), method.GetParameters()[1].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_Overloaded_ShouldResolveCorrectIntOverload()
    {
        // Arrange
        var types = new[] { typeof(int) };

        // Act
        var success = _methodsMetadata.TryGetMethod("Overloaded", types, _entityType, out var method);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("Overloaded", method.Name);
        Assert.AreEqual(1, method.GetParameters().Length);
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_Overloaded_ShouldResolveCorrectStringOverload()
    {
        // Arrange
        var types = new[] { typeof(string) };

        // Act
        var success = _methodsMetadata.TryGetMethod("Overloaded", types, _entityType, out var method);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("Overloaded", method.Name);
        Assert.AreEqual(1, method.GetParameters().Length);
        Assert.AreEqual(typeof(string), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_NonExistentMethod_ShouldReturnFalse()
    {
        // Arrange
        var types = new[] { typeof(int) };

        // Act
        var success = _methodsMetadata.TryGetMethod("NonExistentMethod", types, _entityType, out var method);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(method);
    }

    [TestMethod]
    public void TryGetMethod_WrongParameterTypes_ShouldReturnFalse()
    {
        // Arrange
        var types = new[] { typeof(DateTime) }; // Method expects int

        // Act
        var success = _methodsMetadata.TryGetMethod("SingleParameter", types, _entityType, out var method);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(method);
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

        public new void RegisterMethod(MethodInfo methodInfo)
        {
            base.RegisterMethod(methodInfo);
        }
    }
}