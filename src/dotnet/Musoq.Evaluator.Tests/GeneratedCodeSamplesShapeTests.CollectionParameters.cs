using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void CollectionParameterInMembershipSample_WhenCheckedIn_ShouldUseTypedIndexedLoop()
    {
        var sample = ReadSample(CollectionParameterInMembershipSampleFileName);

        Assert.Contains("param(ids: int[])", sample.Content);
        Assert.Contains(
            "var paramIds = ScriptParameterBinder.GetRequiredCollection<int>(__musoqExecutionState.Parameters, \"ids\");",
            sample.Content);
        Assert.Contains(
            "private static bool CollectionParameterContains<T>(T value, IReadOnlyList<T> values)",
            sample.Content);
        Assert.Contains("var comparer = EqualityComparer<T>.Default;", sample.Content);
        Assert.Contains("for (var index = 0; index < values.Count; index++)", sample.Content);
        Assert.Contains("comparer.Equals(value, values[index])", sample.Content);
        Assert.IsFalse(sample.Content.Contains("Enumerable.Contains", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("System.Reflection", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("Array.IndexOf", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("Comparer<object>", StringComparison.Ordinal), sample.FileName);
    }
}
