namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{

    private static GeneratedCodeSample ChainedApplyGroupedAggregateWindow()
    {
        return new GeneratedCodeSample
        {
            Name = "Q61_ChainedApplyGroupedAggregateWindow",
            FileName = "Q61_ChainedApplyGroupedAggregateWindow.cs",
            Query = "select i.Name as Name, Sum(n.Value) as ValueSum, RowNumber() over (order by Sum(n.Value) desc, i.Name) as GroupRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by GroupRowNo",
            Category = "Apply",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateGeneratedApplySchemaProvider
        };
    }

    private static GeneratedCodeSample AccessMethodApply()
    {
        return new GeneratedCodeSample
        {
            Name = "Q62_AccessMethodApply",
            FileName = "Q62_AccessMethodApply.cs",
            Query = "select i.Name as Name, s.Value as Text from #apply.items() i cross apply i.JustReturnArrayOfString() s",
            Category = "Apply",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateGeneratedApplySchemaProvider
        };
    }

    private static GeneratedCodeSample OuterAccessMethodApply()
    {
        return new GeneratedCodeSample
        {
            Name = "Q63_OuterAccessMethodApply",
            FileName = "Q63_OuterAccessMethodApply.cs",
            Query = "select i.Name as Name, s.Value as Text from #apply.items() i outer apply i.JustReturnArrayOfString() s",
            Category = "Apply",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateGeneratedApplySchemaProvider
        };
    }

    private static GeneratedCodeSample CteBackedAsOfJoin()
    {
        return Basic(
            "Q64_CteBackedAsOfJoin",
            "Join",
            "with rightCte as (select e.Name as Name, e.Population as Population from #A.entities() e) select a.Name, a.Population, r.Name, r.Population from #A.entities() a asof join rightCte r on a.Population >= r.Population");
    }

    private static GeneratedCodeSample AggregateOverHashJoin()
    {
        return Basic(
            "Q65_AggregateOverHashJoin",
            "Grouping",
            "select a.City as City, Count(b.Name) as MatchCount from #A.entities() a inner join #B.entities() b on a.City = b.City group by a.City");
    }

    private static GeneratedCodeSample CteBackedAggregateOverHashJoin()
    {
        return Basic(
            "Q66_CteBackedAggregateOverHashJoin",
            "Grouping",
            "with leftCte as (select a.City as City from #A.entities() a), rightCte as (select b.City as City, b.Name as Name from #B.entities() b) select l.City as City, Count(r.Name) as MatchCount from leftCte l inner join rightCte r on l.City = r.City group by l.City");
    }

    private static GeneratedCodeSample DynamicCteBackedAsOfJoin()
    {
        return new GeneratedCodeSample
        {
            Name = "Q67_DynamicCteBackedAsOfJoin",
            FileName = "Q67_DynamicCteBackedAsOfJoin.cs",
            Query = "with rightCte as (select d.Team as Team, d.Name as Name, d.Score as Score from #dynamic.all() d) select l.Name as LeftName, r.Name as RightName from #dynamic.all() l asof join rightCte r on l.Team = r.Team and l.Score >= r.Score",
            Category = "Join",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateDynamicAsOfSchemaProvider
        };
    }

    private static GeneratedCodeSample ChainedApplyWindow()
    {
        return new GeneratedCodeSample
        {
            Name = "Q68_ChainedApplyWindow",
            FileName = "Q68_ChainedApplyWindow.cs",
            Query = "select i.Name, n.Value as FirstValue, m.Value as SecondValue, RowNumber() over (partition by i.Name order by n.Value, m.Value) as RowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m order by i.Name, RowNo",
            Category = "Apply",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateGeneratedApplySchemaProvider
        };
    }

    private static GeneratedCodeSample ChainedApplyMixedDistinctAggregateSort()
    {
        return new GeneratedCodeSample
        {
            Name = "Q69_ChainedApplyMixedDistinctAggregateSort",
            FileName = "Q69_ChainedApplyMixedDistinctAggregateSort.cs",
            Query = "select i.Name as Name, Sum(n.Value) as RepeatedSum, Sum(distinct n.Value) as DistinctSum from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Sum(distinct n.Value) desc, Sum(n.Value) desc, i.Name",
            Category = "Apply",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateGeneratedApplySchemaProvider
        };
    }

    private static GeneratedCodeSample ChainedApplyMixedDistinctMinMaxAggregateSort()
    {
        return new GeneratedCodeSample
        {
            Name = "Q70_ChainedApplyMixedDistinctMinMaxAggregateSort",
            FileName = "Q70_ChainedApplyMixedDistinctMinMaxAggregateSort.cs",
            Query = "select i.Name as Name, Min(n.Value) as RepeatedMin, Min(distinct n.Value) as DistinctMin, Max(n.Value) as RepeatedMax, Max(distinct n.Value) as DistinctMax from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Max(distinct n.Value) desc, Max(n.Value) desc, Min(distinct n.Value), Min(n.Value), i.Name",
            Category = "Apply",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateMixedDistinctGeneratedApplySchemaProvider
        };
    }

    private static GeneratedCodeSample ChainedApplyMixedDistinctAvgAggregateSort()
    {
        return new GeneratedCodeSample
        {
            Name = "Q71_ChainedApplyMixedDistinctAvgAggregateSort",
            FileName = "Q71_ChainedApplyMixedDistinctAvgAggregateSort.cs",
            Query = "select i.Name as Name, Avg(n.Value) as RepeatedAvg, Avg(distinct n.Value) as DistinctAvg from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Avg(distinct n.Value) desc, Avg(n.Value) desc, i.Name",
            Category = "Apply",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateMixedDistinctGeneratedApplySchemaProvider
        };
    }

    private static GeneratedCodeSample ChainedApplyMixedDistinctMinMaxAggregateWindow()
    {
        return new GeneratedCodeSample
        {
            Name = "Q72_ChainedApplyMixedDistinctMinMaxAggregateWindow",
            FileName = "Q72_ChainedApplyMixedDistinctMinMaxAggregateWindow.cs",
            Query = "select i.Name as Name, Min(n.Value) as RepeatedMin, Min(distinct n.Value) as DistinctMin, Max(n.Value) as RepeatedMax, Max(distinct n.Value) as DistinctMax, RowNumber() over (order by Max(distinct n.Value) desc, Max(n.Value) desc, Min(distinct n.Value), Min(n.Value), i.Name) as MixedMinMaxRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by MixedMinMaxRowNo",
            Category = "Apply",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateMixedDistinctGeneratedApplySchemaProvider
        };
    }

    private static GeneratedCodeSample ChainedApplyMixedDistinctAvgAggregateWindow()
    {
        return new GeneratedCodeSample
        {
            Name = "Q73_ChainedApplyMixedDistinctAvgAggregateWindow",
            FileName = "Q73_ChainedApplyMixedDistinctAvgAggregateWindow.cs",
            Query = "select i.Name as Name, Avg(n.Value) as RepeatedAvg, Avg(distinct n.Value) as DistinctAvg, RowNumber() over (order by Avg(distinct n.Value) desc, Avg(n.Value) desc, i.Name) as MixedAvgRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by MixedAvgRowNo",
            Category = "Apply",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateMixedDistinctGeneratedApplySchemaProvider
        };
    }

    private static GeneratedCodeSample ChainedApplyQualifyWindow()
    {
        return new GeneratedCodeSample
        {
            Name = "Q74_ChainedApplyQualifyWindow",
            FileName = "Q74_ChainedApplyQualifyWindow.cs",
            Query = "select i.Name, n.Value as FirstValue, m.Value as SecondValue from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m qualify RowNumber() over (partition by i.Name order by n.Value, m.Value) <= 1 order by i.Name",
            Category = "Apply",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateGeneratedApplySchemaProvider
        };
    }

    private static GeneratedCodeSample ChainedApplyGroupedAggregateQualifyWindow()
    {
        return new GeneratedCodeSample
        {
            Name = "Q75_ChainedApplyGroupedAggregateQualifyWindow",
            FileName = "Q75_ChainedApplyGroupedAggregateQualifyWindow.cs",
            Query = "select i.Name as Name, Avg(n.Value) as ValueAvg, Min(n.Value) as ValueMin, Max(n.Value) as ValueMax from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name having Max(n.Value) >= 2 qualify RowNumber() over (order by Avg(n.Value) desc, Min(n.Value), Max(n.Value) desc) <= 1 order by Name",
            Category = "Apply",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateGeneratedApplySchemaProvider
        };
    }

    private static GeneratedCodeSample ApplyWithOrdinality()
    {
        return new GeneratedCodeSample
        {
            Name = "Q173_ApplyWithOrdinality",
            FileName = "Q173_ApplyWithOrdinality.cs",
            Query = "select i.Name, n.Value as Number, n.Ordinal as NumberOrdinal from #apply.items() i cross apply i.Numbers n with ordinality order by i.Name, n.Ordinal",
            Category = "Apply",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = CreateGeneratedApplySchemaProvider
        };
    }
}
