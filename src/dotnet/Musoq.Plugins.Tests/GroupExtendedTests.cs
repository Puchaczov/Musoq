using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Additional tests for the Group class to increase coverage (currently 77.4%)
/// </summary>
[TestClass]
public class GroupExtendedTests
{
    #region Hit and Count Tests

    [TestMethod]
    public void Group_Hit_ShouldIncrementCount()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        Assert.AreEqual(0, group.Count);

        // Act
        group.Hit();

        // Assert
        Assert.AreEqual(1, group.Count);
    }

    [TestMethod]
    public void Group_Hit_MultipleTimes_ShouldIncrementCountCorrectly()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());

        // Act
        group.Hit();
        group.Hit();
        group.Hit();

        // Assert
        Assert.AreEqual(3, group.Count);
    }

    #endregion

    #region GetRawValue Tests

    [TestMethod]
    public void Group_GetRawValue_ShouldReturnValue()
    {
        // Arrange
        var group = new Group(null, new[] { "field1" }, new object[] { 42 });

        // Act
        var result = group.GetRawValue<int>("field1");

        // Assert
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void Group_GetRawValue_WithSetValue_ShouldReturnCorrectValue()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        group.SetValue("myKey", "myValue");

        // Act
        var result = group.GetRawValue<string>("myKey");

        // Assert
        Assert.AreEqual("myValue", result);
    }

    [TestMethod]
    public void Group_GetRawValue_WithMissingKey_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => group.GetRawValue<int>("nonExistentKey"));
    }

    #endregion

    #region GetValue Tests

    [TestMethod]
    public void Group_GetValue_WithMissingKey_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => group.GetValue<int>("nonExistentKey"));
    }

    [TestMethod]
    public void Group_GetValue_WithConverter_ShouldApplyConverter()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());

        // First use GetOrCreateValueWithConverter to set up the converter
        group.GetOrCreateValueWithConverter<int, string>("key", 42, val => val?.ToString() ?? "null");

        // Act - GetValue should use the converter
        var result = group.GetValue<string>("key");

        // Assert
        Assert.AreEqual("42", result);
    }

    #endregion

    #region GetOrCreateValue Tests

    [TestMethod]
    public void Group_GetOrCreateValue_WithDefaultValue_ShouldCreateValue()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());

        // Act
        var result = group.GetOrCreateValue("myKey", 100);

        // Assert
        Assert.AreEqual(100, result);
    }

    [TestMethod]
    public void Group_GetOrCreateValue_WithDefaultValue_ShouldReturnExistingValue()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        group.SetValue("myKey", 50);

        // Act
        var result = group.GetOrCreateValue("myKey", 100);

        // Assert
        Assert.AreEqual(50, result);
    }

    [TestMethod]
    public void Group_GetOrCreateValue_WithFactory_ShouldCreateValue()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        var factoryCalled = false;

        // Act
        var result = group.GetOrCreateValue("myKey", () =>
        {
            factoryCalled = true;
            return new List<int> { 1, 2, 3 };
        });

        // Assert
        Assert.IsTrue(factoryCalled);
        Assert.IsNotNull(result);
        Assert.HasCount(3, result);
    }

    [TestMethod]
    public void Group_GetOrCreateValue_WithFactory_ShouldNotCallFactoryForExistingValue()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        var existingList = new List<int> { 10, 20 };
        group.SetValue("myKey", existingList);
        var factoryCalled = false;

        // Act
        var result = group.GetOrCreateValue("myKey", () =>
        {
            factoryCalled = true;
            return new List<int> { 1, 2, 3 };
        });

        // Assert
        Assert.IsFalse(factoryCalled);
        Assert.AreSame(existingList, result);
    }

    #endregion

    #region GetOrCreateValueWithConverter Tests

    [TestMethod]
    public void Group_GetOrCreateValueWithConverter_ShouldCreateAndConvert()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());

        // Act
        var result = group.GetOrCreateValueWithConverter<int, double>("key", 42, val => (double)(int)val * 2);

        // Assert
        Assert.AreEqual(84.0, result);
    }

    [TestMethod]
    public void Group_GetOrCreateValueWithConverter_ShouldReturnExistingConverted()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        group.GetOrCreateValueWithConverter<int, double>("key", 42, val => (double)(int)val * 2);

        // Act - call again with same key
        var result = group.GetOrCreateValueWithConverter<int, double>("key", 100, val => (double)(int)val * 2);

        // Assert - should use original value (42) not the new one (100)
        Assert.AreEqual(84.0, result);
    }

    [TestMethod]
    public void Group_GetOrCreateValueWithConverter_ConverterShouldBeReused()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        var converterCallCount = 0;

        Func<object?, object?> converter = val =>
        {
            converterCallCount++;
            return (int)val! * 10;
        };

        // Act
        group.GetOrCreateValueWithConverter<int, int>("key", 5, converter);
        group.GetOrCreateValueWithConverter<int, int>("key", 5, converter);

        // Assert - converter should be called twice (once per GetOrCreateValueWithConverter call)
        Assert.AreEqual(2, converterCallCount);
    }

    #endregion

    #region Parent Tests

    [TestMethod]
    public void Group_Parent_ShouldBeSet()
    {
        // Arrange
        var parent = new Group(null, Array.Empty<string>(), Array.Empty<object>());

        // Act
        var child = new Group(parent, new[] { "childField" }, new object[] { "value" });

        // Assert
        Assert.AreSame(parent, child.Parent);
    }

    [TestMethod]
    public void Group_Parent_ShouldBeNullForRoot()
    {
        // Arrange & Act
        var root = new Group(null, Array.Empty<string>(), Array.Empty<object>());

        // Assert
        Assert.IsNull(root.Parent);
    }

    #endregion

    #region Constructor Tests

    [TestMethod]
    public void Group_Constructor_ShouldInitializeValuesFromFieldNamesAndValues()
    {
        // Arrange
        var fieldNames = new[] { "name", "age", "city" };
        var values = new object[] { "John", 30, "NYC" };

        // Act
        var group = new Group(null, fieldNames, values);

        // Assert
        Assert.AreEqual("John", group.GetRawValue<string>("name"));
        Assert.AreEqual(30, group.GetRawValue<int>("age"));
        Assert.AreEqual("NYC", group.GetRawValue<string>("city"));
    }

    [TestMethod]
    public void Group_Constructor_WithEmptyFieldNames_ShouldWork()
    {
        // Arrange & Act
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());

        // Assert
        Assert.IsNotNull(group);
        Assert.AreEqual(0, group.Count);
    }

    #endregion

    #region SetValue Tests

    [TestMethod]
    public void Group_SetValue_ShouldOverwriteExistingValue()
    {
        // Arrange
        var group = new Group(null, new[] { "field" }, new object[] { "original" });

        // Act
        group.SetValue("field", "updated");

        // Assert
        Assert.AreEqual("updated", group.GetRawValue<string>("field"));
    }

    [TestMethod]
    public void Group_SetValue_ShouldAllowNullValue()
    {
        // Arrange
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());

        // Act
        group.SetValue<string?>("nullableKey", null);

        // Assert
        Assert.IsNull(group.GetRawValue<string?>("nullableKey"));
    }

    #endregion
}
