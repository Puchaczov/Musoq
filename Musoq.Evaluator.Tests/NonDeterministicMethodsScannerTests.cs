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
        // Arrange
        var assemblies = new[] { typeof(LibraryBase).Assembly };

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert - verify case insensitivity
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
        // Arrange - Rand has overloads: Rand() and Rand(min, max)
        var assemblies = new[] { typeof(LibraryBase).Assembly };

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert - the method name should be in the set (HashSet ensures uniqueness)
        Assert.Contains("Rand", result);

        // Count how many times "Rand" appears (should be 1 in a HashSet)
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

        // Assert - simulate CSE filtering logic
        var testCases = new Dictionary<string, bool>
        {
            { "NewId", true }, // Should be marked as non-deterministic
            { "Rand", true }, // Should be marked as non-deterministic
            { "GetDate", true }, // Should be marked as non-deterministic
            { "Length", false }, // Should NOT be marked as non-deterministic
            { "ToUpper", false }, // Should NOT be marked as non-deterministic
            { "Sum", false }, // Aggregates are deterministic once computed
            { "Count", false } // Aggregates are deterministic once computed
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
        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(null);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenAssembliesIsEmpty_ShouldReturnEmptySet()
    {
        // Arrange
        var assemblies = Array.Empty<Assembly>();

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    #endregion

    #region Real Assembly Scanning Tests

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenScanningPluginsAssembly_ShouldFindKnownNonDeterministicMethods()
    {
        // Arrange - scan the actual Musoq.Plugins assembly
        var assemblies = new[] { typeof(LibraryBase).Assembly };

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert - verify known non-deterministic methods are found
        Assert.Contains("NewId", result, "Should find NewId method");
        Assert.Contains("Rand", result, "Should find Rand method");
        Assert.Contains("GetDate", result, "Should find GetDate method");
        Assert.Contains("UtcGetDate", result, "Should find UtcGetDate method");
    }

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenScanningPluginsAssembly_ShouldNotIncludeDeterministicMethods()
    {
        // Arrange
        var assemblies = new[] { typeof(LibraryBase).Assembly };

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert - deterministic methods should NOT be in the result
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
        // Arrange - include multiple assemblies
        var assemblies = new[]
        {
            typeof(LibraryBase).Assembly, // Musoq.Plugins - has non-deterministic methods
            typeof(object).Assembly // mscorlib - no Musoq attributes
        };

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert - should still find methods from Musoq.Plugins
        Assert.Contains("NewId", result);
        Assert.Contains("Rand", result);
    }

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenAssemblyHasNoBindableMethods_ShouldReturnEmptySet()
    {
        // Arrange - use an assembly that has no Musoq attributes
        var assemblies = new[] { typeof(object).Assembly };

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert
        Assert.IsEmpty(result);
    }

    #endregion

    #region Attribute Combination Tests

    [TestMethod]
    public void ScanForNonDeterministicMethods_MethodWithOnlyBindableAttribute_ShouldNotBeIncluded()
    {
        // Arrange - Length has [BindableMethod] but not [NonDeterministic]
        var assemblies = new[] { typeof(LibraryBase).Assembly };

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert
        Assert.DoesNotContain("Length",
            result, "Methods with only [BindableMethod] should not be included");
    }

    [TestMethod]
    public void ScanForNonDeterministicMethods_MethodWithBothAttributes_ShouldBeIncluded()
    {
        // Arrange - NewId has both [BindableMethod] and [NonDeterministic]
        var assemblies = new[] { typeof(LibraryBase).Assembly };

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert
        Assert.Contains("NewId",
            result, "Methods with both attributes should be included");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenCalledMultipleTimes_ShouldReturnConsistentResults()
    {
        // Arrange
        var assemblies = new[] { typeof(LibraryBase).Assembly };

        // Act - call multiple times
        var result1 = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);
        var result2 = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert - results should be equivalent
        Assert.HasCount(result1.Count, result2);
        foreach (var method in result1) Assert.Contains(method, result2, $"Method '{method}' missing in second scan");
    }

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenDuplicateAssemblies_ShouldHandleGracefully()
    {
        // Arrange - same assembly multiple times
        var assemblies = new[]
        {
            typeof(LibraryBase).Assembly,
            typeof(LibraryBase).Assembly,
            typeof(LibraryBase).Assembly
        };

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert - should still work correctly (HashSet handles duplicates)
        Assert.Contains("NewId", result);
        Assert.Contains("Rand", result);
    }

    #endregion

    #region LibraryBase Inheritance Tests

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenTypeDoesNotInheritFromLibraryBase_ShouldBeIgnored()
    {
        // Arrange - use an assembly with classes that have the attributes but don't inherit LibraryBase
        // The mscorlib assembly has many public classes, none inheriting from LibraryBase
        var assemblies = new[] { typeof(object).Assembly };

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert - should be empty since no types inherit from LibraryBase
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ScanForNonDeterministicMethods_WhenTypeInheritsFromLibraryBase_ShouldBeScanned()
    {
        // Arrange - LibraryBase itself and its subclasses should be scanned
        var assemblies = new[] { typeof(LibraryBase).Assembly };

        // Act
        var result = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);

        // Assert - should find non-deterministic methods from LibraryBase subclasses
        Assert.IsNotEmpty(result, "Should find at least one non-deterministic method");
        Assert.Contains("NewId", result, "Should find NewId from LibraryBase hierarchy");
    }

    #endregion
}