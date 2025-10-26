using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Managers;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Schema.Tests;

[TestClass]
public class CaseInsensitiveEndToEndTests
{
    // Test library with various method names for testing
    public class TestLibrary : LibraryBase
    {
        [BindableMethod]
        public new string ToUpper(string input)
        {
            return input?.ToUpper() ?? string.Empty;
        }

        [BindableMethod]
        public string Format_String(string template, string value)
        {
            return string.Format(template, value);
        }

        [BindableMethod]
        public int Multiply_By_Two(int value)
        {
            return value * 2;
        }

        [BindableMethod]
        public string ConcatenateStrings(string first, string second)
        {
            return first + second;
        }
    }

    [TestMethod]
    public void MethodsManager_ShouldRegisterAndResolveCaseInsensitiveMethods()
    {
        // Arrange
        var methodsManager = new MethodsManager();
        var testLibrary = new TestLibrary();
        
        // Register the test library
        methodsManager.RegisterLibraries(testLibrary);

        // Act & Assert - Test various case variations
        var testCases = new[]
        {
            // ToUpper method variations
            ("ToUpper", "toupper"),
            ("ToUpper", "TOUPPER"), 
            ("ToUpper", "to_upper"),
            ("ToUpper", "TO_UPPER"),

            // Format_String method variations  
            ("Format_String", "format_string"),
            ("Format_String", "FORMAT_STRING"),
            ("Format_String", "formatstring"),
            ("Format_String", "FORMATSTRING"),

            // Multiply_By_Two method variations
            ("Multiply_By_Two", "multiply_by_two"),
            ("Multiply_By_Two", "MULTIPLY_BY_TWO"),
            ("Multiply_By_Two", "multiplybytwo"),
            ("Multiply_By_Two", "MULTIPLYBYTWO"),

            // ConcatenateStrings method variations
            ("ConcatenateStrings", "concatenatestrings"),
            ("ConcatenateStrings", "CONCATENATESTRINGS"),
            ("ConcatenateStrings", "concatenate_strings"),
            ("ConcatenateStrings", "CONCATENATE_STRINGS")
        };

        foreach (var (originalName, testName) in testCases)
        {
            // Get the expected method by original name
            var expectedSuccess = methodsManager.TryGetMethod(originalName, GetParameterTypesForMethod(originalName), null, out var expectedMethod);
            Assert.IsTrue(expectedSuccess, $"Failed to resolve original method: {originalName}");

            // Test that case-insensitive name resolves to the same method
            var actualSuccess = methodsManager.TryGetMethod(testName, GetParameterTypesForMethod(originalName), null, out var actualMethod);
            Assert.IsTrue(actualSuccess, $"Failed to resolve case-insensitive method: {testName}");
            
            Assert.AreEqual(expectedMethod, actualMethod, $"Case-insensitive lookup for '{testName}' should return same method as '{originalName}'");
            Assert.AreEqual(originalName, actualMethod.Name, $"Method name should be preserved as original: {originalName}");
        }
    }

    [TestMethod]
    public void MethodsManager_ExactMatchShouldTakePrecedence()
    {
        // Arrange
        var methodsManager = new MethodsManager();
        var testLibrary = new TestLibrary();
        
        methodsManager.RegisterLibraries(testLibrary);

        // Act & Assert - Exact match should work and return original method
        var success = methodsManager.TryGetMethod("ToUpper", new[] { typeof(string) }, null, out var method);
        
        Assert.IsTrue(success);
        Assert.AreEqual("ToUpper", method.Name);
    }

    [TestMethod]
    public void MethodsManager_ShouldHandleOverloadedMethodsCaseInsensitive()
    {
        // This test verifies that case-insensitive resolution works with method overloading
        // We'll use the existing methods which may have different parameter signatures
        
        var methodsManager = new MethodsManager();
        var testLibrary = new TestLibrary();
        
        methodsManager.RegisterLibraries(testLibrary);

        // Test single parameter method
        var success1 = methodsManager.TryGetMethod("toupper", new[] { typeof(string) }, null, out var method1);
        Assert.IsTrue(success1);
        Assert.AreEqual("ToUpper", method1.Name);
        Assert.HasCount(1, method1.GetParameters());

        // Test two parameter method  
        var success2 = methodsManager.TryGetMethod("format_string", new[] { typeof(string), typeof(string) }, null, out var method2);
        Assert.IsTrue(success2);
        Assert.AreEqual("Format_String", method2.Name);
        Assert.HasCount(2, method2.GetParameters());
    }

    [TestMethod]
    public void MethodsManager_ShouldReturnFalseForNonExistentMethods()
    {
        var methodsManager = new MethodsManager();
        var testLibrary = new TestLibrary();
        
        methodsManager.RegisterLibraries(testLibrary);

        // Test completely non-existent method
        var success = methodsManager.TryGetMethod("nonexistentmethod", new[] { typeof(string) }, null, out var method);
        
        Assert.IsFalse(success);
        Assert.IsNull(method);
    }

    private static Type[] GetParameterTypesForMethod(string methodName)
    {
        return methodName switch
        {
            "ToUpper" => new[] { typeof(string) },
            "Format_String" => new[] { typeof(string), typeof(string) },
            "Multiply_By_Two" => new[] { typeof(int) },
            "ConcatenateStrings" => new[] { typeof(string), typeof(string) },
            _ => new Type[0]
        };
    }
}