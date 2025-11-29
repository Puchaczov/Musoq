using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadataGenericResolutionTests
{
    private class TestClass
    {
        public void GenericMethod1<T>(T obj) { }
        
        public void GenericMethod2<T>(T[] objs) { }
        
        public void GenericMethod3<T>(IEnumerable<T> objs) { }
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
    public void TryGetMethod_GenericT_ShouldResolveCorrectly()
    {
        var types = new[]
        {
            typeof(string)
        };

        var success = _methodsMetadata.TryGetMethod("GenericMethod1", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("GenericMethod1", method.Name);
        Assert.HasCount(1, method.GetParameters());
    }

    [TestMethod]
    public void TryGetMethod_GenericTArray_ShouldResolveCorrectly()
    {
        var types = new[]
        {
            typeof(string[])
        };

        var success = _methodsMetadata.TryGetMethod("GenericMethod2", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("GenericMethod2", method.Name);
        Assert.HasCount(1, method.GetParameters());
    }

    [TestMethod]
    public void TryGetMethod_GenericEnumerableT_ShouldResolveCorrectly()
    {
        var types = new[]
        {
            typeof(IEnumerable<string>)
        };

        var success = _methodsMetadata.TryGetMethod("GenericMethod3", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("GenericMethod3", method.Name);
        Assert.HasCount(1, method.GetParameters());
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