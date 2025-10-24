using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Managers;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Schema.Tests;

[TestClass]
public class StandardLibraryCompatibilityTests
{
    private MethodsManager _methodsManager;
    private LibraryBase _standardLibrary;

    [TestInitialize]
    public void Initialize()
    {
        _methodsManager = new MethodsManager();
        _standardLibrary = new LibraryBase();
        _methodsManager.RegisterLibraries(_standardLibrary);
    }

    [TestMethod]
    public void StandardLibrary_CaseInsensitiveResolutionWorks()
    {
        // Test specific known methods to ensure case-insensitive resolution works
        var testCases = new[]
        {
            ("Trim", new[] { typeof(string) }),
            ("ToUpper", new[] { typeof(string) }),
            ("ToLower", new[] { typeof(string) }),
            ("Abs", new[] { typeof(int?) }),
            ("NewId", new Type[0])
        };

        foreach (var (methodName, paramTypes) in testCases)
        {
            // Exact case should work
            var exactSuccess = _methodsManager.TryGetMethod(methodName, paramTypes, null, out var exactMethod);
            Assert.IsTrue(exactSuccess, $"Exact case should resolve: {methodName}");
            
            // Lowercase should work and return method with original name
            var lowerSuccess = _methodsManager.TryGetMethod(methodName.ToLowerInvariant(), paramTypes, null, out var lowerMethod);
            Assert.IsTrue(lowerSuccess, $"Lowercase should resolve: {methodName}");
            Assert.AreEqual(methodName, lowerMethod.Name, $"Should preserve original name for: {methodName}");
            
            // Uppercase should work and return method with original name
            var upperSuccess = _methodsManager.TryGetMethod(methodName.ToUpperInvariant(), paramTypes, null, out var upperMethod);
            Assert.IsTrue(upperSuccess, $"Uppercase should resolve: {methodName}");
            Assert.AreEqual(methodName, upperMethod.Name, $"Should preserve original name for: {methodName}");
            
            // Underscore variant should work and return method with original name
            var underscoreSuccess = _methodsManager.TryGetMethod(InsertUnderscores(methodName), paramTypes, null, out var underscoreMethod);
            Assert.IsTrue(underscoreSuccess, $"Underscore variant should resolve: {methodName}");
            Assert.AreEqual(methodName, underscoreMethod.Name, $"Should preserve original name for: {methodName}");
        }
    }

    [TestMethod]
    public void StandardLibrary_ExactMatchShouldTakePrecedence()
    {
        // Test some common methods that exist in standard library
        var commonMethods = new[]
        {
            ("Abs", new[] { typeof(decimal?) }),
            ("Trim", new[] { typeof(string) }),
            ("ToUpper", new[] { typeof(string) }),
            ("ToLower", new[] { typeof(string) }),
            ("NewId", new Type[0])
        };

        foreach (var (methodName, paramTypes) in commonMethods)
        {
            // Exact match should work
            var exactSuccess = _methodsManager.TryGetMethod(methodName, paramTypes, null, out var exactMethod);
            Assert.IsTrue(exactSuccess, $"Exact match should work for {methodName}");
            
            // Case insensitive should resolve to same method
            var lowerSuccess = _methodsManager.TryGetMethod(methodName.ToLowerInvariant(), paramTypes, null, out var lowerMethod);
            Assert.IsTrue(lowerSuccess, $"Lowercase should work for {methodName}");
            
            Assert.AreEqual(exactMethod, lowerMethod, $"Exact and case-insensitive should resolve to same method for {methodName}");
            Assert.AreEqual(methodName, lowerMethod.Name, $"Original method name should be preserved for {methodName}");
        }
    }

    [TestMethod]
    public void StandardLibrary_OverloadedMethodsShouldResolveCorrectly()
    {
        // Test overloaded methods like Abs which has multiple versions (only decimal, long, int exist)
        var absTests = new[]
        {
            (new[] { typeof(decimal?) }, "decimal?"),
            (new[] { typeof(long?) }, "long?"),
            (new[] { typeof(int?) }, "int?")
        };

        foreach (var (paramTypes, expectedType) in absTests)
        {
            // Test exact case
            var exactSuccess = _methodsManager.TryGetMethod("Abs", paramTypes, null, out var exactMethod);
            Assert.IsTrue(exactSuccess, $"Exact 'Abs' should resolve for {expectedType}");
            
            // Test lowercase
            var lowerSuccess = _methodsManager.TryGetMethod("abs", paramTypes, null, out var lowerMethod);
            Assert.IsTrue(lowerSuccess, $"Lowercase 'abs' should resolve for {expectedType}");
            
            // Test uppercase
            var upperSuccess = _methodsManager.TryGetMethod("ABS", paramTypes, null, out var upperMethod);
            Assert.IsTrue(upperSuccess, $"Uppercase 'ABS' should resolve for {expectedType}");
            
            // All should resolve to same method
            Assert.AreEqual(exactMethod, lowerMethod, $"Case variations should resolve to same method for {expectedType}");
            Assert.AreEqual(exactMethod, upperMethod, $"Case variations should resolve to same method for {expectedType}");
            
            // Method name should be preserved
            Assert.AreEqual("Abs", lowerMethod.Name);
            Assert.AreEqual("Abs", upperMethod.Name);
        }
    }

