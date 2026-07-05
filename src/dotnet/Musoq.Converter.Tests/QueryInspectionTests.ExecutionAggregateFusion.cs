using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenInlineAggregateInputsDiffer_ShouldSkipAggregateFusion()
    {
        var result = Inspect(
            """
            with values as (
                select 'A' as Country, 10 as Population, 5 as Money, 'Alpha' as Name from #system.dual()
                union all (Country, Population, Money, Name) select 'A' as Country, 20 as Population, 7 as Money, 'Beta' as Name from #system.dual()
            )
            select Country, Min(Population), Max(Money) from values group by Country
            """,
            new CompilationOptions());

        AssertGeneratedCSharpContains("var __agg0Input = (int?)money;", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("var __agg1Input = (int?)population;", result.GeneratedCSharpCode);
        Assert.AreEqual(2, CountOccurrences(result.GeneratedCSharpCode, "GetValueOrDefault();"));
    }

    [TestMethod]
    public void CompileForInspection_WhenInlineAggregateFiltersDiffer_ShouldSkipAggregateFusion()
    {
        var result = Inspect(
            """
            with values as (
                select 'A' as Country, 10 as Population from #system.dual()
                union all (Country, Population) select 'A' as Country, 20 as Population from #system.dual()
            )
            select Country, Min(Population) filter (where Population > 0), Max(Population) filter (where Population > 10) from values group by Country
            """,
            new CompilationOptions());

        AssertGeneratedCSharpContains("var __agg0Input = (int?)population;", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("var __agg1Input = (int?)population;", result.GeneratedCSharpCode);
        Assert.AreEqual(2, CountOccurrences(result.GeneratedCSharpCode, "GetValueOrDefault();"));
    }

    [TestMethod]
    public void CompileForInspection_WhenCustomKernelSeparatesInlineAggregates_ShouldSkipAggregateFusionAcrossCustomKernel()
    {
        var result = Inspect(
            "select d.Dummy, Min(1), CustomLengthTotal(Length(d.Dummy)), Max(1) from #system.dual() d group by d.Dummy",
            new CompilationOptions());

        AssertGeneratedCSharpContains("CustomLengthTotalAggregate.Set(ref group.__agg1", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("var __agg0Input", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("var __agg2Input", result.GeneratedCSharpCode);
        Assert.AreEqual(2, CountOccurrences(result.GeneratedCSharpCode, "GetValueOrDefault();"));
    }
}
