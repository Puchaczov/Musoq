using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for aggregation methods (Count, Max, Min) to improve branch coverage.
///     Focuses on untested type overloads and edge cases.
/// </summary>
[TestClass]
public class AggregationMethodsExtendedTests
{
    private LibraryBase _library = null!;

    [TestInitialize]
    public void Setup()
    {
        _library = new LibraryBase();
    }

    #region SetCount Tests for Untested Types

    [TestMethod]
    public void SetCount_ByteValue_IncrementsCount()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (byte)1);
        _library.SetCount(group, "test", (byte)2);
        Assert.AreEqual(2, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_ByteNull_DoesNotIncrement()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (byte?)null);
        Assert.AreEqual(0, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_SByteValue_IncrementsCount()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", 1);
        _library.SetCount(group, "test", -1);
        Assert.AreEqual(2, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_SByteNull_DoesNotIncrement()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (sbyte?)null);
        Assert.AreEqual(0, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_ShortValue_IncrementsCount()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (short)100);
        _library.SetCount(group, "test", (short)-100);
        Assert.AreEqual(2, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_ShortNull_DoesNotIncrement()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (short?)null);
        Assert.AreEqual(0, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_UShortValue_IncrementsCount()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (ushort)100);
        _library.SetCount(group, "test", (ushort)200);
        Assert.AreEqual(2, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_UShortNull_DoesNotIncrement()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (ushort?)null);
        Assert.AreEqual(0, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_UIntValue_IncrementsCount()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (uint)100);
        _library.SetCount(group, "test", (uint)200);
        Assert.AreEqual(2, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_UIntNull_DoesNotIncrement()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (uint?)null);
        Assert.AreEqual(0, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_ULongValue_IncrementsCount()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (ulong)100);
        _library.SetCount(group, "test", (ulong)200);
        Assert.AreEqual(2, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_ULongNull_DoesNotIncrement()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (ulong?)null);
        Assert.AreEqual(0, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_FloatValue_IncrementsCount()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", 1.5f);
        _library.SetCount(group, "test", 2.5f);
        Assert.AreEqual(2, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_FloatNull_DoesNotIncrement()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (float?)null);
        Assert.AreEqual(0, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_DoubleValue_IncrementsCount()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", 1.5);
        _library.SetCount(group, "test", 2.5);
        Assert.AreEqual(2, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_DoubleNull_DoesNotIncrement()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (double?)null);
        Assert.AreEqual(0, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetCount_MixedNullAndValues_CorrectCount()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", (byte)1);
        _library.SetCount(group, "test", (byte?)null);
        _library.SetCount(group, "test", (byte)2);
        _library.SetCount(group, "test", (byte?)null);
        _library.SetCount(group, "test", (byte)3);
        Assert.AreEqual(3, _library.Count(group, "test"));
    }

    #endregion

    #region SetMax Tests for Untested Types

    [TestMethod]
    public void SetMax_ByteValue_ReturnsMax()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", (byte)10);
        _library.SetMax(group, "test", (byte)50);
        _library.SetMax(group, "test", (byte)30);
        Assert.AreEqual(50m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_ByteNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", (byte)10);
        _library.SetMax(group, "test", (byte?)null);
        _library.SetMax(group, "test", (byte)5);
        Assert.AreEqual(10m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_SByteValue_ReturnsMax()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", -10);
        _library.SetMax(group, "test", 50);
        _library.SetMax(group, "test", -30);
        Assert.AreEqual(50m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_SByteNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", 10);
        _library.SetMax(group, "test", null);
        Assert.AreEqual(10m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_ShortValue_ReturnsMax()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", (short)100);
        _library.SetMax(group, "test", 500);
        _library.SetMax(group, "test", 300);
        Assert.AreEqual(500m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_ShortNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", (short)100);
        _library.SetMax(group, "test", (short?)null);
        Assert.AreEqual(100m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_UShortValue_ReturnsMax()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", (ushort)100);
        _library.SetMax(group, "test", (ushort)500);
        _library.SetMax(group, "test", (ushort)300);
        Assert.AreEqual(500m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_UShortNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", (ushort)100);
        _library.SetMax(group, "test", (ushort?)null);
        Assert.AreEqual(100m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_UIntValue_ReturnsMax()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", (uint)100);
        _library.SetMax(group, "test", (uint)500);
        _library.SetMax(group, "test", (uint)300);
        Assert.AreEqual(500m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_UIntNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", (uint)100);
        _library.SetMax(group, "test", (uint?)null);
        Assert.AreEqual(100m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_ULongValue_ReturnsMax()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", (ulong)100);
        _library.SetMax(group, "test", (ulong)500);
        _library.SetMax(group, "test", (ulong)300);
        Assert.AreEqual(500m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_ULongNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", (ulong)100);
        _library.SetMax(group, "test", (ulong?)null);
        Assert.AreEqual(100m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_FloatValue_ReturnsMax()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", 1.5f);
        _library.SetMax(group, "test", 5.5f);
        _library.SetMax(group, "test", 3.5f);
        Assert.AreEqual(5.5m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_FloatNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", 1.5f);
        _library.SetMax(group, "test", (float?)null);
        Assert.AreEqual(1.5m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_DoubleValue_ReturnsMax()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", 1.5);
        _library.SetMax(group, "test", 5.5);
        _library.SetMax(group, "test", 3.5);
        Assert.AreEqual(5.5m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_DoubleNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", 1.5);
        _library.SetMax(group, "test", (double?)null);
        Assert.AreEqual(1.5m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_AllNullValues_ReturnsMinValue()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", (decimal?)null);
        _library.SetMax(group, "test", (decimal?)null);
        Assert.AreEqual(decimal.MinValue, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMax_NegativeValues_ReturnsLargestNegative()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", -100m);
        _library.SetMax(group, "test", -50m);
        _library.SetMax(group, "test", -200m);
        Assert.AreEqual(-50m, _library.Max(group, "test"));
    }

    #endregion

    #region SetMin Tests for Untested Types

    [TestMethod]
    public void SetMin_ByteValue_ReturnsMin()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", (byte)50);
        _library.SetMin(group, "test", (byte)10);
        _library.SetMin(group, "test", (byte)30);
        Assert.AreEqual(10m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_ByteNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", (byte)10);
        _library.SetMin(group, "test", (byte?)null);
        _library.SetMin(group, "test", (byte)50);
        Assert.AreEqual(10m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_SByteValue_ReturnsMin()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", 50);
        _library.SetMin(group, "test", -10);
        _library.SetMin(group, "test", 30);
        Assert.AreEqual(-10m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_SByteNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", 10);
        _library.SetMin(group, "test", null);
        Assert.AreEqual(10m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_ShortValue_ReturnsMin()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", 500);
        _library.SetMin(group, "test", (short)100);
        _library.SetMin(group, "test", 300);
        Assert.AreEqual(100m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_ShortNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", (short)100);
        _library.SetMin(group, "test", (short?)null);
        Assert.AreEqual(100m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_UShortValue_ReturnsMin()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", (ushort)500);
        _library.SetMin(group, "test", (ushort)100);
        _library.SetMin(group, "test", (ushort)300);
        Assert.AreEqual(100m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_UShortNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", (ushort)100);
        _library.SetMin(group, "test", (ushort?)null);
        Assert.AreEqual(100m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_UIntValue_ReturnsMin()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", (uint)500);
        _library.SetMin(group, "test", (uint)100);
        _library.SetMin(group, "test", (uint)300);
        Assert.AreEqual(100m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_UIntNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", (uint)100);
        _library.SetMin(group, "test", (uint?)null);
        Assert.AreEqual(100m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_ULongValue_ReturnsMin()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", (ulong)500);
        _library.SetMin(group, "test", (ulong)100);
        _library.SetMin(group, "test", (ulong)300);
        Assert.AreEqual(100m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_ULongNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", (ulong)100);
        _library.SetMin(group, "test", (ulong?)null);
        Assert.AreEqual(100m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_FloatValue_ReturnsMin()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", 5.5f);
        _library.SetMin(group, "test", 1.5f);
        _library.SetMin(group, "test", 3.5f);
        Assert.AreEqual(1.5m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_FloatNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", 1.5f);
        _library.SetMin(group, "test", (float?)null);
        Assert.AreEqual(1.5m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_DoubleValue_ReturnsMin()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", 5.5);
        _library.SetMin(group, "test", 1.5);
        _library.SetMin(group, "test", 3.5);
        Assert.AreEqual(1.5m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_DoubleNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", 1.5);
        _library.SetMin(group, "test", (double?)null);
        Assert.AreEqual(1.5m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_AllNullValues_ReturnsMaxValue()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", (decimal?)null);
        _library.SetMin(group, "test", (decimal?)null);
        Assert.AreEqual(decimal.MaxValue, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetMin_NegativeValues_ReturnsSmallestNegative()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", -50m);
        _library.SetMin(group, "test", -200m);
        _library.SetMin(group, "test", -100m);
        Assert.AreEqual(-200m, _library.Min(group, "test"));
    }

    #endregion

    #region SetSum Tests for Additional Types

    [TestMethod]
    public void SetSum_SByteValue_ReturnsSum()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetSum(group, "test", 10);
        _library.SetSum(group, "test", -5);
        _library.SetSum(group, "test", 20);
        Assert.AreEqual(25m, _library.Sum(group, "test"));
    }

    [TestMethod]
    public void SetSum_SByteNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetSum(group, "test", 10);
        _library.SetSum(group, "test", null);
        _library.SetSum(group, "test", 5);
        Assert.AreEqual(15m, _library.Sum(group, "test"));
    }

    #endregion

    #region SetAvg Tests for Edge Cases

    [TestMethod]
    public void SetAvg_SByteValue_ReturnsAverage()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetAvg(group, "test", 10);
        _library.SetAvg(group, "test", 20);
        _library.SetAvg(group, "test", 30);
        Assert.AreEqual(20m, _library.Avg(group, "test"));
    }

    [TestMethod]
    public void SetAvg_SByteNull_IgnoresNull()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetAvg(group, "test", 10);
        _library.SetAvg(group, "test", null);
        _library.SetAvg(group, "test", 20);
        Assert.AreEqual(15m, _library.Avg(group, "test"));
    }

    [TestMethod]
    public void SetAvg_AllNullValues_ThrowsDivideByZero()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetAvg(group, "test", (decimal?)null);
        _library.SetAvg(group, "test", (decimal?)null);

        Assert.Throws<DivideByZeroException>(() => _library.Avg(group, "test"));
    }

    [TestMethod]
    public void SetAvg_SingleValue_ReturnsThatValue()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetAvg(group, "test", 42m);
        Assert.AreEqual(42m, _library.Avg(group, "test"));
    }

    #endregion

    #region Multiple Groups Tests

    [TestMethod]
    public void SetCount_MultipleGroups_IndependentCounts()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "group1", (byte)1);
        _library.SetCount(group, "group1", (byte)2);
        _library.SetCount(group, "group2", (byte)10);
        Assert.AreEqual(2, _library.Count(group, "group1"));
        Assert.AreEqual(1, _library.Count(group, "group2"));
    }

    [TestMethod]
    public void SetMax_MultipleGroups_IndependentMax()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "group1", 10m);
        _library.SetMax(group, "group1", 5m);
        _library.SetMax(group, "group2", 100m);
        Assert.AreEqual(10m, _library.Max(group, "group1"));
        Assert.AreEqual(100m, _library.Max(group, "group2"));
    }

    [TestMethod]
    public void SetMin_MultipleGroups_IndependentMin()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "group1", 10m);
        _library.SetMin(group, "group1", 5m);
        _library.SetMin(group, "group2", 1m);
        Assert.AreEqual(5m, _library.Min(group, "group1"));
        Assert.AreEqual(1m, _library.Min(group, "group2"));
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void SetMax_ZeroValue_HandledCorrectly()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMax(group, "test", 0m);
        _library.SetMax(group, "test", -10m);
        _library.SetMax(group, "test", -5m);
        Assert.AreEqual(0m, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMin_ZeroValue_HandledCorrectly()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetMin(group, "test", 0m);
        _library.SetMin(group, "test", 10m);
        _library.SetMin(group, "test", 5m);
        Assert.AreEqual(0m, _library.Min(group, "test"));
    }

    [TestMethod]
    public void SetSum_ZeroValue_IncludedInSum()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetSum(group, "test", 10m);
        _library.SetSum(group, "test", 0m);
        _library.SetSum(group, "test", 5m);
        Assert.AreEqual(15m, _library.Sum(group, "test"));
    }

    [TestMethod]
    public void SetCount_ByteMaxValue_CountsCorrectly()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        _library.SetCount(group, "test", byte.MaxValue);
        _library.SetCount(group, "test", byte.MinValue);
        Assert.AreEqual(2, _library.Count(group, "test"));
    }

    [TestMethod]
    public void SetMax_LargeDecimalValue_HandledCorrectly()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        var largeValue = 79228162514264337593543950335m;
        _library.SetMax(group, "test", 1m);
        _library.SetMax(group, "test", largeValue);
        Assert.AreEqual(largeValue, _library.Max(group, "test"));
    }

    [TestMethod]
    public void SetMin_SmallDecimalValue_HandledCorrectly()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        var smallValue = -79228162514264337593543950335m;
        _library.SetMin(group, "test", 1m);
        _library.SetMin(group, "test", smallValue);
        Assert.AreEqual(smallValue, _library.Min(group, "test"));
    }

    #endregion
}
