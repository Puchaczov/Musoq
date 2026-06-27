namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreatePivotSamples()
    {
        return
        [
            Basic(
                "Q162_PivotGroupedSingleMeasure",
                "Pivot",
                @"pivot #A.entities()
                  on Month in ('Jan' as Jan, 'Feb' as Feb)
                  using Sum(Money) as Sales
                  group by City
                  order by City"),
            Basic(
                "Q163_PivotMultipleMeasures",
                "Pivot",
                @"pivot #A.entities()
                  on Month in ('Jan' as Jan, 'Feb' as Feb)
                  using Sum(Money) as Sales, Count(*) as Orders
                  group by City
                  order by City"),
            Basic(
                "Q164_PivotCteNoGroupBy",
                "Pivot",
                @"with p as (
                      pivot #A.entities()
                      on Month in ('Jan' as Jan, 'Feb' as Feb)
                      using Sum(Money) as Sales
                  )
                  select Jan, Feb from p")
        ];
    }
}
