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
        // Built-in type tests
        public void BoolMethod(bool x) { }
        public void ShortMethod(short x) { }
        public void IntMethod(int x) { }
        public void LongMethod(long x) { }
        
        // DateTime/TimeSpan tests
        public void DateTimeOffsetMethod(DateTimeOffset date) { }
        public void DateTimeMethod(DateTime date) { }
        public void TimeSpanMethod(TimeSpan span) { }
        
        // String and decimal tests
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
        // bool can only be assigned to bool
        Assert.IsTrue(_methodsMetadata.TryGetMethod("BoolMethod", new[] { typeof(bool) }, _entityType, out _), "bool -> bool should work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("ShortMethod", new[] { typeof(bool) }, _entityType, out _), "bool -> short should not work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("IntMethod", new[] { typeof(bool) }, _entityType, out _), "bool -> int should not work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("LongMethod", new[] { typeof(bool) }, _entityType, out _), "bool -> long should not work");
    }

    [TestMethod]
    public void TryGetMethod_Short_Compatibility()
    {
        // short can only be assigned to short
        Assert.IsTrue(_methodsMetadata.TryGetMethod("ShortMethod", new[] { typeof(short) }, _entityType, out _), "short -> short should work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("BoolMethod", new[] { typeof(short) }, _entityType, out _), "short -> bool should not work");
        Assert.IsTrue(_methodsMetadata.TryGetMethod("IntMethod", new[] { typeof(short) }, _entityType, out _), "short -> int should work");
        Assert.IsTrue(_methodsMetadata.TryGetMethod("LongMethod", new[] { typeof(short) }, _entityType, out _), "short -> long should work");
    }

    [TestMethod]
    public void TryGetMethod_Int_Compatibility()
    {
        // int can be assigned to int and short
        Assert.IsTrue(_methodsMetadata.TryGetMethod("IntMethod", new[] { typeof(int) }, _entityType, out _), "int -> int should work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("ShortMethod", new[] { typeof(int) }, _entityType, out _), "int -> short should not work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("BoolMethod", new[] { typeof(int) }, _entityType, out _), "int -> bool should not work");
        Assert.IsTrue(_methodsMetadata.TryGetMethod("LongMethod", new[] { typeof(int) }, _entityType, out _), "int -> long should work");
    }

    [TestMethod]
    public void TryGetMethod_Long_Compatibility()
    {
        // long can be assigned to long, int, and short
        Assert.IsTrue(_methodsMetadata.TryGetMethod("LongMethod", new[] { typeof(long) }, _entityType, out _), "long -> long should work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("IntMethod", new[] { typeof(long) }, _entityType, out _), "long -> int should not work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("ShortMethod", new[] { typeof(long) }, _entityType, out _), "long -> short should not work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("BoolMethod", new[] { typeof(long) }, _entityType, out _), "long -> bool should not work");
    }

    [TestMethod]
    public void TryGetMethod_DateTimeTypes_Compatibility()
    {
        // DateTimeOffset, DateTime, and TimeSpan are strict about their types
        Assert.IsTrue(_methodsMetadata.TryGetMethod("DateTimeOffsetMethod", new[] { typeof(DateTimeOffset) }, _entityType, out _));
        Assert.IsFalse(_methodsMetadata.TryGetMethod("DateTimeOffsetMethod", new[] { typeof(DateTime) }, _entityType, out _));
        
        Assert.IsTrue(_methodsMetadata.TryGetMethod("DateTimeMethod", new[] { typeof(DateTime) }, _entityType, out _));
        Assert.IsFalse(_methodsMetadata.TryGetMethod("DateTimeMethod", new[] { typeof(DateTimeOffset) }, _entityType, out _));
        
        Assert.IsTrue(_methodsMetadata.TryGetMethod("TimeSpanMethod", new[] { typeof(TimeSpan) }, _entityType, out _));
        Assert.IsFalse(_methodsMetadata.TryGetMethod("TimeSpanMethod", new[] { typeof(DateTime) }, _entityType, out _));
    }

    [TestMethod]
    public void TryGetMethod_StringAndDecimal_StrictTypeMatching()
    {
        // String and decimal should only match their exact types
        Assert.IsTrue(_methodsMetadata.TryGetMethod("StringMethod", new[] { typeof(string) }, _entityType, out _), "string -> string should work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("StringMethod", new[] { typeof(object) }, _entityType, out _), "object -> string should not work");
        
        Assert.IsTrue(_methodsMetadata.TryGetMethod("DecimalMethod", new[] { typeof(decimal) }, _entityType, out _), "decimal -> decimal should work");
        Assert.IsFalse(_methodsMetadata.TryGetMethod("DecimalMethod", new[] { typeof(double) }, _entityType, out _), "double -> decimal should not work");
    }

    [TestMethod]
    public void TryGetMethod_InheritanceBasedCompatibility()
    {
        // Test inheritance-based compatibility (this should work regardless of TypeCompatibilityTable)
        Assert.IsTrue(_methodsMetadata.TryGetMethod("BaseClassMethod", new[] { typeof(Animal) }, _entityType, out _), "Animal -> Animal should work");
        Assert.IsTrue(_methodsMetadata.TryGetMethod("BaseClassMethod", new[] { typeof(Dog) }, _entityType, out _), "Dog -> Animal should work");
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