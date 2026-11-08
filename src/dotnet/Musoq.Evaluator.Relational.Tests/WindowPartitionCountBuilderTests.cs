using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class WindowPartitionCountBuilderTests
{
    [TestMethod]
    public void ToResult_WhenReferenceValuesContainNulls_ShouldCountOnlyIncludedRowsPerPartition()
    {
        string?[] partitionKeys = ["eng", "eng", "sales", "eng", "sales", null, null];
        bool[] includeValues = [true, false, true, true, false, true, false];
        var builder = CreateBuilder(partitionKeys, includeValues);

        CollectionAssert.AreEqual(
            new[] { 2, 2, 1, 2, 1, 1, 1 },
            builder.ToResult());
    }

    [TestMethod]
    public void ToResult_WhenAllValuesAreIncluded_ShouldCountRowsPerPartition()
    {
        int[] partitionKeys = [1, 1, 2, 3, 2, 1];
        bool[] includeValues = [true, true, true, true, true, true];
        var builder = new WindowPartitionCountBuilder<int>(partitionKeys.Length);
        for (var rowIndex = 0; rowIndex < partitionKeys.Length; rowIndex++)
            builder.Add(partitionKeys[rowIndex], includeValues[rowIndex], rowIndex);

        CollectionAssert.AreEqual(
            new[] { 3, 3, 2, 1, 2, 3 },
            builder.ToResult());
    }

    [TestMethod]
    public void ToResult_WhenReferenceKeysAreEqualButNotSameInstance_ShouldCountTogether()
    {
        string[] partitionKeys =
        [
            new("eng".ToCharArray()),
            new("eng".ToCharArray()),
            new("sales".ToCharArray()),
            new("eng".ToCharArray())
        ];

        var builder = new WindowPartitionCountBuilder<string>(partitionKeys.Length);
        for (var rowIndex = 0; rowIndex < partitionKeys.Length; rowIndex++)
            builder.Add(partitionKeys[rowIndex], true, rowIndex);

        CollectionAssert.AreEqual(
            new[] { 3, 3, 1, 3 },
            builder.ToResult());
    }

    [TestMethod]
    public void ToResult_WhenDistinctPartitionsExceedLinearLimit_ShouldPromoteAndCount()
    {
        var builder = new WindowPartitionCountBuilder<int>(20);
        for (var rowIndex = 0; rowIndex < 20; rowIndex++)
            builder.Add(rowIndex, true, rowIndex);

        CollectionAssert.AreEqual(
            Enumerable.Repeat(1, 20).ToArray(),
            builder.ToResult());
    }

    [TestMethod]
    public void ToResult_WhenPartitionHashesCollide_ShouldKeepSeparateCounts()
    {
        CollidingKey[] partitionKeys =
        [
            new(1),
            new(2),
            new(1),
            new(3),
            new(2)
        ];

        var builder = new WindowPartitionCountBuilder<CollidingKey>(partitionKeys.Length);
        for (var rowIndex = 0; rowIndex < partitionKeys.Length; rowIndex++)
            builder.Add(partitionKeys[rowIndex], true, rowIndex);

        CollectionAssert.AreEqual(
            new[] { 2, 2, 2, 1, 2 },
            builder.ToResult());
    }

    [TestMethod]
    public void ToResult_WhenRowsAreMissing_ShouldThrow()
    {
        var builder = new WindowPartitionCountBuilder<string>(2);
        builder.Add("eng", true, 0);

        Assert.Throws<InvalidOperationException>(builder.ToResult);
    }

    [TestMethod]
    public void ToResultUnchecked_WhenRowsAreAddedByExactGeneratedLoop_ShouldMatchValidatedResult()
    {
        string?[] partitionKeys = ["eng", "sales", "eng", null, "sales"];
        bool[] includeValues = [true, true, false, true, false];
        var builder = new WindowPartitionCountBuilder<string>(partitionKeys.Length);
        for (var rowIndex = 0; rowIndex < partitionKeys.Length; rowIndex++)
            builder.AddReferenceUnchecked(partitionKeys[rowIndex], includeValues[rowIndex], rowIndex);

        CollectionAssert.AreEqual(
            new[] { 1, 1, 1, 1, 1 },
            builder.ToResultUnchecked());
    }

    [TestMethod]
    public void ToResultInPlaceUnchecked_WhenRowsAreAddedByExactGeneratedLoop_ShouldReusePartitionBufferAsCounts()
    {
        string?[] partitionKeys = ["eng", "sales", "eng", null, "sales"];
        bool[] includeValues = [true, true, false, true, false];
        var builder = new WindowPartitionCountBuilder<string>(partitionKeys.Length);
        for (var rowIndex = 0; rowIndex < partitionKeys.Length; rowIndex++)
            builder.AddReferenceUnchecked(partitionKeys[rowIndex], includeValues[rowIndex], rowIndex);

        var result = builder.ToResultInPlaceUnchecked();

        CollectionAssert.AreEqual(
            new[] { 1, 1, 1, 1, 1 },
            result);
    }

    [TestMethod]
    public void ToCountResult_ShouldExposeCountsWithoutChangingSemantics()
    {
        string?[] partitionKeys = ["eng", "eng", "sales", "eng", null];
        bool[] includeValues = [true, false, true, true, true];
        var builder = CreateBuilder(partitionKeys, includeValues);

        var result = builder.ToCountResult();

        Assert.AreEqual(2, result[0]);
        Assert.AreEqual(2, result[1]);
        Assert.AreEqual(1, result[2]);
        Assert.AreEqual(2, result[3]);
        Assert.AreEqual(1, result[4]);
        CollectionAssert.AreEqual(
            new[] { 2, 2, 1, 2, 1 },
            builder.ToResult());
    }

    [TestMethod]
    public void ToCountResultUnchecked_WhenRowsAreAddedByExactGeneratedLoop_ShouldExposeCounts()
    {
        string?[] partitionKeys = ["eng", "eng", "sales", null];
        bool[] includeValues = [true, false, true, true];
        var builder = new WindowPartitionCountBuilder<string>(partitionKeys.Length);
        for (var rowIndex = 0; rowIndex < partitionKeys.Length; rowIndex++)
            builder.AddReferenceUnchecked(partitionKeys[rowIndex], includeValues[rowIndex], rowIndex);

        var result = builder.ToCountResultUnchecked();

        Assert.AreEqual(1, result[0]);
        Assert.AreEqual(1, result[1]);
        Assert.AreEqual(1, result[2]);
        Assert.AreEqual(1, result[3]);
    }

    private static WindowPartitionCountBuilder<string> CreateBuilder(string?[] partitionKeys, bool[] includeValues)
    {
        var builder = new WindowPartitionCountBuilder<string>(partitionKeys.Length);
        for (var rowIndex = 0; rowIndex < partitionKeys.Length; rowIndex++)
            builder.Add(partitionKeys[rowIndex], includeValues[rowIndex], rowIndex);

        return builder;
    }

    private sealed class CollidingKey(int value)
    {
        private readonly int _value = value;

        public override bool Equals(object? obj)
        {
            return obj is CollidingKey other && other._value == _value;
        }

        public override int GetHashCode()
        {
            return 1;
        }
    }
}
