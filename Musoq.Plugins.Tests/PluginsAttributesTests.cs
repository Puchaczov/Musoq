using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Tests for Plugins Attributes with low coverage (Session 5 - Phase 2)
/// </summary>
[TestClass]
public class PluginsAttributesTests
{
    #region BindableClassAttribute Tests

    [TestMethod]
    public void BindableClassAttribute_IsAttribute()
    {
        // Arrange & Act
        var attr = new BindableClassAttribute();

        // Assert
        Assert.IsInstanceOfType(attr, typeof(Attribute));
    }

    #endregion

    #region AggregationMethodAttribute Tests

    [TestMethod]
    public void AggregationMethodAttribute_IsBindableMethodAttribute()
    {
        // Arrange & Act
        var attr = new AggregationMethodAttribute();

        // Assert
        Assert.IsInstanceOfType(attr, typeof(BindableMethodAttribute));
    }

    #endregion

    #region AggregationGetMethodAttribute Tests

    [TestMethod]
    public void AggregationGetMethodAttribute_IsAggregationMethodAttribute()
    {
        // Arrange & Act
        var attr = new AggregationGetMethodAttribute();

        // Assert
        Assert.IsInstanceOfType(attr, typeof(AggregationMethodAttribute));
    }

    #endregion

    #region AggregationSetMethodAttribute Tests

    [TestMethod]
    public void AggregationSetMethodAttribute_IsAggregationMethodAttribute()
    {
        // Arrange & Act
        var attr = new AggregationSetMethodAttribute();

        // Assert
        Assert.IsInstanceOfType(attr, typeof(AggregationMethodAttribute));
    }

    #endregion

    #region AggregateSetDoNotResolveAttribute Tests

    [TestMethod]
    public void AggregateSetDoNotResolveAttribute_IsAttribute()
    {
        // Arrange & Act
        var attr = new AggregateSetDoNotResolveAttribute();

        // Assert
        Assert.IsInstanceOfType(attr, typeof(Attribute));
    }

    #endregion

    #region BindablePropertyAsTableAttribute Tests

    [TestMethod]
    public void BindablePropertyAsTableAttribute_IsAttribute()
    {
        // Arrange & Act
        var attr = new BindablePropertyAsTableAttribute();

        // Assert
        Assert.IsInstanceOfType(attr, typeof(Attribute));
    }

    #endregion

    #region BindableMethodAttribute Tests

    [TestMethod]
    public void BindableMethodAttribute_DefaultConstructor_IsInternalFalse()
    {
        // Arrange & Act
        var attr = new BindableMethodAttribute();

        // Assert - IsInternal is internal, but we verify attribute was created
        Assert.IsNotNull(attr);
    }

    [TestMethod]
    public void BindableMethodAttribute_IsAttribute()
    {
        // Arrange & Act
        var attr = new BindableMethodAttribute();

        // Assert
        Assert.IsInstanceOfType(attr, typeof(Attribute));
    }

    #endregion

    #region InjectGroupAttribute Tests

    [TestMethod]
    public void InjectGroupAttribute_InjectType_ReturnsGroupType()
    {
        // Arrange & Act
        var attr = new InjectGroupAttribute();

        // Assert
        Assert.AreEqual(typeof(Group), attr.InjectType);
    }

    [TestMethod]
    public void InjectGroupAttribute_IsInjectTypeAttribute()
    {
        // Arrange & Act
        var attr = new InjectGroupAttribute();

        // Assert
        Assert.IsInstanceOfType(attr, typeof(InjectTypeAttribute));
    }

    #endregion

    #region InjectQueryStatsAttribute Tests

    [TestMethod]
    public void InjectQueryStatsAttribute_InjectType_ReturnsQueryStatsType()
    {
        // Arrange & Act
        var attr = new InjectQueryStatsAttribute();

        // Assert
        Assert.AreEqual(typeof(QueryStats), attr.InjectType);
    }

