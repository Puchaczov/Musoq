using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class CaseInsensitiveMethodResolutionTests
{
    private class TestClass
    {
        public void MyMethod() { }
        
        public void MyMethod(int value) { }
        
        public void My_Underscore_Method() { }
        
        public void UPPERCASE_METHOD() { }
        
        public void mixedCaseMethod() { }
        
        public void simple() { }
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

    private MethodsMetadata _methodsMetadata;
    private Type _entityType;

    [TestInitialize]
    public void Initialize()
    {
        _methodsMetadata = new TestMethodsMetadata();
        _entityType = null;
    }

    [TestMethod]
    public void MethodNameNormalizer_ShouldNormalizeDifferentCases()
    {
        Assert.AreEqual("mymethod", MethodNameNormalizer.Normalize("MyMethod"));
        Assert.AreEqual("mymethod", MethodNameNormalizer.Normalize("mymethod"));
        Assert.AreEqual("mymethod", MethodNameNormalizer.Normalize("MYMETHOD"));
        Assert.AreEqual("mymethod", MethodNameNormalizer.Normalize("my_method"));
        Assert.AreEqual("mymethod", MethodNameNormalizer.Normalize("MY_METHOD"));
        Assert.AreEqual("mymethod", MethodNameNormalizer.Normalize("My_Method"));
    }

    [TestMethod]
    public void MethodNameNormalizer_ShouldThrowOnNull()
    {
        Assert.ThrowsException<ArgumentNullException>(() => MethodNameNormalizer.Normalize(null));
    }

    [TestMethod]
    public void MethodNameNormalizer_ShouldThrowOnEmpty()
    {
        Assert.ThrowsException<ArgumentException>(() => MethodNameNormalizer.Normalize(""));
        Assert.ThrowsException<ArgumentException>(() => MethodNameNormalizer.Normalize("   "));
    }

    [TestMethod]
    public void TryGetMethod_ExactCase_ShouldResolveCorrectly()
    {
        var types = new Type[0];

        var success = _methodsMetadata.TryGetMethod("MyMethod", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("MyMethod", method.Name);
    }

    [TestMethod]
    public void TryGetMethod_LowerCase_ShouldResolveCorrectly()
    {
        var types = new Type[0];

        var success = _methodsMetadata.TryGetMethod("mymethod", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("MyMethod", method.Name); // Should return original method name
    }

    [TestMethod]
    public void TryGetMethod_UpperCase_ShouldResolveCorrectly()
    {
        var types = new Type[0];

        var success = _methodsMetadata.TryGetMethod("MYMETHOD", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("MyMethod", method.Name); // Should return original method name
    }

    [TestMethod]
    public void TryGetMethod_WithUnderscores_ShouldResolveCorrectly()
    {
        var types = new Type[0];

        var success = _methodsMetadata.TryGetMethod("my_method", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("MyMethod", method.Name); // Should return original method name
    }

    [TestMethod]
    public void TryGetMethod_Overloaded_LowerCase_ShouldResolveCorrectly()
    {
        var types = new[] { typeof(int) };

        var success = _methodsMetadata.TryGetMethod("mymethod", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("MyMethod", method.Name);
        Assert.AreEqual(1, method.GetParameters().Length);
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_UnderscoreMethodName_DifferentCases_ShouldResolveCorrectly()
    {
        var types = new Type[0];

        // Test the method originally named "My_Underscore_Method"
        var testCases = new[]
        {
            "my_underscore_method",
            "MY_UNDERSCORE_METHOD", 
            "myunderscoremethod",
            "MYUNDERSCOREMETHOD"
        };

        foreach (var testCase in testCases)
        {
            var success = _methodsMetadata.TryGetMethod(testCase, types, _entityType, out var method);

            Assert.IsTrue(success, $"Failed to resolve method for case: {testCase}");
            Assert.IsNotNull(method);
            Assert.AreEqual("My_Underscore_Method", method.Name);
        }
    }

    [TestMethod]
    public void TryGetMethod_UppercaseMethodName_DifferentCases_ShouldResolveCorrectly()
    {
        var types = new Type[0];

        // Test the method originally named "UPPERCASE_METHOD"
        var testCases = new[]
        {
            "uppercase_method",
            "UPPERCASE_METHOD",
            "uppercasemethod", 
            "UPPERCASEMETHOD"
        };

        foreach (var testCase in testCases)
        {
            var success = _methodsMetadata.TryGetMethod(testCase, types, _entityType, out var method);

            Assert.IsTrue(success, $"Failed to resolve method for case: {testCase}");
            Assert.IsNotNull(method);
            Assert.AreEqual("UPPERCASE_METHOD", method.Name);
        }
    }

    [TestMethod]
    public void TryGetMethod_MixedCaseMethodName_DifferentCases_ShouldResolveCorrectly()
    {
        var types = new Type[0];

        // Test the method originally named "mixedCaseMethod"
        var testCases = new[]
        {
            "mixedcasemethod",
            "MIXEDCASEMETHOD",
            "mixed_case_method",
            "MIXED_CASE_METHOD"
        };

        foreach (var testCase in testCases)
        {
            var success = _methodsMetadata.TryGetMethod(testCase, types, _entityType, out var method);

            Assert.IsTrue(success, $"Failed to resolve method for case: {testCase}");
            Assert.IsNotNull(method);
            Assert.AreEqual("mixedCaseMethod", method.Name);
        }
    }

    [TestMethod]
    public void TryGetMethod_ExactMatchTakesPrecedence_OverCaseInsensitive()
    {
        var types = new Type[0];

        // If we have both "simple" (exact match) and could match other methods, 
        // exact match should take precedence
        var success = _methodsMetadata.TryGetMethod("simple", types, _entityType, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("simple", method.Name);
    }

    [TestMethod]
    public void TryGetMethod_NonExistentMethod_ShouldReturnFalse()
    {
        var types = new Type[0];

        var success = _methodsMetadata.TryGetMethod("nonexistentmethod", types, _entityType, out var method);

        Assert.IsFalse(success);
        Assert.IsNull(method);
    }

    [TestMethod]
    public void TryGetRawMethod_CaseInsensitive_ShouldResolveCorrectly()
    {
        var types = new Type[0];

        var success = _methodsMetadata.TryGetRawMethod("mymethod", types, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("MyMethod", method.Name);
    }

    [TestMethod]
    public void TryGetRawMethod_WithUnderscores_ShouldResolveCorrectly()
    {
        var types = new Type[0];

        var success = _methodsMetadata.TryGetRawMethod("my_method", types, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("MyMethod", method.Name);
    }
}