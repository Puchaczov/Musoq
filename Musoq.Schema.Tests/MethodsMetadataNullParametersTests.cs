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
        // Nullable value types
        public void NullableInt(int? x) { }
        public void NullableDateTime(DateTime? dt) { }
        public void NullableDecimal(decimal? d) { }
        
        // Methods accepting both nullable and non-nullable
        public void OverloadedInt(int x) { }
        public void OverloadedInt(int? x) { }
        
        // Reference types and null
        public void StringMethod(string s) { }
        public void ObjectMethod(object o) { }
        
        // Mixed nullable parameters
        public void MixedNullables(int? x, string y, DateTime? dt) { }
        
        // Optional nullable parameters
        public void OptionalNullable(int? x = null) { }
        public void OptionalNullableWithDefault(int? x = 42) { }
        
        // Generic with nullable constraints
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
        // Testing passing null to nullable value types
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableInt", new[] { typeof(NullNode.NullType) }, _entityType, out var method),
            "Should accept null for nullable int"
        );
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableDateTime", new[] { typeof(NullNode.NullType) }, _entityType, out _),
            "Should accept null for nullable DateTime"
        );
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableDecimal", new[] { typeof(NullNode.NullType) }, _entityType, out _),
            "Should accept null for nullable decimal"
        );
    }

    [TestMethod]
    public void TryGetMethod_NullableValueTypes_WithActualTypes()
    {
        // Testing passing actual values to nullable value types
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableInt", new[] { typeof(int) }, _entityType, out _),
            "Should accept int for nullable int"
        );
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableDateTime", new[] { typeof(DateTime) }, _entityType, out _),
            "Should accept DateTime for nullable DateTime"
        );
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableDecimal", new[] { typeof(decimal) }, _entityType, out _),
            "Should accept decimal for nullable decimal"
        );
    }

    [TestMethod]
    public void TryGetMethod_Overloaded_NullableAndNonNullable()
    {
        // When both nullable and non-nullable overloads exist
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OverloadedInt", new[] { typeof(int) }, _entityType, out var nonNullable),
            "Should resolve non-nullable overload for int"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OverloadedInt", new[] { typeof(NullNode.NullType) }, _entityType, out var nullable),
            "Should resolve nullable overload for null"
        );
        
        // Verify we got different methods
        Assert.AreNotEqual(nullable, nonNullable, "Should resolve to different overloads");
    }

    [TestMethod]
    public void TryGetMethod_ReferenceTypes_WithNull()
    {
        // Reference types should accept null
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("StringMethod", new[] { typeof(NullNode.NullType) }, _entityType, out _),
            "String should accept null"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("ObjectMethod", new[] { typeof(NullNode.NullType) }, _entityType, out _),
            "Object should accept null"
        );
    }

    [TestMethod]
    public void TryGetMethod_MixedNullables_AllCombinations()
    {
        // Test various combinations of null and non-null values
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedNullables", 
                new[] { typeof(NullNode.NullType), typeof(string), typeof(DateTime) }, 
                _entityType, 
                out _),
            "Should accept null for nullable int"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedNullables", 
                new[] { typeof(int), typeof(string), typeof(NullNode.NullType) }, 
                _entityType, 
                out _),
            "Should accept null for nullable DateTime"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MixedNullables", 
                new[] { typeof(int), typeof(NullNode.NullType), typeof(DateTime) }, 
                _entityType, 
                out _),
            "Should accept null for string"
        );
    }

    [TestMethod]
    public void TryGetMethod_OptionalNullable_AllCombinations()
    {
        // Test optional nullable parameters
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalNullable", Array.Empty<Type>(), _entityType, out _),
            "Should work with no parameters (default null)"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalNullable", new[] { typeof(NullNode.NullType) }, _entityType, out _),
            "Should accept explicit null"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalNullable", new[] { typeof(int) }, _entityType, out _),
            "Should accept actual value"
        );
    }

    [TestMethod]
    public void TryGetMethod_OptionalNullableWithDefault_AllCombinations()
    {
        // Test optional nullable parameters with non-null default
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalNullableWithDefault", Array.Empty<Type>(), _entityType, out _),
            "Should work with no parameters (default value)"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalNullableWithDefault", new[] { typeof(NullNode.NullType) }, _entityType, out _),
            "Should accept explicit null"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalNullableWithDefault", new[] { typeof(int) }, _entityType, out _),
            "Should accept actual value"
        );
    }

    [TestMethod]
    public void TryGetMethod_GenericNullable_ValueTypes()
    {
        // Test generic nullable parameters
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GenericNullable", new[] { typeof(int?) }, _entityType, out _),
            "Should accept nullable int"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GenericNullable", new[] { typeof(DateTime?) }, _entityType, out _),
            "Should accept nullable DateTime"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GenericNullable", new[] { typeof(NullNode.NullType) }, _entityType, out _),
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