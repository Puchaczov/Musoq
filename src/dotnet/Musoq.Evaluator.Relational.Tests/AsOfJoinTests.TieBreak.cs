using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void TieBreakInjectsTheCurrentCandidateRow()
    {
        const string sql =
            "select b.Country from #A.Entities() a asof join #B.Entities() b on a.Population>=b.Population"
            + " tie break by b.GetCountry() desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity()],
            ["#B"] = [new BasicEntity { Country = "A" }, new BasicEntity { Country = "B" }],
        };
        using var query = CreateAndRunVirtualMachine(sql, sources);
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { "B" });
    }

    [TestMethod]
    public void ScalarTieBreakOrdersEqualCandidatesByItsResult()
    {
        const string sql =
            "select b.V from values { (K:1) } a asof join values { (K:1,V:'1'),(K:1,V:'2') } b"
            + " on a.K >= b.K tie break by ToInt32(b.V) desc";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { "2" });
    }

    [TestMethod]
    [DataRow("a.GetCountry() >= b.Country")]
    [DataRow("b.GetCountry() <= a.Country")]
    public void SourceMethodReceiverSuppliesItsSideOfTheAsOfKey(string condition)
    {
        var sql = $"select b.Country from #A.Entities() a asof join #B.Entities() b on {condition}";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Country = "B" }],
            ["#B"] = [new BasicEntity { Country = "A" }],
        };
        using var query = CreateAndRunVirtualMachine(sql, sources);
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { "A" });
    }

    [TestMethod]
    [DataRow("asc", "Z")]
    [DataRow("desc", "a")]
    public void StringTieBreak_UsesOrdinalOrdering(string direction, string expected)
    {
        var query = $$"""
            select b.Label from values { (K: 1) } a
            asof join values { (K: 1, Label: 'Z'), (K: 1, Label: 'a') } b on a.K >= b.K
            tie break by b.Label {{direction}}
            """;

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { expected });
    }

    [TestMethod]
    public void WhenAsOfJoinTieBreakAscending_ShouldPickLowestTieKey()
    {
        const string query = (
            "\n"
            + "select a.Name, b.Name\n"
            + "from #A.entities() a\n"
            + "asof join #B.entities() b on a.Population >= b.Population\n"
            + "tie break by b.Name asc"
        );

        var table = CreateAndRunVirtualMachine(query, CreateTieBreakSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1", "B-Alpha"]);
    }

    [TestMethod]
    public void WhenAsOfJoinTieBreakDescending_ShouldPickHighestTieKey()
    {
        const string query = (
            "\n"
            + "select a.Name, b.Name\n"
            + "from #A.entities() a\n"
            + "asof join #B.entities() b on a.Population >= b.Population\n"
            + "tie break by b.Name desc"
        );

        var table = CreateAndRunVirtualMachine(query, CreateTieBreakSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1", "B-Zulu"]);
    }

    [TestMethod]
    public void WhenAsOfJoinTieBreakNullsLast_ShouldUseExplicitNullOrdering()
    {
        const string query = (
            "\n"
            + "select a.Name, b.Name\n"
            + "from #A.entities() a\n"
            + "asof join #B.entities() b on a.Population >= b.Population\n"
            + "tie break by b.NullableValue asc nulls last"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            {
                "#B",
                [
                    new BasicEntity
                    {
                        Name = "B-Null",
                        Population = 90,
                        NullableValue = null,
                    },
                    new BasicEntity
                    {
                        Name = "B-Value",
                        Population = 90,
                        NullableValue = 5,
                    },
                ]
            },
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1", "B-Value"]);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinTieBreakDescending_ShouldTieBreakMatchesAndNullExtendMisses()
    {
        const string query = (
            "\n"
            + "select a.Name, b.Name\n"
            + "from #A.entities() a\n"
            + "asof left join #B.entities() b on a.Population >= b.Population\n"
            + "tie break by b.Name desc\n"
            + "order by a.Name"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity { Name = "A1", Population = 100 }, new BasicEntity { Name = "A2", Population = 10 }]
            },
            {
                "#B",
                [
                    new BasicEntity { Name = "B-Alpha", Population = 90 },
                    new BasicEntity { Name = "B-Zulu", Population = 90 },
                ]
            },
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A1", "B-Zulu"], new object?[] { "A2", null });
    }

    [TestMethod]
    public void WhenAsOfJoinWithoutTieBreak_ShouldKeepFirstDuplicateRightRow()
    {
        const string query = (
            "\n"
            + "select a.Name, b.Name\n"
            + "from #A.entities() a\n"
            + "asof join #B.entities() b on a.Population >= b.Population"
        );

        var table = CreateAndRunVirtualMachine(query, CreateTieBreakSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1", "B-Zulu"]);
    }

    [TestMethod]
    public void WhenAsOfJoinTieBreakReferencesLeftSide_ShouldThrow()
    {
        const string query = (
            "\n"
            + "select a.Name\n"
            + "from #A.entities() a\n"
            + "asof join #B.entities() b on a.Population >= b.Population\n"
            + "tie break by a.Name"
        );

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateTieBreakSources()));

        AssertErrorEnvelope(
            ex,
            DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides,
            DiagnosticPhase.Bind,
            "right-side"
        );
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateTieBreakSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            {
                "#B",
                [
                    new BasicEntity { Name = "B-Zulu", Population = 90 },
                    new BasicEntity { Name = "B-Alpha", Population = 90 },
                    new BasicEntity { Name = "B-Older", Population = 50 },
                ]
            },
        };
    }
}
