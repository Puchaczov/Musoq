namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateClassicTailSamples()
    {
        return
        [
            Basic("Q18_CaseWhen", "Scalar", "select Name, case when Population > 500 then 'large' when Population > 100 then 'medium' else 'small' end as Size from #A.entities()"),
            Basic("Q19_CrossApply", "Apply", "select a.Name, b.Name as ChildName from #A.entities() a cross apply #B.entities() b"),
            Basic("Q20_OuterApply", "Apply", "select a.Name, b.Name as OtherName from #A.entities() a outer apply #B.entities() b"),
            Basic("Q21_RightJoin", "Join", "select a.Name, b.Country from #A.entities() a right outer join #A.entities() b on a.Id = b.Id"),
            Basic("Q22_OrderBySkipTake", "Pagination", "select Name, Population from #A.entities() order by Population desc skip 2 take 5"),
            Basic("Q110_OrderByTopOffsetHiddenKey", "Pagination", "select Name from #A.entities() order by Population + Money desc skip 1 take 3"),
            Basic("Q23_InClause", "InClause", "select Name, City from #A.entities() where City in ('Warsaw', 'Berlin', 'Paris')"),
            Basic("Q24_AggregateNoGroupBy", "Grouping", "select Count(Name), Sum(Population), Min(Population), Max(Population) from #A.entities()"),
            Basic("Q25_UnionAll", "Set", "select Name from #A.entities() union all (Name) select Name from #A.entities()"),
            Basic("Q26_WindowRankDenseRank", "Window", "select Name, City, Rank() over (partition by City order by Population desc) as rnk, DenseRank() over (partition by City order by Population desc) as dense_rnk from #A.entities()"),
            Basic("Q27_WindowLead", "Window", "select Name, City, Lead(Population, 1) over (partition by City order by Population desc) as next_pop from #A.entities()"),
            Basic("Q28_MultipleCteChained", "CTE", "with cte1 as (select Name, City, Population from #A.entities() where Population > 0), cte2 as (select Name, City from cte1 where City is not null) select Name, City from cte2"),
            Basic("Q29_Intersect", "Set", "select Name from #A.entities() intersect (Name) select Name from #A.entities()"),
            Basic("Q30_GroupBySkipTake", "Grouping", "select City, Count(City) from #A.entities() group by City skip 1 take 3"),
            Basic("Q31_GroupByHavingNoOrderBy", "Grouping", "select City, Count(City), Sum(Population) from #A.entities() group by City having Count(City) > 0"),
            Basic("Q32_NonEquiJoin", "Join", "select a.Name, b.Name from #A.entities() a inner join #A.entities() b on a.Population > b.Population"),
            Basic("Q33_AsOfJoin", "Join", "select a.Name, a.Population, b.Name, b.Population from #A.entities() a asof join #A.entities() b on a.Population >= b.Population"),
            Basic("Q174_AsOfTieBreak", "Join", "select a.Name, b.Name, b.Money from #A.entities() a asof join #A.entities() b on a.Population >= b.Population tie break by b.Money desc nulls last"),
            Basic("Q34_InnerJoinWithWhere", "Join", "select a.Name, b.Country from #A.entities() a inner join #A.entities() b on a.Id = b.Id where a.Population > 0"),
            Basic("Q35_LeftJoinWithMultipleColumns", "Join", "select a.Name, a.City, b.Country, b.Population from #A.entities() a left outer join #A.entities() b on a.Id = b.Id"),
            Basic("Q36_InnerJoinWithFunctionCallInSelect", "Join", "select ToUpper(a.Name), b.Country from #A.entities() a inner join #A.entities() b on a.Id = b.Id"),
            Basic("Q37_InnerJoinWithArithmeticInSelect", "Join", "select a.Name, a.Population * 2 from #A.entities() a inner join #A.entities() b on a.Id = b.Id"),
            Basic("Q38_InnerJoinWithCaseWhenInSelect", "Join", "select case when a.Population > 100 then 'Big' else 'Small' end, b.Country from #A.entities() a inner join #A.entities() b on a.Id = b.Id"),
            Basic("Q39_InnerJoinWithStringConcatInSelect", "Join", "select a.Name + ' - ' + b.Country, b.Population from #A.entities() a inner join #A.entities() b on a.Id = b.Id"),
            Basic("Q40_InnerJoinWithCoalesceInSelect", "Join", "select Coalesce(a.Name, 'Unknown'), b.Country from #A.entities() a inner join #A.entities() b on a.Id = b.Id"),
            Basic("Q41_InnerJoinWithMultipleFunctionsInSelect", "Join", "select ToUpper(a.Name), ToLower(b.Country), a.Population + b.Population from #A.entities() a inner join #A.entities() b on a.Id = b.Id"),
            Basic("Q42_InClauseLarge20Values", "InClause", "select Name, City, Country from #A.entities() where Name in ('A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T')"),
            Basic("Q42_WindowFrameRowsSum", "Window", "select Name, Population, Sum(Population) over (order by Name rows between 1 preceding and current row) as RunSum from #A.entities()"),
            Basic("Q43_QualifyBasic", "Window", "select Name, City, RowNumber() over (partition by City order by Population desc) as rn from #A.entities() qualify RowNumber() over (partition by City order by Population desc) <= 2"),
            Basic("Q44_WindowFrameWithJoin", "Window", "select a.Name, b.City, Sum(a.Population) over (order by a.Name rows between 1 preceding and current row) as RunSum from #A.entities() a inner join #A.entities() b on a.Id = b.Id"),
            Basic("Q45_QualifyWithFrameSpec", "Window", "select Name, City, Sum(Population) over (partition by City order by Name rows between unbounded preceding and current row) as RunSum from #A.entities() qualify Sum(Population) over (partition by City order by Name rows between unbounded preceding and current row) > 100"),
            Basic("Q46_WindowFrameWithWhere", "Window", "select Name, Population, Sum(Population) over (order by Name rows between 1 preceding and current row) as RunSum from #A.entities() where Population > 100"),
            BasicWithOptions("Q47_CteJoinFrameQualify", "Window", "with base as ( select Name, City, Population from #A.entities() where Population > 0) select b.Name, a.City, Sum(b.Population) over (partition by a.City order by b.Name rows between unbounded preceding and current row) as RunSum from base b inner join #A.entities() a on b.Name = a.Name qualify Sum(b.Population) over (partition by a.City order by b.Name rows between unbounded preceding and current row) > 100", new CompilationOptions().WithStabilityAwareScalarReuse(false)),
            Basic("Q48_InnerJoinTwoSchemasSameColumnName", "Join", "select a.Name, b.Name from #A.entities() a inner join #B.entities() b on a.Id = b.Id"),
            Basic("Q49_LeftJoinTwoSchemasSameKey", "Join", "select a.Id, b.Id from #A.entities() a left outer join #B.entities() b on a.Id = b.Id"),
            Basic("Q50_CteDistinctJoinByCountry", "CTE", "with cte as (select distinct a.Country as Country from #A.Entities() a inner join #B.Entities() b on a.Country = b.Country) select Country from cte"),
            Basic("Q98_GroupByExpressionNoAlias", "Grouping", "select Country, Substring(City, IndexOf(City, ':')) as 'City', Count(City) as 'Count', Sum(Population) as 'Sum' from #A.Entities() group by Substring(City, IndexOf(City, ':')), Country"),
            Basic("Q99_BitwiseShiftLeftIntDebug", "Scalar", "select ShiftLeft(1i, 1) from #A.entities()"),
            Basic("Q99_ExceptWithGroupBySides", "Set", "select City, Sum(Population) from #A.Entities() group by City except (City) select City, Sum(Population) from #B.Entities() group by City except (City) select City, Sum(Population) from #C.Entities() group by City"),
            Basic("Q99_InSubqueryBasic", "InClause", "SELECT a.City FROM #A.entities() a WHERE a.City IN (SELECT b.City FROM #B.entities() b)"),
            Basic("Q99_Union3WithGroupBySides", "Set", "select City, Sum(Population) from #A.Entities() group by City union (City) select City, Sum(Population) from #B.Entities() group by City union (City) select City, Sum(Population) from #C.Entities() group by City"),
            Basic("Q99_UnionWithGroupBySides", "Set", "select City, Sum(Population) from #A.Entities() group by City union (City) select City, Sum(Population) from #A.Entities() group by City")
        ];
    }
}
