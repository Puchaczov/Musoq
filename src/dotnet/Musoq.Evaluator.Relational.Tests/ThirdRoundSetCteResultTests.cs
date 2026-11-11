using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class ThirdRoundSetCteResultTests : BasicEntityTestBase
{
    [TestMethod]
    public void UnionAndUnionAll_ShouldApplyDistinctAndAllDuplicateContracts()
    {
        const string unionQuery = (
            "\n"
            + "            select Name from #A.entities()\n"
            + "            union (Name)\n"
            + "            select Name from #B.entities()"
        );
        const string unionAllQuery = (
            "\n"
            + "            select Name from #A.entities()\n"
            + "            union all (Name)\n"
            + "            select Name from #B.entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("002")],
            ["#B"] = [new BasicEntity("002"), new BasicEntity("003")],
        };

        var union = CreateAndRunVirtualMachine(unionQuery, sources).Run(TestContext.CancellationToken);
        var unionAll = CreateAndRunVirtualMachine(unionAllQuery, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(union, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(union, ["001"], ["002"], ["003"]);
        TableMaterializationTestHelper.AssertColumns(unionAll, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(unionAll, ["001"], ["002"], ["002"], ["002"], ["003"]);
    }

    [TestMethod]
    public void ExceptAndIntersect_ShouldAssertCompleteSetMembership()
    {
        const string exceptQuery = (
            "\n"
            + "            select Name from #A.entities()\n"
            + "            except (Name)\n"
            + "            select Name from #B.entities()"
        );
        const string intersectQuery = (
            "\n"
            + "            select Name from #A.entities()\n"
            + "            intersect (Name)\n"
            + "            select Name from #B.entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("002")],
            ["#B"] = [new BasicEntity("002"), new BasicEntity("003")],
        };

        var except = CreateAndRunVirtualMachine(exceptQuery, sources).Run(TestContext.CancellationToken);
        var intersect = CreateAndRunVirtualMachine(intersectQuery, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(except, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(except, ["001"]);
        TableMaterializationTestHelper.AssertColumns(intersect, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(intersect, ["002"], ["002"]);
    }

    [TestMethod]
    public void CteFanoutWithIndependentConsumers_ShouldNotShareFiltersOrOrdering()
    {
        const string query = (
            "\n"
            + "            with source as (\n"
            + "                select Country, City, Population from #A.entities()\n"
            + "            )\n"
            + "            select leftSide.Country, leftSide.City, rightSide.City,\n"
            + "                   leftSide.Population, rightSide.Population\n"
            + "            from source leftSide\n"
            + "            left outer join source rightSide\n"
            + "                on leftSide.Country = rightSide.Country\n"
            + "                and rightSide.Population > leftSide.Population\n"
            + "            order by leftSide.Country, leftSide.Population, rightSide.Population"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("Berlin", "DE", 100),
                new BasicEntity("Munich", "DE", 200),
                new BasicEntity("Paris", "FR", 300),
            ],
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)),
            ("City", typeof(string)),
            ("Population", typeof(decimal)),
            ("Population", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", "Berlin", "Munich", 100m, 200m],
            ["DE", "Munich", null, 200m, null],
            ["FR", "Paris", null, 300m, null]
        );
    }

    [TestMethod]
    public void DistinctAfterJoin_ShouldRemoveOnlyCompleteDuplicateRows()
    {
        const string query = (
            "\n"
            + "            select distinct a.Country, b.Name\n"
            + "            from #A.entities() a\n"
            + "            inner join #B.entities() b on a.Country = b.Country\n"
            + "            order by a.Country, b.Name"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("Berlin", "DE", 100),
                new BasicEntity("Munich", "DE", 200),
                new BasicEntity("Paris", "FR", 300),
            ],
            ["#B"] =
            [
                new BasicEntity
                {
                    Name = "Deploy",
                    Country = "DE",
                    Population = 1,
                },
                new BasicEntity
                {
                    Name = "Deploy",
                    Country = "DE",
                    Population = 2,
                },
                new BasicEntity
                {
                    Name = "Release",
                    Country = "FR",
                    Population = 3,
                },
            ],
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["DE", "Deploy"], ["FR", "Release"]);
    }
}
