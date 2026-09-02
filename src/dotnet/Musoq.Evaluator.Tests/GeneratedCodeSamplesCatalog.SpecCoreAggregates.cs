namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateSpecificationCoreAggregateSamples()
    {
        return
        [
            Basic(
                "Q286_SpecCoreParentLevelAggregate",
                "Grouping",
                "select Country, City, Count(City, 1), Count(City) as CountOfCities " +
                "from #A.entities() group by Country, City"),
            Basic(
                "Q287_SpecCoreAggregateFilter",
                "Grouping",
                "select Country, Count(*) filter (where Population > 200) as FilteredCount " +
                "from #A.entities() group by Country"),
            Basic(
                "Q288_SpecCoreConstantGroup",
                "Grouping",
                "select AggregateValues(Name, ', ') as Names from #A.entities() group by 'const'"),
            Basic(
                "Q289_SpecCorePivotTupleNullDistinct",
                "Pivot",
                "pivot #A.Entities() on Id, Country in ((2000, 'NL') as y2000_nl, (0, null) as Missing) " +
                "using Count(distinct Name) as Customers group by City order by City"),
            Basic(
                "Q290_SpecCoreNamedWindowAnalytics",
                "Window",
                "select Name, Ntile(2) over ranked as Bucket, " +
                "FirstValue(Name) over ranked as FirstName, LastValue(Name) over ranked as LastName, " +
                "NthValue(Name, 1) over ranked as NthName, " +
                "Min(Population) over ranked as MinPopulation, Max(Population) over ranked as MaxPopulation " +
                "from #A.entities() window ranked as (order by Name)"),
            Basic(
                "Q291_SpecCoreSetResultModifiers",
                "Set",
                "select Name as Label from #A.entities() " +
                "union () select Name from #B.entities() " +
                "union all select Name from #C.entities() order by Label desc skip 1 take 3"),
            Basic(
                "Q292_SpecCoreSetBranchLocalSlice",
                "Set",
                "with sliced as (select Name from #A.entities() order by Name take 1) " +
                "select sliced.Name as Label from sliced " +
                "union all select Name from #B.entities() order by Label desc")
        ];
    }
}
