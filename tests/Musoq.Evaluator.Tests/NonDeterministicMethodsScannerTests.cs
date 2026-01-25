using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Unit tests for <see cref="NonDeterministicMethodsScanner" />.
///     Tests verify that the scanner correctly identifies methods marked with
///     both [BindableMethod] and [NonDeterministic] attributes.
/// </summary>
[TestClass]
public class NonDeterministicMethodsScannerTests
{
    #region Case Insensitivity Tests

    [TestMethod]
    public void ScanForNonDeterministicMethods_ReturnedSet_ShouldBeCaseInsensitive()
    {
        var assemblies = new[] { typeof(LibraryBase).Assembly };


        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.Contains("newid", result, "Should match 'newid' (lowercase)");
        Assert.Contains("NEWID", result, "Should match 'NEWID' (uppercase)");
        Assert.Contains("NewId", result, "Should match 'NewId' (mixed case)");
        Assert.Contains("RAND", result, "Should match 'RAND' (uppercase)");
        Assert.Contains("rand", result, "Should match 'rand' (lowercase)");
    }

    #endregion

    #region Method Overload Tests

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenMethodHasOverloads_ShouldIncludeMethodNameOnce()
    {
        var assemblies = new[] { typeof(LibraryBase).Assembly };

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.Contains("Rand", result);


        var count = 0;
        foreach (var name in result)
            if (name.Equals("Rand", StringComparison.OrdinalIgnoreCase))
                count++;
        Assert.AreEqual(1, count, "Rand should appear exactly once in the set");
    }

    #endregion

    #region Integration with CSE Tests

    [TestMethod]
    public void ScanForNonDeterministicMethods_ResultSet_ShouldBeUsableForCseFiltering()
    {
        // Arrange
        var assemblies = new[] { typeof(LibraryBase).Assembly };

        // Act
        var nonDeterministicMethods = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        var testCases = new Dictionary<string, bool>
        {
            { "NewId", true },
            { "Rand", true },
            { "GetDate", true },
            { "Length", false },
            { "ToUpper", false },
            { "Sum", false },
            { "Count", false }
        };

        foreach (var (methodName, expectedNonDeterministic) in testCases)
        {
            var isNonDeterministic = nonDeterministicMethods.Contains(methodName);
            Assert.AreEqual(expectedNonDeterministic, isNonDeterministic,
                $"Method '{methodName}' non-deterministic status mismatch");
        }
    }

    #endregion

    #region Null and Empty Input Tests

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenAssembliesIsNull_ShouldReturnEmptySet()
    {
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(null);


        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenAssembliesIsEmpty_ShouldReturnEmptySet()
    {
        var assemblies = Array.Empty<Assembly>();


        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    #endregion

    #region Real Assembly Scanning Tests

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenScanningPluginsAssembly_ShouldFindKnownNonDeterministicMethods()
    {
        var assemblies = new[] { typeof(LibraryBase).Assembly };


        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.Contains("NewId", result, "Should find NewId method");
        Assert.Contains("Rand", result, "Should find Rand method");
        Assert.Contains("GetDate", result, "Should find GetDate method");
        Assert.Contains("UtcGetDate", result, "Should find UtcGetDate method");
    }

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenScanningPluginsAssembly_ShouldNotIncludeDeterministicMethods()
    {
        var assemblies = new[] { typeof(LibraryBase).Assembly };


        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.DoesNotContain("Length", result, "Length is deterministic");
        Assert.DoesNotContain("ToUpper", result, "ToUpper is deterministic");
        Assert.DoesNotContain("ToString", result, "ToString is deterministic");
        Assert.DoesNotContain("Substring", result, "Substring is deterministic");
        Assert.DoesNotContain("Abs", result, "Abs is deterministic");
    }

    #endregion

    #region Multiple Assemblies Tests

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenMultipleAssemblies_ShouldScanAll()
    {
        var assemblies = new[]
        {
            typeof(LibraryBase).Assembly,
            typeof(object).Assembly
        };


        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.Contains("NewId", result);
        Assert.Contains("Rand", result);
    }

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenAssemblyHasNoBindableMethods_ShouldReturnEmptySet()
    {
        var assemblies = new[] { typeof(object).Assembly };


        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.IsEmpty(result);
    }

    #endregion

    #region Attribute Combination Tests

    [TestMethod]
    public void ScanForNonDeterministicMethods_MethodWithOnlyBindableAttribute_ShouldNotBeIncluded()
    {
        var assemblies = new[] { typeof(LibraryBase).Assembly };


        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.DoesNotContain("Length",
            result, "Methods with only [BindableMethod] should not be included");
    }

    [TestMethod]
    public void ScanForNonDeterministicMethods_MethodWithBothAttributes_ShouldBeIncluded()
    {
        var assemblies = new[] { typeof(LibraryBase).Assembly };


        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.Contains("NewId",
            result, "Methods with both attributes should be included");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenCalledMultipleTimes_ShouldReturnConsistentResults()
    {
        var assemblies = new[] { typeof(LibraryBase).Assembly };


        var result1 = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);
        var result2 = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.HasCount(result1.Count, result2);
        foreach (var method in result1) Assert.Contains(method, result2, $"Method '{method}' missing in second scan");
    }

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenDuplicateAssemblies_ShouldHandleGracefully()
    {
        var assemblies = new[]
        {
            typeof(LibraryBase).Assembly,
            typeof(LibraryBase).Assembly,
            typeof(LibraryBase).Assembly
        };


        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.Contains("NewId", result);
        Assert.Contains("Rand", result);
    }

    #endregion

    #region LibraryBase Inheritance Tests

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenTypeDoesNotInheritFromLibraryBase_ShouldBeIgnored()
    {
        var assemblies = new[] { typeof(object).Assembly };


        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenTypeInheritsFromLibraryBase_ShouldBeScanned()
    {
        var assemblies = new[] { typeof(LibraryBase).Assembly };


        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);


        Assert.IsNotEmpty(result, "Should find at least one non-deterministic method");
        Assert.Contains("NewId", result, "Should find NewId from LibraryBase hierarchy");
    }

    #endregion
}