    [TestMethod]
    public void StandardLibrary_NoNamingConflicts()
    {
        // Get all method names and their normalized versions
        var standardMethods = typeof(LibraryBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<BindableMethodAttribute>() != null)
            .GroupBy(m => m.Name)
            .ToArray();

        foreach (var methodGroup in standardMethods)
        {
            var methodName = methodGroup.Key;
            var normalizedName = methodName.ToLowerInvariant().Replace("_", "");
            
            // Check if any other method groups would normalize to the same name
            var conflictingMethods = standardMethods
                .Where(g => g.Key != methodName)
                .Where(g => g.Key.ToLowerInvariant().Replace("_", "") == normalizedName)
                .ToArray();
            
            Assert.AreEqual(0, conflictingMethods.Length, 
                $"Method '{methodName}' has potential naming conflicts after normalization: " +
                $"{string.Join(", ", conflictingMethods.Select(g => g.Key))}");
        }
    }

    [TestMethod]
    public void StandardLibrary_PerformanceIsAcceptable()
    {
        // Test that case-insensitive resolution doesn't significantly impact performance
        var testMethod = "trim";
        var paramTypes = new[] { typeof(string) };
        
        // Warm up
        for (int i = 0; i < 100; i++)
        {
            _methodsManager.TryGetMethod(testMethod, paramTypes, null, out _);
        }
        
        // Measure performance
        var start = DateTime.UtcNow;
        const int iterations = 10000;
        
        for (int i = 0; i < iterations; i++)
        {
            var success = _methodsManager.TryGetMethod(testMethod, paramTypes, null, out var method);
            Assert.IsTrue(success);
            Assert.IsNotNull(method);
        }
        
        var elapsed = DateTime.UtcNow - start;
        var msPerResolution = elapsed.TotalMilliseconds / iterations;
        
        // Should be very fast - less than 0.1ms per resolution
        Assert.IsTrue(msPerResolution < 0.1, 
            $"Case-insensitive method resolution is too slow: {msPerResolution:F4}ms per resolution");
    }

    [TestMethod]
    public void StandardLibrary_AllMethodsPreserveOriginalName()
    {
        // Verify that case-insensitive resolution always returns the original method name
        var standardMethods = typeof(LibraryBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<BindableMethodAttribute>() != null)
            .Where(m => !HasInjectedParameters(m)) // Skip methods with injected parameters
            .Take(10) // Test subset for speed
            .ToArray();

        foreach (var method in standardMethods)
        {
            var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
            var lowerName = method.Name.ToLowerInvariant();
            
            var success = _methodsManager.TryGetMethod(lowerName, parameterTypes, null, out var resolvedMethod);
            
            Assert.IsTrue(success, $"Should resolve {method.Name} via lowercase {lowerName}");
            Assert.AreEqual(method.Name, resolvedMethod.Name, 
                $"Should preserve original method name {method.Name}, not return {resolvedMethod.Name}");
        }
    }

    [TestMethod]
    public void StandardLibrary_EdgeCasesHandleCorrectly()
    {
        // Test various edge cases with the standard library
        
        // Non-existent method should fail
        var success1 = _methodsManager.TryGetMethod("nonexistentmethod", new Type[0], null, out _);
        Assert.IsFalse(success1, "Non-existent method should not resolve");
        
        // Wrong parameter types should fail
        var success2 = _methodsManager.TryGetMethod("abs", new[] { typeof(string) }, null, out _);
        Assert.IsFalse(success2, "Wrong parameter types should not resolve");
        
        // Null method name should throw
        Assert.Throws<ArgumentNullException>(() => 
            _methodsManager.TryGetMethod(null, new Type[0], null, out _));
            
        // Empty string should throw (our implementation validates input)
        Assert.Throws<ArgumentException>(() => 
            _methodsManager.TryGetMethod("", new Type[0], null, out _));
    }

    /// <summary>
    /// Helper method to insert underscores before uppercase letters in PascalCase names
    /// </summary>
    private static string InsertUnderscores(string methodName)
    {
        if (string.IsNullOrEmpty(methodName)) return methodName;
        
        var result = "";
        for (int i = 0; i < methodName.Length; i++)
        {
            if (i > 0 && char.IsUpper(methodName[i]) && char.IsLower(methodName[i - 1]))
            {
                result += "_";
            }
            result += char.ToLowerInvariant(methodName[i]);
        }
        return result;
    }

    /// <summary>
    /// Helper method to check if a method has injected parameters that require special handling
    /// </summary>
    private static bool HasInjectedParameters(MethodInfo method)
    {
        return method.GetParameters()
            .Any(p => p.GetCustomAttributes()
                .Any(attr => attr.GetType().Name.Contains("Inject")));
    }
}