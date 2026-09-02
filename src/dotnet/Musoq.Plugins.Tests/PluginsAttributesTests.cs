using System;
using System.Reflection;
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
        var hasAttribute = Attribute.IsDefined(typeof(AttributeTargetsFixture), typeof(BindableClassAttribute));

        // Assert
        Assert.IsTrue(hasAttribute);
    }

    #endregion

    #region AggregationMethodAttribute Tests

    [TestMethod]
    public void AggregationMethodAttribute_IsBindableMethodAttribute()
    {
        // Arrange & Act
        var attr = new AggregationMethodAttribute();

        // Assert
        Assert.IsInstanceOfType<BindableMethodAttribute>(attr);
    }

    #endregion

    #region BindablePropertyAsTableAttribute Tests

    [TestMethod]
    public void BindablePropertyAsTableAttribute_IsAttribute()
    {
        // Arrange
        var property = typeof(AttributeTargetsFixture).GetProperty(nameof(AttributeTargetsFixture.TableProperty));

        // Act
        var hasAttribute = Attribute.IsDefined(property!, typeof(BindablePropertyAsTableAttribute));

        // Assert
        Assert.IsTrue(hasAttribute);
    }

    #endregion

    [BindableClass]
    private sealed class AttributeTargetsFixture
    {
        [BindablePropertyAsTable] public string TableProperty { get; } = "table";

        [NonDeterministic]
        public int VolatileProperty => 1;

        [BindableMethod]
        [AggregationMethod]
        [NonDeterministic]
        [MethodCategory("TestCategory")]
        public void SampleMethod()
        {
        }

        [DynamicObjectPropertyTypeHint("First", typeof(string))]
        [DynamicObjectPropertyTypeHint("Second", typeof(int))]
        public sealed class WithMultipleHints;
    }

    private sealed class TestQueryStats : QueryStats
    {
        public void SetRowNumber(int value)
        {
            RowNumber = value;
        }
    }

    #region BindableMethodAttribute Tests

    [TestMethod]
    public void BindableMethodAttribute_DefaultConstructor_IsInternalFalse()
    {
        // Arrange & Act
        var attr = new BindableMethodAttribute();

        // Assert
        var isInternalProperty = typeof(BindableMethodAttribute)
            .GetProperty("IsInternal", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(isInternalProperty);
        var isInternalValue = (bool?)isInternalProperty.GetValue(attr);
        Assert.IsFalse(isInternalValue);
    }

    [TestMethod]
    public void BindableMethodAttribute_IsAttribute()
    {
        // Arrange
        var method = typeof(AttributeTargetsFixture).GetMethod(nameof(AttributeTargetsFixture.SampleMethod));

        // Act
        var hasAttribute = Attribute.IsDefined(method!, typeof(BindableMethodAttribute));

        // Assert
        Assert.IsTrue(hasAttribute);
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
        Assert.IsInstanceOfType<InjectTypeAttribute>(attr);
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
        Assert.IsInstanceOfType<InjectTypeAttribute>(attr);
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
        var attributes = (DynamicObjectPropertyTypeHintAttribute[])Attribute
            .GetCustomAttributes(typeof(AttributeTargetsFixture.WithMultipleHints),
                typeof(DynamicObjectPropertyTypeHintAttribute), false);

        // Assert
        Assert.HasCount(2, attributes);
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
        var value = typeof(MethodCategories).GetField(nameof(MethodCategories.String))?.GetRawConstantValue() as string;
        Assert.AreEqual("String", value);
    }

    [TestMethod]
    public void MethodCategories_Math_IsCorrectValue()
    {
        var value = typeof(MethodCategories).GetField(nameof(MethodCategories.Math))?.GetRawConstantValue() as string;
        Assert.AreEqual("Math", value);
    }

    [TestMethod]
    public void MethodCategories_DateTime_IsCorrectValue()
    {
        var value =
            typeof(MethodCategories).GetField(nameof(MethodCategories.DateTime))?.GetRawConstantValue() as string;
        Assert.AreEqual("DateTime", value);
    }

    [TestMethod]
    public void MethodCategories_Conversion_IsCorrectValue()
    {
        var value =
            typeof(MethodCategories).GetField(nameof(MethodCategories.Conversion))?.GetRawConstantValue() as string;
        Assert.AreEqual("Conversion", value);
    }

    [TestMethod]
    public void MethodCategories_Aggregate_IsCorrectValue()
    {
        var value =
            typeof(MethodCategories).GetField(nameof(MethodCategories.Aggregate))?.GetRawConstantValue() as string;
        Assert.AreEqual("Aggregate", value);
    }

    [TestMethod]
    public void MethodCategories_Cryptography_IsCorrectValue()
    {
        var value =
            typeof(MethodCategories).GetField(nameof(MethodCategories.Cryptography))?.GetRawConstantValue() as string;
        Assert.AreEqual("Cryptography", value);
    }

    [TestMethod]
    public void MethodCategories_Json_IsCorrectValue()
    {
        var value = typeof(MethodCategories).GetField(nameof(MethodCategories.Json))?.GetRawConstantValue() as string;
        Assert.AreEqual("Json", value);
    }

    [TestMethod]
    public void MethodCategories_Network_IsCorrectValue()
    {
        var value = typeof(MethodCategories).GetField(nameof(MethodCategories.Network))
            ?.GetRawConstantValue() as string;
        Assert.AreEqual("Network", value);
    }

    #endregion

    #region NonDeterministicAttribute Tests

    [TestMethod]
    public void NonDeterministicAttribute_IsAttribute()
    {
        // Arrange
        var method = typeof(AttributeTargetsFixture).GetMethod(nameof(AttributeTargetsFixture.SampleMethod));

        // Act
        var hasAttribute = Attribute.IsDefined(method!, typeof(NonDeterministicAttribute));

        // Assert
        Assert.IsTrue(hasAttribute);
    }

    [TestMethod]
    public void NonDeterministicAttribute_TargetsMethodAndProperty()
    {
        // Arrange
        var attrUsage = typeof(NonDeterministicAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        // Assert
        Assert.HasCount(1, attrUsage);
        var usage = (AttributeUsageAttribute)attrUsage[0];
        Assert.AreEqual(AttributeTargets.Method | AttributeTargets.Property, usage.ValidOn);

        var property = typeof(AttributeTargetsFixture).GetProperty(nameof(AttributeTargetsFixture.VolatileProperty));
        Assert.IsTrue(Attribute.IsDefined(property!, typeof(NonDeterministicAttribute)));
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
        var stats = new TestQueryStats();

        // Act
        stats.SetRowNumber(5);

        // Assert
        Assert.AreEqual(5, stats.RowNumber);
    }

    #endregion
}