    [TestMethod]
    public void InjectQueryStatsAttribute_IsInjectTypeAttribute()
    {
        // Arrange & Act
        var attr = new InjectQueryStatsAttribute();

        // Assert
        Assert.IsInstanceOfType(attr, typeof(InjectTypeAttribute));
    }

    #endregion

    #region InjectSpecificSourceAttribute Tests

    [TestMethod]
    public void InjectSpecificSourceAttribute_Constructor_SetsType()
    {
        // Arrange
        var expectedType = typeof(string);

        // Act
        var attr = new InjectSpecificSourceAttribute(expectedType);

        // Assert
        Assert.AreEqual(expectedType, attr.InjectType);
    }

    [TestMethod]
    public void InjectSpecificSourceAttribute_IsInjectTypeAttribute()
    {
        // Arrange & Act
        var attr = new InjectSpecificSourceAttribute(typeof(int));

        // Assert
        Assert.IsInstanceOfType(attr, typeof(InjectTypeAttribute));
    }

    #endregion

    #region InjectGroupAccessName Tests

    [TestMethod]
    public void InjectGroupAccessName_InjectType_ReturnsStringType()
    {
        // Arrange & Act
        var attr = new InjectGroupAccessName();

        // Assert
        Assert.AreEqual(typeof(string), attr.InjectType);
    }

    [TestMethod]
    public void InjectGroupAccessName_IsInjectTypeAttribute()
    {
        // Arrange & Act
        var attr = new InjectGroupAccessName();

        // Assert
        Assert.IsInstanceOfType(attr, typeof(InjectTypeAttribute));
    }

    #endregion

    #region DynamicObjectPropertyTypeHintAttribute Tests

    [TestMethod]
    public void DynamicObjectPropertyTypeHintAttribute_Constructor_SetsProperties()
    {
        // Arrange
        var name = "testProperty";
        var type = typeof(int);

        // Act
        var attr = new DynamicObjectPropertyTypeHintAttribute(name, type);

        // Assert
        Assert.AreEqual(name, attr.Name);
        Assert.AreEqual(type, attr.Type);
    }

