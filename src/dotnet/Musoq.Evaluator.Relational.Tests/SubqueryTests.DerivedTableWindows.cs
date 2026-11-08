using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenQualifiedWindowDerivedTableIsLeftJoined_ShouldPreserveAliasesAndUnmatchedLookup()
    {
        const string query = @"
            SELECT d.CountryCode as Country, d.TopCity as City,
                   d.CityPopulation as Population, d.CityRank as Rank,
                   l.Name as LookupName
            FROM (
                SELECT a.Country as CountryCode, a.City as TopCity,
                       a.Population as CityPopulation,
                       RowNumber() over (partition by a.Country order by a.Population desc, a.City) as CityRank
                FROM #A.entities() a
                QUALIFY RowNumber() over (partition by a.Country order by a.Population desc, a.City) = 1
            ) d
            LEFT OUTER JOIN #B.entities() l ON d.CountryCode = l.Country
            ORDER BY d.CountryCode";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "PL", 500),
                    new BasicEntity("Krakow", "PL", 300),
                    new BasicEntity("Berlin", "DE", 250),
                    new BasicEntity("Munich", "DE", 350),
                    new BasicEntity("Lyon", "FR", 200),
                    new BasicEntity("Paris", "FR", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("Germany") { Country = "DE" },
                    new BasicEntity("Poland") { Country = "PL" }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)),
            ("Population", typeof(decimal)),
            ("Rank", typeof(long)),
            ("LookupName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", "Munich", 350m, 1L, "Germany"],
            ["FR", "Paris", 300m, 1L, null],
            ["PL", "Warsaw", 500m, 1L, "Poland"]);
    }
}
