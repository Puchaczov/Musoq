namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateCoreSamples()
    {
        return
        [
            Basic("Q01_SimpleSelectWhere", "Scan", "select Name, Population from #A.entities() where Population > 0"),
            Basic("Q02_CteDownstream", "CTE", "with cte as (select Name, Population from #A.entities() where Population > 0) select Name, Population from cte"),
            Basic("Q03_InnerJoin", "Join", "select a.Name, b.Country from #A.entities() a inner join #A.entities() b on a.Id = b.Id"),
            Basic("Q04_LeftJoin", "Join", "select a.Name, b.Country from #A.entities() a left outer join #A.entities() b on a.Id = b.Id"),
            Basic("Q05_GroupBySingle", "Grouping", "select City, Count(City) from #A.entities() group by City"),
            Basic("Q06_GroupByMulti", "Grouping", "select City, Country, Count(Name) from #A.entities() group by City, Country"),
            Basic("Q07_GroupByHavingOrderBy", "Grouping", "select City, Country, Sum(Population) from #A.entities() group by City, Country having Count(City) > 0 order by Sum(Population) desc"),
            Basic("Q08_Distinct", "Set", "select distinct City, Country from #A.entities()"),
            Basic("Q09_Union", "Set", "select Name from #A.entities() union (Name) select Name from #A.entities()"),
            Basic("Q10_WindowRowNumber", "Window", "select Name, City, RowNumber() over (partition by City order by Population desc) as rn from #A.entities()"),
            Basic("Q11_WindowSumAggregate", "Window", "select Name, City, Sum(Population) over (partition by City) as total from #A.entities()"),
            Basic("Q12_WindowLag", "Window", "select Name, City, Lag(Population, 1) over (partition by City order by Population desc) as prev from #A.entities()"),
            Basic("Q13_MultipleWindows", "Window", "select Name, City, RowNumber() over (partition by City order by Population desc) as rn, Sum(Population) over (partition by City) as total from #A.entities()"),
            Basic("Q14_Except", "Set", "select Name from #A.entities() except (Name) select Name from #A.entities()"),
            Basic("Q15_CteWithJoin", "CTE", "with cte as (select Name, City, Population from #A.entities() where Population > 0) select c.Name, a.Country from cte c inner join #A.entities() a on c.Name = a.Name"),
            BinaryInterpretation(),
            TextInterpretation(),
            BinaryConditionalInterpretation(),
            BinaryStringInterpretation(),
            BinaryComputedInterpretation(),
            BinaryNestedInterpretation(),
            BinaryInlineArrayInterpretation(),
            BinaryStringRepeatUntilInterpretation(),
            BinaryInlineRepeatUntilInterpretation(),
            BinaryGenericInterpretation(),
            BinaryNestedGenericInterpretation(),
            BinaryBitsRepeatUntilInterpretation(),
            BenchmarkMultipleFilesInterpretation(),
            BenchmarkHighThroughputInterpretation(),
            ChainedApplyGroupedAggregateWindow(),
            AccessMethodApply(),
            OuterAccessMethodApply(),
            CteBackedAsOfJoin(),
            AggregateOverHashJoin(),
            CteBackedAggregateOverHashJoin(),
            DynamicCteBackedAsOfJoin(),
            ChainedApplyWindow(),
            ChainedApplyMixedDistinctAggregateSort(),
            ChainedApplyMixedDistinctMinMaxAggregateSort(),
            ChainedApplyMixedDistinctAvgAggregateSort(),
            ChainedApplyMixedDistinctMinMaxAggregateWindow(),
            ChainedApplyMixedDistinctAvgAggregateWindow(),
            ChainedApplyQualifyWindow(),
            ChainedApplyGroupedAggregateQualifyWindow(),
            ApplyWithOrdinality(),
            CrossApplyWhereLeftGuard(),
            ChainedCrossApplyScopedGuards(),
            ChainedCrossApplyResidualPredicate(),
            OuterApplyWhereLeftGuard(),
            CrossApplyMethodWhereLeftGuard(),
            BasicWithOptions(
                "Q76_NonEquiJoinSortMergeEnabled",
                "Join",
                "select a.Name, b.Name from #A.entities() a inner join #A.entities() b on a.Population > b.Population + 950",
                new CompilationOptions(useSortMergeJoin: true)),
            BasicWithOptions(
                "Q77_NonEquiJoinSortMergeDisabled",
                "Join",
                "select a.Name, b.Name from #A.entities() a inner join #A.entities() b on a.Population > b.Population + 950",
                new CompilationOptions(useSortMergeJoin: false)),
            Basic("Q78_WindowSumWholePartitionDecimal", "Window", "select Name, City, Sum(ToDecimal(Population)) over (partition by City) as total from #A.entities()"),
            Basic("Q79_WindowSumRunningDecimal", "Window", "select Name, City, Sum(ToDecimal(Population)) over (partition by City order by Population) as running from #A.entities()"),
            Basic("Q80_WindowAvgRunningDecimal", "Window", "select Name, City, Avg(ToDecimal(Population)) over (partition by City order by Population) as running_avg from #A.entities()"),
            Basic("Q81_WindowRunningProductPlugin", "Window", "select Name, City, RunningProduct(ToDecimal(Population)) over (partition by City order by Population) as running_product from #A.entities()"),
            BasicWithOptions(
                "Q82_ParallelIndependentCtes",
                "CTE",
                "with p as (select Name as Name from #A.entities()), q as (select Name as Name from #B.entities()) select p.Name, q.Name from p inner join q on p.Name = q.Name",
                new CompilationOptions(useCteParallelization: true)),
            Basic(
                "Q83_CompositeHashJoin",
                "Join",
                "select a.Name, b.Name from #A.entities() a inner join #B.entities() b on a.Id = b.Id and a.Population = b.Population"),
            Basic(
                "Q84_RepeatedCteSelfJoin",
                "CTE",
                "with c as (select Name, City from #A.entities()) select l.Name, r.City from c l inner join c r on l.Name = r.Name"),
            Basic(
                "Q85_OrderByTake",
                "Pagination",
                "select Name, Population from #A.entities() order by Population desc take 5"),
            Basic(
                "Q86_WindowRunningProductFramedPlugin",
                "Window",
                "select Name, City, RunningProduct(ToDecimal(Population)) over (partition by City order by Population rows between unbounded preceding and current row) as running_product from #A.entities()"),
            Basic(
                "Q165_WindowAggregateFilter",
                "Window",
                "select Name, Sum(Population) filter (where Population > 100) over (order by Name) as FilteredPopulation from #A.entities()"),
            Basic(
                "Q166_WindowRangeFrame",
                "Window",
                "select Name, Sum(Population) over (order by Population range between 100 preceding and current row) as RangePopulation from #A.entities()"),
            Basic(
                "Q167_MultiSourceAggregateOwnerResolution",
                "Grouping",
                "select a.Country, Count(a.Name) as NameCount from #A.entities() a inner join #B.entities() b on a.Id = b.Id group by a.Country"),
            Basic(
                "Q168_IsDistinctFromNullSafeComparison",
                "Values",
                @"from values {
    { Label: 'same', LeftValue: 1, RightValue: 1 },
    { Label: 'different', LeftValue: 1, RightValue: 2 },
    { Label: 'both-null', LeftValue: null, RightValue: null },
    { Label: 'left-null', LeftValue: null, RightValue: 3 }
} pairs
select pairs.Label,
       pairs.LeftValue is distinct from pairs.RightValue as IsDifferent,
       pairs.LeftValue is not distinct from pairs.RightValue as IsSame"),
            Basic(
                "Q169_NullsFirstLastOrdering",
                "Ordering",
                "select Name, City, NullableValue from #A.entities() order by NullableValue nulls last, City desc nulls first"),
            Basic(
                "Q170_SelectStarRename",
                "Scan",
                "select * replace (Population * 2 as Population) rename (Name as EntityName, Population as WeightedPopulation) from #A.entities()"),
            Basic(
                "Q175_DescQuery",
                "Description",
                "desc query (select Name as PersonName, Population + Money as Total from #A.entities())"),
            Basic(
                "Q242_WindowDistributionRankings",
                "Window",
                "select Name, City, PercentRank() over (partition by City order by NullableValue desc nulls last, Country) as PercentRankValue, CumeDist() over (partition by City order by NullableValue desc nulls last, Country) as CumeDistValue from #A.entities()"),
            Basic(
                "Q87_CompilationSimpleSelect",
                "Compilation",
                "select City, Country, Population from #A.Entities() where Population > 500000"),
            Basic(
                "Q88_CompilationComplexGroupedSort",
                "Compilation",
                "select City, Country, Population, City + ' (' + Country + ')' as CityCountry from #A.Entities() where Population > 500000 group by City, Country, Population having Count(City) > 0 order by Population desc"),
            BasicWithOptions(
                "Q89_CompilationCseDisabled",
                "Compilation",
                "select Population as LeftPopulation, Population as RightPopulation from #A.Entities() where Population = Population",
                new CompilationOptions(useCommonSubexpressionElimination: false)),
            BasicWithOptions(
                "Q90_CompilationCseEnabled",
                "Compilation",
                "select Population as LeftPopulation, Population as RightPopulation from #A.Entities() where Population = Population",
                new CompilationOptions(useCommonSubexpressionElimination: true)),
            Basic(
                "Q91_OrderBySimple",
                "Ordering",
                "select Name, Population from #A.entities() order by Population"),
            Basic(
                "Q92_OrderByMultipleKeys",
                "Ordering",
                "select City, Country, Name, Population from #A.entities() order by Country, City desc, Population desc, Name"),
            Basic(
                "Q93_OrderByAlias",
                "Ordering",
                "select Name, Population * 2 as WeightedPopulation from #A.entities() order by WeightedPopulation desc, Name"),
            Basic(
                "Q94_OrderByHiddenComputedKey",
                "Ordering",
                "select Name, City from #A.entities() order by Population + Money desc, Country"),
            BasicWithOptions(
                "Q146_CteSidecarHashJoin",
                "CTE",
                "with indexed as (select Id, Name from #A.entities()) select b.Name, i.Name from #B.entities() b inner join indexed i on b.Id = i.Id",
                new CompilationOptions(
                    useHashJoin: true,
                    useSortMergeJoin: false,
                    useCteSidecarIndexes: true)),
            BasicWithOptions(
                "Q147_CteSidecarKeySetSemiJoin",
                "CTE",
                "with indexed as (select Id from #A.entities()) select b.Name from #B.entities() b semi join indexed i on b.Id = i.Id",
                new CompilationOptions(
                    useHashJoin: true,
                    useSortMergeJoin: false,
                    useCteSidecarIndexes: true)),
            BasicWithOptions(
                "Q148_CteSidecarFanoutThreeHashes",
                "CTE",
                "with names as (select Id, Name from #A.entities()), cities as (select Id, City from #A.entities()), countries as (select Id, Country from #A.entities()) select b.Name, n.Name, c.City, co.Country from #B.entities() b inner join names n on b.Id = n.Id inner join cities c on b.Id = c.Id inner join countries co on b.Id = co.Id",
                new CompilationOptions(
                    useHashJoin: true,
                    useSortMergeJoin: false,
                    useCteSidecarIndexes: true)),
            BasicWithOptions(
                "Q149_CteSidecarStagedGraphMixed",
                "CTE",
                "with raw as (select Id, Name, City, Country, Population from #A.entities()), names as (select Id, Name from raw), cities as (select Id, City from raw), eligible as (select Id from raw where Population > 0), joined as (select b.Id, n.Name, c.City from #B.entities() b inner join names n on b.Id = n.Id inner join cities c on b.Id = c.Id) select j.Id, j.Name, j.City from joined j semi join eligible e on j.Id = e.Id",
                new CompilationOptions(
                    useHashJoin: true,
                    useSortMergeJoin: false,
                    useCteParallelization: false,
                    useCteSidecarIndexes: true))
        ];
    }
}
