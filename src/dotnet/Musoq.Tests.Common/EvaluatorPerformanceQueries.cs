namespace Musoq.Tests.Common;

/// <summary>
/// Query text shared by the evaluator performance samples and their executable
/// benchmark harness.
/// </summary>
public static class EvaluatorPerformanceQueries
{
    public const string Q227 = (
        "select a.City as City, Count(b.Name) as MatchCount\n"
        + "from #A.entities() a\n"
        + "inner join #B.entities() b on a.City = b.City\n"
        + "group by a.City"
    );

    public const string Q228 = (
        "SELECT a.Name,\n"
        + "       CASE WHEN EXISTS (\n"
        + "           SELECT b.City FROM #B.entities() b\n"
        + "           WHERE b.Name = a.Name\n"
        + "             AND b.City = a.City\n"
        + "             AND b.Country = a.Country\n"
        + "             AND b.Population = a.Population\n"
        + "             AND b.Month = a.Month\n"
        + "             AND b.Money = a.Money\n"
        + "             AND b.Id = a.Id\n"
        + "             AND b.NullableValue = a.NullableValue\n"
        + "       ) THEN 'Y' ELSE 'N' END AS ExistsResult,\n"
        + "       CASE WHEN NOT EXISTS (\n"
        + "           SELECT b.City FROM #B.entities() b\n"
        + "           WHERE b.Name = a.Name\n"
        + "             AND b.City = a.City\n"
        + "             AND b.Country = a.Country\n"
        + "             AND b.Population = a.Population\n"
        + "             AND b.Month = a.Month\n"
        + "             AND b.Money = a.Money\n"
        + "             AND b.Id = a.Id\n"
        + "             AND b.NullableValue = a.NullableValue\n"
        + "       ) THEN 'Y' ELSE 'N' END AS NotExistsResult,\n"
        + "       (\n"
        + "           SELECT b.City FROM #B.entities() b\n"
        + "           WHERE b.Name = a.Name\n"
        + "             AND b.City = a.City\n"
        + "             AND b.Country = a.Country\n"
        + "             AND b.Population = a.Population\n"
        + "             AND b.Month = a.Month\n"
        + "             AND b.Money = a.Money\n"
        + "             AND b.Id = a.Id\n"
        + "             AND b.NullableValue = a.NullableValue\n"
        + "       ) AS Lookup\n"
        + "FROM #A.entities() a\n"
        + "ORDER BY a.Name"
    );

    public const string Q229 = (
        "with ranked as (\n"
        + "    select Name, Country\n"
        + "    from #A.entities()\n"
        + ")\n"
        + "select Name, Country,\n"
        + "       RowNumber() over (partition by Country order by Name) as BranchRank\n"
        + "from ranked\n"
        + "union (Name, Country, BranchRank)\n"
        + "    select Name, Country,\n"
        + "           RowNumber() over (partition by Country order by Name) as BranchRank\n"
        + "    from #B.entities()\n"
        + "order by Country, BranchRank, Name"
    );

    public const string Q230 = ("select Name, City, Population\n" + "from #A.entities()\n" + "where Population > 0");
}
