using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadataTypeCompatibilityTests
{
    private class TestClass
    {
        public void BoolMethod(bool x) { }
        public void ShortMethod(short x) { }
        public void IntMethod(int x) { }
        public void LongMethod(long x) { }
        
        public void DateTimeOffsetMethod(DateTimeOffset date) { }
        public void DateTimeMethod(DateTime date) { }
        public void TimeSpanMethod(TimeSpan span) { }
        
        public void StringMethod(string text) { }
        public void DecimalMethod(decimal value) { }
        
        // Method for inheritance tests
        public void BaseClassMethod(Animal animal) { }
    }
    
    private class Animal { }
    private class Dog : Animal { }

    private MethodsMetadata _methodsMetadata;
    private Type _entityType;

    [TestInitialize]
    public void Initialize()
    {
        _methodsMetadata = new TestMethodsMetadata();
        _entityType = null;
    }

    [TestMethod]
    public void TryGetMethod_Bool_Compatibility()
    {
        Assert.IsTrue(_methodsMetadata.TryGetMethod("BoolMethod", [typeof(bool)], _entityType, out _), "bool -> bool should work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("ShortMethod", [typeof(bool)], _entityType, out _), "bool -> short should not work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("IntMethod", [typeof(bool)], _entityType, out _), "bool -> int should not work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("LongMethod", [typeof(bool)], _entityType, out _), "bool -> long should not work");
    }

    [TestMethod]
    public void TryGetMethod_Short_Compatibility()
    {
        Assert.IsTrue(_methodsMetadata.TryGetMethod("ShortMethod", [typeof(short)], _entityType, out _), "short -> short should work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("BoolMethod", [typeof(short)], _entityType, out _), "short -> bool should not work");
        Assert.IsTrue(_methodsMetadata.TryGetMethod("IntMethod", [typeof(short)], _entityType, out _), "short -> int should work");
        Assert.IsTrue(_methodsMetadata.TryGetMethod("LongMethod", [typeof(short)], _entityType, out _), "short -> long should work");
    }

    [TestMethod]
    public void TryGetMethod_Int_Compatibility()
    {
        Assert.IsTrue(_methodsMetadata.TryGetMethod("IntMethod", [typeof(int)], _entityType, out _), "int -> int should work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("ShortMethod", [typeof(int)], _entityType, out _), "int -> short should not work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("BoolMethod", [typeof(int)], _entityType, out _), "int -> bool should not work");
        Assert.IsTrue(_methodsMetadata.TryGetMethod("LongMethod", [typeof(int)], _entityType, out _), "int -> long should work");
    }

    [TestMethod]
    public void TryGetMethod_Long_Compatibility()
    {
        Assert.IsTrue(_methodsMetadata.TryGetMethod("LongMethod", [typeof(long)], _entityType, out _), "long -> long should work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("IntMethod", [typeof(long)], _entityType, out _), "long -> int should not work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("ShortMethod", [typeof(long)], _entityType, out _), "long -> short should not work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("BoolMethod", [typeof(long)], _entityType, out _), "long -> bool should not work");
    }

    [TestMethod]
    public void TryGetMethod_DateTimeTypes_Compatibility()
    {
        Assert.IsTrue(_methodsMetadata.TryGetMethod("DateTimeOffsetMethod", [typeof(DateTimeOffset)], _entityType, out _));
        Assert.IsFalse(_methodsMetadata.TryGetMethod("DateTimeOffsetMethod", [typeof(DateTime)], _entityType, out _));
        
        Assert.IsTrue(_methodsMetadata.TryGetMethod("DateTimeMethod", [typeof(DateTime)], _entityType, out _));
        Assert.IsFalse(_methodsMetadata.TryGetMethod("DateTimeMethod", [typeof(DateTimeOffset)], _entityType, out _));
        
        Assert.IsTrue(_methodsMetadata.TryGetMethod("TimeSpanMethod", [typeof(TimeSpan)], _entityType, out _));
        Assert.IsFalse(_methodsMetadata.TryGetMethod("TimeSpanMethod", [typeof(DateTime)], _entityType, out _));
    }

    [TestMethod]
    public void TryGetMethod_StringAndDecimal_StrictTypeMatching()
    {
        Assert.IsTrue(_methodsMetadata.TryGetMethod("StringMethod", [typeof(string)], _entityType, out _), "string -> string should work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("StringMethod", [typeof(object)], _entityType, out _), "object -> string should not work");
        
        Assert.IsTrue(_methodsMetadata.TryGetMethod("DecimalMethod", [typeof(decimal)], _entityType, out _), "decimal -> decimal should work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("DecimalMethod", [typeof(double)], _entityType, out _), "double -> decimal should not work");
    }

    [TestMethod]
    public void TryGetMethod_InheritanceBasedCompatibility()
    {
        Assert.IsTrue(_methodsMetadata.TryGetMethod("BaseClassMethod", [typeof(Animal)], _entityType, out _), "Animal -> Animal should work");
        Assert.IsTrue(_methodsMetadata.TryGetMethod("BaseClassMethod", [typeof(Dog)], _entityType, out _), "Dog -> Animal should work");
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