namespace Musoq.Tests.Common;

/// <summary>
/// Query text shared by the evaluator performance samples and their executable
/// benchmark harness.
/// </summary>
public static class EvaluatorPerformanceQueries
{
    public const string Q227 =
        """
        select a.City as City, Count(b.Name) as MatchCount
        from #A.entities() a
        inner join #B.entities() b on a.City = b.City
        group by a.City
        """;

    public const string Q228 =
        """
        SELECT a.Name,
               CASE WHEN EXISTS (
                   SELECT b.City FROM #B.entities() b
                   WHERE b.Name = a.Name
                     AND b.City = a.City
                     AND b.Country = a.Country
                     AND b.Population = a.Population
                     AND b.Month = a.Month
                     AND b.Money = a.Money
                     AND b.Id = a.Id
                     AND b.NullableValue = a.NullableValue
               ) THEN 'Y' ELSE 'N' END AS ExistsResult,
               CASE WHEN NOT EXISTS (
                   SELECT b.City FROM #B.entities() b
                   WHERE b.Name = a.Name
                     AND b.City = a.City
                     AND b.Country = a.Country
                     AND b.Population = a.Population
                     AND b.Month = a.Month
                     AND b.Money = a.Money
                     AND b.Id = a.Id
                     AND b.NullableValue = a.NullableValue
               ) THEN 'Y' ELSE 'N' END AS NotExistsResult,
               (
                   SELECT b.City FROM #B.entities() b
                   WHERE b.Name = a.Name
                     AND b.City = a.City
                     AND b.Country = a.Country
                     AND b.Population = a.Population
                     AND b.Month = a.Month
                     AND b.Money = a.Money
                     AND b.Id = a.Id
                     AND b.NullableValue = a.NullableValue
               ) AS Lookup
        FROM #A.entities() a
        ORDER BY a.Name
        """;

    public const string Q229 =
        """
        with ranked as (
            select Name, Country
            from #A.entities()
        )
        select Name, Country,
               RowNumber() over (partition by Country order by Name) as BranchRank
        from ranked
        union (Name, Country, BranchRank)
            select Name, Country,
                   RowNumber() over (partition by Country order by Name) as BranchRank
            from #B.entities()
        order by Country, BranchRank, Name
        """;

    public const string Q230 =
        """
        select Name, City, Population
        from #A.entities()
        where Population > 0
        """;
}
