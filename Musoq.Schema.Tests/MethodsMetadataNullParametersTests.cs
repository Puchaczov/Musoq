using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadataNullableParametersTests
{
    private class TestClass
    {
        public void NullableInt(int? x) { }
        public void NullableDateTime(DateTime? dt) { }
        public void NullableDecimal(decimal? d) { }
        
        public void OverloadedInt(int x) { }
        public void OverloadedInt(int? x) { }
        
        public void StringMethod(string s) { }
        public void ObjectMethod(object o) { }
        
        public void MixedNullables(int? x, string y, DateTime? dt) { }
        
        public void OptionalNullable(int? x = null) { }
        public void OptionalNullableWithDefault(int? x = 42) { }
        
        public void GenericNullable<T>(T? x) where T : struct { }
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
    public void TryGetMethod_NullableValueTypes_WithNullType()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableInt", [typeof(NullNode.NullType)], _entityType, out var method),
            "Should accept null for nullable int"
        );
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableDateTime", [typeof(NullNode.NullType)], _entityType, out _),
            "Should accept null for nullable DateTime"
        );
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableDecimal", [typeof(NullNode.NullType)], _entityType, out _),
            "Should accept null for nullable decimal"
        );
    }

    [TestMethod]
    public void TryGetMethod_NullableValueTypes_WithActualTypes()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableInt", [typeof(int)], _entityType, out _),
            "Should accept int for nullable int"
        );
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableDateTime", [typeof(DateTime)], _entityType, out _),
            "Should accept DateTime for nullable DateTime"
        );
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableDecimal", [typeof(decimal)], _entityType, out _),
            "Should accept decimal for nullable decimal"
        );
    }

    [TestMethod]
    public void TryGetMethod_Overloaded_NullableAndNonNullable()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OverloadedInt", [typeof(int)], _entityType, out var nonNullable),
            "Should resolve non-nullable overload for int"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OverloadedInt", [typeof(NullNode.NullType)], _entityType, out var nullable),
            "Should resolve nullable overload for null"
        );
        
        Assert.AreNotEqual(nullable, nonNullable, "Should resolve to different overloads");
    }

    [TestMethod]
    public void TryGetMethod_ReferenceTypes_WithNull()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("StringMethod", [typeof(NullNode.NullType)], _entityType, out _),
            "String should accept null"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("ObjectMethod", [typeof(NullNode.NullType)], _entityType, out _),
            "Object should accept null"
        );
    }

    [TestMethod]
    public void TryGetMethod_MixedNullables_AllCombinations()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedNullables",
                [typeof(NullNode.NullType), typeof(string), typeof(DateTime)], 
                _entityType, 
                out _),
            "Should accept null for nullable int"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedNullables",
                [typeof(int), typeof(string), typeof(NullNode.NullType)], 
                _entityType, 
                out _),
            "Should accept null for nullable DateTime"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedNullables",
                [typeof(int), typeof(NullNode.NullType), typeof(DateTime)], 
                _entityType, 
                out _),
            "Should accept null for string"
        );
    }

    [TestMethod]
    public void TryGetMethod_OptionalNullable_AllCombinations()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalNullable", [], _entityType, out _),
            "Should work with no parameters (default null)"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalNullable", [typeof(NullNode.NullType)], _entityType, out _),
            "Should accept explicit null"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalNullable", [typeof(int)], _entityType, out _),
            "Should accept actual value"
        );
    }

    [TestMethod]
    public void TryGetMethod_OptionalNullableWithDefault_AllCombinations()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalNullableWithDefault", [], _entityType, out _),
            "Should work with no parameters (default value)"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalNullableWithDefault", [typeof(NullNode.NullType)], _entityType, out _),
            "Should accept explicit null"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalNullableWithDefault", [typeof(int)], _entityType, out _),
            "Should accept actual value"
        );
    }

    [TestMethod]
    public void TryGetMethod_GenericNullable_ValueTypes()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GenericNullable", [typeof(int?)], _entityType, out _),
            "Should accept nullable int"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GenericNullable", [typeof(DateTime?)], _entityType, out _),
            "Should accept nullable DateTime"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GenericNullable", [typeof(NullNode.NullType)], _entityType, out _),
            "Should accept null"
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