using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void UnpivotSamples_WhenCheckedIn_ShouldUseStreamedGeneratedRows()
    {
        var samples = ReadSamples().ToDictionary(static sample => sample.FileName);

        foreach (var fileName in UnpivotSampleFileNames)
        {
            var content = samples[fileName].Content;
            var generatedCode = ExtractGeneratedCodeSection(content);

            Assert.Contains("PhysicalUnpivot [", content, fileName);
            Assert.Contains("ScopedBlock", content, fileName);
            Assert.Contains("CreateGeneratedRow [__unpivot <-", content, fileName);
            Assert.Contains("new __unpivotUnpivot", generatedCode, fileName);
            Assert.IsFalse(content.Contains("CreateUnpivotRows", StringComparison.Ordinal), fileName);
            Assert.IsFalse(content.Contains("ExecutionCreateUnpivotRows", StringComparison.Ordinal), fileName);
            Assert.IsFalse(content.Contains("__unpivotRows", StringComparison.Ordinal), fileName);
        }
    }

    [TestMethod]
    public void UnpivotCompositionSamples_WhenCheckedIn_ShouldExposeCompositionShapes()
    {
        var samples = ReadSamples().ToDictionary(static sample => sample.FileName);
        var cte = samples[UnpivotCteNullableOrderingSampleFileName].Content;
        var setOperator = samples[UnpivotSetOperatorSampleFileName].Content;

        Assert.Contains("StoreTable [cte0 ->", cte);
        Assert.Contains("Sort [u.Label, u.Metric]", cte);
        Assert.Contains("Skip [1]", cte);
        Assert.Contains("Take [5]", cte);

        Assert.Contains("PhysicalSetOp [UnionAll]", setOperator);
        Assert.AreEqual(2, CountOccurrences(setOperator, "PhysicalUnpivot ["));
        Assert.AreEqual(2, CountOccurrences(setOperator, "CreateGeneratedRow [__unpivot <-"));
        Assert.Contains("Sort [__unpivot.Name]", setOperator);
    }
}
