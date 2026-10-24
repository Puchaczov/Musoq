using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class SchemaConversionClassifierTests
{
    public static IEnumerable<object[]> NumericConversions => LosslessTargets.SelectMany(source =>
        LosslessTargets.Keys.Select(target => new object[] { source.Key, target, source.Value.Contains(target) }));

    private static readonly IReadOnlyDictionary<Type, Type[]> LosslessTargets = new Dictionary<Type, Type[]>
    {
        [typeof(sbyte)] = [typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(byte)] = [typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(short)] = [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(ushort)] = [typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(int)] = [typeof(int), typeof(long), typeof(double), typeof(decimal)],
        [typeof(uint)] = [typeof(uint), typeof(long), typeof(ulong), typeof(double), typeof(decimal)],
        [typeof(long)] = [typeof(long), typeof(decimal)],
        [typeof(ulong)] = [typeof(ulong), typeof(decimal)],
        [typeof(float)] = [typeof(float), typeof(double)],
        [typeof(double)] = [typeof(double)],
        [typeof(decimal)] = [typeof(decimal)],
        [typeof(char)] = [typeof(char), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)]
    };

    [TestMethod]
    [DynamicData(nameof(NumericConversions))]
    public void NumericConversion_ShouldMatchTheLosslessContract(Type source, Type target, bool expected)
    {
        Assert.AreEqual(expected, SchemaConversionClassifier.TryGetCost(source, target, out _));
    }

    [TestMethod]
    [DynamicData(nameof(NumericConversions))]
    public void LiftedNumericConversion_ShouldPreserveTheLosslessContract(Type source, Type target, bool expected)
    {
        var nullableSource = typeof(Nullable<>).MakeGenericType(source);
        var nullableTarget = typeof(Nullable<>).MakeGenericType(target);

        Assert.AreEqual(expected, SchemaConversionClassifier.TryGetCost(nullableSource, nullableTarget, out _));
    }

    [TestMethod]
    public void NullableValue_ShouldRemainAssignableToAnObjectReceiver()
    {
        Assert.IsTrue(SchemaConversionClassifier.TryGetCost(typeof(int?), typeof(object), out _));
    }

    [TestMethod]
    public void TryGetCost_NullableSourceToNonNullableTarget_IsRejected()
    {
        Assert.IsFalse(SchemaConversionClassifier.TryGetCost(typeof(int?), typeof(int), out _));
    }

    [TestMethod]
    public void TryGetCost_NonNullableSourceToNullableTarget_IsLifted()
    {
        Assert.IsTrue(SchemaConversionClassifier.TryGetCost(typeof(int), typeof(int?), out var cost));
        Assert.AreEqual(1, cost);
    }

    [TestMethod]
    public void TryGetCost_UsesExistingDecimalAndCharWideningTable()
    {
        Assert.IsTrue(SchemaConversionClassifier.TryGetCost(typeof(int), typeof(decimal), out var decimalCost));
        Assert.AreEqual(3, decimalCost);
        Assert.IsTrue(SchemaConversionClassifier.TryGetCost(typeof(char), typeof(int), out var charCost));
        Assert.AreEqual(2, charCost);
    }

    [TestMethod]
    public void TryGetCost_RejectsNullableArrayToNonNullableArray()
    {
        Assert.IsFalse(SchemaConversionClassifier.TryGetCost(typeof(int?[]), typeof(int[]), out _));
    }

    [TestMethod]
    public void TryGetCost_DoesNotInventElementWiseArrayConversions()
    {
        Assert.IsFalse(SchemaConversionClassifier.TryGetCost(typeof(int[]), typeof(long[]), out _));
    }
}
