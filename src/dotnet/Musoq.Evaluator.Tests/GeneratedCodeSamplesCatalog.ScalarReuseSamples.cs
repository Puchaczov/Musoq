namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateScalarReuseSamples()
    {
        return
        [
            Basic(
                "Q252_StableFilterProjectionReuse",
                "ScalarReuse",
                "select Name, Population, Population * 2 as Twice from #A.entities() where Population > 0"),
            new GeneratedCodeSample
            {
                Name = "Q253_VolatileFilterProjectionReuse",
                FileName = "Q253_VolatileFilterProjectionReuse.cs",
                Query = "select a.VolatileValue, a.Value from #licm.outers() a where a.VolatileValue > 0",
                Category = "ScalarReuse",
                Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
                CreateSchemaProvider = CreateLoopInvariantSampleSchemaProvider
            },
            Basic(
                "Q254_SharedStableWindowInputs",
                "ScalarReuse",
                "select City, RowNumber() over (partition by City order by Population desc) as rn, Sum(Population) over (partition by City) as total from #A.entities()"),
            new GeneratedCodeSample
            {
                Name = "Q255_VolatileWindowInputs",
                FileName = "Q255_VolatileWindowInputs.cs",
                Query = "select a.VolatileValue, RowNumber() over (order by a.Value) as rn from #licm.outers() a",
                Category = "ScalarReuse",
                Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
                CreateSchemaProvider = CreateLoopInvariantSampleSchemaProvider
            },
            Basic(
                "Q256_ParallelAggregateSharedArguments",
                "ScalarReuse",
                "select City, Sum(Population) as Total, Count(Population) as Rows from #A.entities() group by City") with
            {
                CompilationOptions = new CompilationOptions(useCteParallelization: true)
            },
            Basic(
                "Q257_PivotPredicateDispatch",
                "ScalarReuse",
                @"pivot #A.entities()
                  on Month in ('Jan' as Jan, 'Feb' as Feb)
                  using Sum(Money) as Sales, Count(*) as Orders
                  group by City
                  order by City"),
            new GeneratedCodeSample
            {
                Name = "Q258_GuardedStableApplyPredicate",
                FileName = "Q258_GuardedStableApplyPredicate.cs",
                Query = "select a.Value, b.Value from #licm.outers() a cross apply a.Middles b where a.Value > 0 and b.Value > 0",
                Category = "ScalarReuse",
                Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
                CreateSchemaProvider = CreateLoopInvariantSampleSchemaProvider
            },
            new GeneratedCodeSample
            {
                Name = "Q259_GuardedVolatileOuterApplyPredicate",
                FileName = "Q259_GuardedVolatileOuterApplyPredicate.cs",
                Query = "select a.VolatileValue, b.Value from #licm.outers() a outer apply a.Middles b where a.VolatileValue > 0",
                Category = "ScalarReuse",
                Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
                CreateSchemaProvider = CreateLoopInvariantSampleSchemaProvider
            },
            Basic(
                "Q260_StableAsOfProbeKeys",
                "ScalarReuse",
                "select a.Name, b.Name from #A.entities() a asof join #B.entities() b on a.Population >= b.Population"),
            Basic(
                "Q261_StableRangeJoinKeys",
                "ScalarReuse",
                "select a.Name, b.Name from #A.entities() a inner join #B.entities() b on a.Population >= b.Population and a.Population <= b.Money"),
            Basic(
                "Q262_CorrelatedCteProbeReuse",
                "ScalarReuse",
                "with base as (select Country, Sum(Population) as Total from #B.entities() group by Country) select a.City, b.Total from #A.entities() a inner join base b on b.Country = a.Country"),
            Basic(
                "Q263_StableUnpivotExpansion",
                "ScalarReuse",
                @"unpivot #A.entities() s
                  on Metric in (s.Population as Population, s.Money as Money)
                  using Amount
                  keep s.Country as Country, s.Name as Name"),
            Basic(
                "Q264_BoundaryRowShapeNarrowing",
                "ScalarReuse",
                "select Name, City, Country, Population, Money from #A.entities() order by Population desc take 10"),
            Basic(
                "Q265_SourceComputedProjectionAccepted",
                "ScalarReuse",
                "select Population * 2 as ComputedPopulation, Name from #A.entities()"),
            Basic(
                "Q266_SourceComputedProjectionResidual",
                "ScalarReuse",
                "select Population * 2 as ResidualPopulation, Name from #A.entities() where Population > 0"),
            ScalarReuseRecursiveInvariant()
        ];
    }

    private static GeneratedCodeSample ScalarReuseRecursiveInvariant()
    {
        var source = Recursive("Q224_RecursiveCompositeInvariantSubplan");
        return source with
        {
            Name = "Q267_RecursiveStableScalarInvariant",
            FileName = "Q267_RecursiveStableScalarInvariant.cs",
            Category = "ScalarReuse"
        };
    }
}
