using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void ValuesStaticParametersAndLetsSample_WhenCheckedIn_ShouldUseTypedParameterAndLetReads()
    {
        var sample = ReadSamples().Single(static item =>
            item.FileName == ValuesStaticParametersAndLetsSampleFileName);

        Assert.Contains("var paramBaseScore = ScriptParameterBinder.GetRequired<int>(__musoqExecutionState.Parameters, \"baseScore\");", sample.Content);
        Assert.Contains("var paramSuffix = ScriptParameterBinder.GetOptional<string>(__musoqExecutionState.Parameters, \"suffix\", \"-ok\");", sample.Content);
        Assert.Contains("const int letBonus = 5;", sample.Content);
        Assert.Contains("paramBaseScore + letBonus", sample.Content);
        Assert.Contains("new Column(\"scores.Name\", typeof(string), 0)", sample.Content);
        Assert.Contains("new Column(\"scores.Score\", typeof(int), 1)", sample.Content);
        Assert.Contains("scoresValues", sample.Content);
        Assert.IsFalse(sample.Content.Contains("System.Reflection", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains(GetColumnValuePattern, StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains(ObjectResolverPattern, StringComparison.Ordinal), sample.FileName);
    }
}
