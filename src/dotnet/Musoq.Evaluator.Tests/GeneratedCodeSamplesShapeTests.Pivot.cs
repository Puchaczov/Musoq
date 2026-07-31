using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void PivotSamples_WhenCheckedIn_ShouldLowerToAggregateProjection()
    {
        var samples = ReadNamedSamples(PivotSampleFileNames).ToDictionary(static sample => sample.FileName);

        foreach (var fileName in PivotSampleFileNames)
        {
            var content = samples[fileName].Content;

            Assert.Contains("pivot #A.entities()", content, fileName);
            Assert.Contains("PhysicalSingleKeyAggregate [", content, fileName);
            Assert.Contains("TypedAggregateSet [Set(", content, fileName);
            Assert.Contains("filter (where Month =", content, fileName);
            Assert.IsFalse(content.Contains("casewhenMonth", StringComparison.OrdinalIgnoreCase), fileName);
            Assert.IsFalse(content.Contains("PhysicalPivot", StringComparison.Ordinal), fileName);
            Assert.IsFalse(content.Contains("CreatePivot", StringComparison.Ordinal), fileName);
        }
    }

    [TestMethod]
    public void PivotSamples_WhenCheckedIn_ShouldExposeSingleMultipleAndCteShapes()
    {
        var samples = ReadNamedSamples(
                PivotGroupedSingleMeasureSampleFileName,
                PivotMultipleMeasuresSampleFileName,
                PivotCteNoGroupBySampleFileName)
            .ToDictionary(static sample => sample.FileName);
        var single = samples[PivotGroupedSingleMeasureSampleFileName].Content;
        var multiple = samples[PivotMultipleMeasuresSampleFileName].Content;
        var cte = samples[PivotCteNoGroupBySampleFileName].Content;

        Assert.Contains("new Column(\"City\", typeof(string), 0)", single);
        Assert.Contains("new Column(\"Jan\", typeof(decimal?), 1)", single);
        Assert.Contains("new Column(\"Feb\", typeof(decimal?), 2)", single);
        Assert.Contains("SortShapeRows [result -> resultSorted by City ASC]", single);

        Assert.Contains("new Column(\"Jan_Sales\", typeof(decimal?), 1)", multiple);
        Assert.Contains("new Column(\"Jan_Orders\", typeof(long), 2)", multiple);
        Assert.Contains("new Column(\"Feb_Sales\", typeof(decimal?), 3)", multiple);
        Assert.Contains("new Column(\"Feb_Orders\", typeof(long), 4)", multiple);
        Assert.Contains("Count(*) filter (where Month = 'Jan')", multiple);
        Assert.Contains("Sum(Money) filter (where Month = 'Jan')", multiple);

        Assert.Contains("PhysicalSingleKeyAggregate [key: 1 (Int16)]", cte);
        Assert.Contains("StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]", cte);
        Assert.Contains("ForEach [p in _cteRowResults.Slot0]", cte);
        Assert.Contains("new Column(\"Jan\", typeof(decimal?), 0)", cte);
        Assert.Contains("new Column(\"Feb\", typeof(decimal?), 1)", cte);
    }
}
