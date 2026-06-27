using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void NullsFirstLastOrderingSample_WhenCheckedIn_ShouldUseInlineTypedNullComparisons()
    {
        var sample = ReadSamples().Single(static item =>
            item.FileName == NullsFirstLastOrderingSampleFileName);

        Assert.Contains("NULLS LAST", sample.Content);
        Assert.Contains("NULLS FIRST", sample.Content);
        Assert.Contains("var leftNull0 = !left.NullableValue.HasValue;", sample.Content);
        Assert.Contains("var rightNull0 = !right.NullableValue.HasValue;", sample.Content);
        Assert.Contains("var leftNull1 = left.City == null;", sample.Content);
        Assert.Contains("var rightNull1 = right.City == null;", sample.Content);
        Assert.Contains("Nullable.Compare(left.NullableValue, right.NullableValue)", sample.Content);
        Assert.Contains("StringComparer.Ordinal.Compare(left.City, right.City)", sample.Content);
        Assert.IsFalse(sample.Content.Contains("RowOrderKey", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("System.Reflection", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("Comparer<object>", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("result.Rows.OrderBy", StringComparison.Ordinal), sample.FileName);
    }
}
