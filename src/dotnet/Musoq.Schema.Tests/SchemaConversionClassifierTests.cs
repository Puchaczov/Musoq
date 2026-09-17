using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class SchemaConversionClassifierTests
{
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
