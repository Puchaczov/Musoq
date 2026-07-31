using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void SelectStarRenameSample_WhenCheckedIn_ShouldUseTypedProjectionFields()
    {
        var sample = ReadSample(SelectStarRenameSampleFileName);

        Assert.Contains("rename (Name as EntityName, Population as WeightedPopulation)", sample.Content);
        Assert.Contains("new Column(\"EntityName\", typeof(string), 0)", sample.Content);
        Assert.Contains("new Column(\"WeightedPopulation\", typeof(decimal), 3)", sample.Content);
        Assert.Contains("public string EntityName { get; private set; }", sample.Content);
        Assert.Contains("public decimal WeightedPopulation { get; private set; }", sample.Content);
        Assert.IsFalse(sample.Content.Contains("System.Reflection", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains(GetColumnValuePattern, StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains(ObjectResolverPattern, StringComparison.Ordinal), sample.FileName);
    }
}