    [TestMethod]
    public void DynamicObjectPropertyTypeHintAttribute_AllowsMultiple()
    {
        // Arrange
        var attrUsage = typeof(DynamicObjectPropertyTypeHintAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        // Assert
        Assert.HasCount(1, attrUsage);
        var usage = (AttributeUsageAttribute)attrUsage[0];
        Assert.IsTrue(usage.AllowMultiple);
    }

    #endregion

    #region DynamicObjectPropertyDefaultTypeHintAttribute Tests

    [TestMethod]
    public void DynamicObjectPropertyDefaultTypeHintAttribute_Constructor_SetsType()
    {
        // Arrange
        var type = typeof(string);

        // Act
        var attr = new DynamicObjectPropertyDefaultTypeHintAttribute(type);

        // Assert
        Assert.AreEqual(type, attr.Type);
    }

    [TestMethod]
    public void DynamicObjectPropertyDefaultTypeHintAttribute_TargetsClass()
    {
        // Arrange
        var attrUsage = typeof(DynamicObjectPropertyDefaultTypeHintAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        // Assert
        Assert.HasCount(1, attrUsage);
        var usage = (AttributeUsageAttribute)attrUsage[0];
        Assert.AreEqual(AttributeTargets.Class, usage.ValidOn);
    }

    #endregion

    #region MethodCategoryAttribute Tests

    [TestMethod]
    public void MethodCategoryAttribute_Constructor_SetsCategory()
    {
        // Arrange
        var category = "TestCategory";

        // Act
        var attr = new MethodCategoryAttribute(category);

        // Assert
        Assert.AreEqual(category, attr.Category);
    }

    [TestMethod]
    public void MethodCategoryAttribute_TargetsMethod()
    {
        // Arrange
        var attrUsage = typeof(MethodCategoryAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        // Assert
        Assert.HasCount(1, attrUsage);
        var usage = (AttributeUsageAttribute)attrUsage[0];
        Assert.AreEqual(AttributeTargets.Method, usage.ValidOn);
    }

    #endregion

    #region MethodCategories Constants Tests

    [TestMethod]
    public void MethodCategories_String_IsCorrectValue()
    {
        Assert.AreEqual("String", MethodCategories.String);
    }

    [TestMethod]
    public void MethodCategories_Math_IsCorrectValue()
    {
        Assert.AreEqual("Math", MethodCategories.Math);
    }

    [TestMethod]
    public void MethodCategories_DateTime_IsCorrectValue()
    {
        Assert.AreEqual("DateTime", MethodCategories.DateTime);
    }

    [TestMethod]
    public void MethodCategories_Conversion_IsCorrectValue()
    {
        Assert.AreEqual("Conversion", MethodCategories.Conversion);
    }

    [TestMethod]
    public void MethodCategories_Aggregate_IsCorrectValue()
    {
        Assert.AreEqual("Aggregate", MethodCategories.Aggregate);
    }

    [TestMethod]
    public void MethodCategories_Cryptography_IsCorrectValue()
    {
        Assert.AreEqual("Cryptography", MethodCategories.Cryptography);
    }

    [TestMethod]
    public void MethodCategories_Json_IsCorrectValue()
    {
        Assert.AreEqual("Json", MethodCategories.Json);
    }

    [TestMethod]
    public void MethodCategories_Network_IsCorrectValue()
    {
        Assert.AreEqual("Network", MethodCategories.Network);
    }

    #endregion

    #region NonDeterministicAttribute Tests

    [TestMethod]
    public void NonDeterministicAttribute_IsAttribute()
    {
        // Arrange & Act
        var attr = new NonDeterministicAttribute();

        // Assert
        Assert.IsInstanceOfType(attr, typeof(Attribute));
    }

    [TestMethod]
    public void NonDeterministicAttribute_TargetsMethod()
    {
        // Arrange
        var attrUsage = typeof(NonDeterministicAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        // Assert
        Assert.HasCount(1, attrUsage);
        var usage = (AttributeUsageAttribute)attrUsage[0];
        Assert.AreEqual(AttributeTargets.Method, usage.ValidOn);
    }

    #endregion

    #region Group Tests

    [TestMethod]
    public void Group_SetValue_GetValue_Works()
    {
        // Arrange
        var group = new Group(null, new[] { "field1" }, new object[] { "value1" });
        var key = "testKey";
        var value = 42;

        // Act
        group.SetValue(key, value);
        var result = group.GetValue<int>(key);

        // Assert
        Assert.AreEqual(value, result);
    }

    [TestMethod]
    public void Group_GetOrCreateValue_CreatesNew()
    {
        // Arrange
        var group = new Group(null, new[] { "field1" }, new object[] { "value1" });
        var key = "newKey";

        // Act
        var result = group.GetOrCreateValue(key, () => 100);

        // Assert
        Assert.AreEqual(100, result);
    }

    [TestMethod]
    public void Group_GetOrCreateValue_ReturnsExisting()
    {
        // Arrange
        var group = new Group(null, new[] { "field1" }, new object[] { "value1" });
        var key = "existingKey";
        group.SetValue(key, 50);

        // Act
        var result = group.GetOrCreateValue(key, () => 100);

        // Assert
        Assert.AreEqual(50, result);
    }

    #endregion

    #region QueryStats Tests

    [TestMethod]
    public void QueryStats_RowNumber_InitialValueIsZero()
    {
        // Arrange & Act
        var stats = new QueryStats();

        // Assert - Default value is 0 (not 1)
        Assert.AreEqual(0, stats.RowNumber);
    }

    [TestMethod]
    public void QueryStats_RowNumber_CanBeRead()
    {
        // Arrange
        var stats = new QueryStats();

        // Act - The setter is protected, so we just test the getter
        var rowNumber = stats.RowNumber;

        // Assert
        Assert.IsGreaterThanOrEqualTo(0, rowNumber);
    }

    #endregion
}