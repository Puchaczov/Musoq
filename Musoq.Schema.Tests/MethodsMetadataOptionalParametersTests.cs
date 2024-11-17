using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadataOptionalParametersTests
{
    private class TestClass
    {
        public void SingleOptional(int x = 42) { }
        public void TwoOptional(int x = 1, int y = 2) { }
        public void MixedOptional(int required, string optional = "default") { }
        
        public void Overloaded(int x) { }
        public void Overloaded(int x, int y = 10) { }
        public void Overloaded(int x, string y = "default") { }
        
        public void AllOptional(int a = 1, int b = 2, int c = 3) { }
        
        public void MixedTypes(int required, string optional1 = "test", int? optional2 = null) { }
        public void NullableParameters(int? x = null, string y = null) { }
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
    public void TryGetMethod_SingleOptionalParameter_AllCombinations()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("SingleOptional", [typeof(int)], _entityType, out var withParam),
            "Should resolve with parameter"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("SingleOptional", [], _entityType, out var withoutParam),
            "Should resolve without parameter"
        );
    }

    [TestMethod]
    public void TryGetMethod_TwoOptionalParameters_AllCombinations()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("TwoOptional", [typeof(int), typeof(int)], _entityType, out _),
            "Should resolve with both parameters"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("TwoOptional", [typeof(int)], _entityType, out _),
            "Should resolve with first parameter only"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("TwoOptional", [], _entityType, out _),
            "Should resolve with no parameters"
        );
    }

    [TestMethod]
    public void TryGetMethod_MixedOptionalRequired_Parameters()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedOptional", [typeof(int)], _entityType, out _),
            "Should resolve with required parameter only"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedOptional", [typeof(int), typeof(string)], _entityType, out _),
            "Should resolve with all parameters"
        );
        
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("MixedOptional", [], _entityType, out _),
            "Should not resolve without required parameter"
        );
    }

    [TestMethod]
    public void TryGetMethod_OverloadedWithOptional_Resolution()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Overloaded", [typeof(int)], _entityType, out var method1),
            "Should resolve single int parameter"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Overloaded", [typeof(int), typeof(int)], _entityType, out var method2),
            "Should resolve two int parameters"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Overloaded", [typeof(int), typeof(string)], _entityType, out var method3),
            "Should resolve int and string parameters"
        );

        Assert.AreNotEqual(method1, method2, "Should resolve to different method overloads");
        Assert.AreNotEqual(method2, method3, "Should resolve to different method overloads");
        Assert.AreNotEqual(method1, method3, "Should resolve to different method overloads");
    }

    [TestMethod]
    public void TryGetMethod_AllOptionalParameters_AllCombinations()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("AllOptional", [], _entityType, out _),
            "Should resolve with no parameters"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("AllOptional", [typeof(int)], _entityType, out _),
            "Should resolve with one parameter"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("AllOptional", [typeof(int), typeof(int)], _entityType, out _),
            "Should resolve with two parameters"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("AllOptional", [typeof(int), typeof(int), typeof(int)], _entityType, out _),
            "Should resolve with all parameters"
        );
    }

    [TestMethod]
    public void TryGetMethod_MixedTypesOptional_AllCombinations()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedTypes", [typeof(int)], _entityType, out _),
            "Should resolve with required parameter only"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedTypes", [typeof(int), typeof(string)], _entityType, out _),
            "Should resolve with required and first optional"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedTypes", [typeof(int), typeof(string), typeof(int?)], _entityType, out _),
            "Should resolve with all parameters"
        );

        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("MixedTypes", [typeof(int), typeof(int?)], _entityType, out _),
            "Should not resolve with wrong parameter type"
        );
    }

    [TestMethod]
    public void TryGetMethod_NullableParameters_AllCombinations()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableParameters", [], _entityType, out _),
            "Should resolve with no parameters"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableParameters", [typeof(int?)], _entityType, out _),
            "Should resolve with nullable int"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableParameters", [typeof(int?)], _entityType, out _),
            "Should resolve with int"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableParameters", [typeof(int?), typeof(string)], _entityType, out _),
            "Should resolve with all parameters"
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