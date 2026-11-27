using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    [DataRow("left join", "")]
    [DataRow("asof left join", " and a.K >= b.K")]
    public void CharacterAndStringPartitionKeys_ShouldUseTypedEquality(string join, string nearest)
    {
        var query = $$"""
            select a.P,b.K from values { (P:'a',K:2),(P:'a',K:2),(P:'x',K:2) } a
            {{join}} values { (P:'a',K:1) } b on a.P::char = b.P{{nearest}}
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["a", 1], ["a", 1], ["x", null]);
    }

    [TestMethod]
    public void StringMatchKey_UsesOrdinalOrdering()
    {
        const string query = "select b.K from values { (K: 'Z') } a asof left join values { (K: 'a') } b on a.K >= b.K";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null });
    }

    [TestMethod]
    public void MixedNumericMatchKeys_UseCommonComparisonType()
    {
        const string query = "select b.K from values { (K: 1) } a asof join values { (K: 1l) } b on a.K >= b.K";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { 1L });
    }

    [TestMethod]
    public void MixedNumericPartitionKeys_UseCommonComparisonType()
    {
        const string query = """
            select b.K from values { (P: 1, K: 1) } a
            asof join values { (P: 1l, K: 1) } b on a.P = b.P and a.K >= b.K
            """;

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { 1 });
    }

    [TestMethod]
    public void SignedAndUnsignedAsOfKeys_PreserveNegativeProbe()
    {
        const string query = """
            select a.K, b.K from values { (K: -1), (K: 2) } a
            asof left join values { (K: 1ui) } b on a.K >= b.K
            """;

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [-1, null], [2, 1u]);
    }

    [TestMethod]
    public void LeftJoin_NonNullablePartitionKey_BindsCandidateBeforeNullExtension()
    {
        const string query = """
            select b.K from values { (P: 1, K: 1) } a
            asof left join values { (P: 1, K: 1) } b on a.P = b.P and a.K >= b.K
            """;

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { 1 });
    }

    [TestMethod]
    public void WhenAsOfJoinWithDateTimeColumn_ShouldMatchByTime()
    {
        var query = (
            "\n"
            + "select\n"
            + "    a.Name,\n"
            + "    b.Name\n"
            + "from #A.entities() a\n"
            + "asof join #B.entities() b on a.Time >= b.Time"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "Error1", Time = new DateTime(2025, 1, 15, 14, 30, 0) },
                    new BasicEntity { Name = "Error2", Time = new DateTime(2025, 1, 15, 10, 0, 0) },
                ]
            },
            {
                "#B",
                [
                    new BasicEntity { Name = "Commit1", Time = new DateTime(2025, 1, 15, 14, 0, 0) },
                    new BasicEntity { Name = "Commit2", Time = new DateTime(2025, 1, 15, 9, 0, 0) },
                    new BasicEntity { Name = "Commit3", Time = new DateTime(2025, 1, 14, 12, 0, 0) },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Error1", "Commit1"], ["Error2", "Commit2"]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithStringColumn_ShouldMatchLexicographically()
    {
        var query = (
            "\n"
            + "select\n"
            + "    a.Name,\n"
            + "    b.Name\n"
            + "from #A.entities() a\n"
            + "asof join #B.entities() b on a.City >= b.City"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", City = "M" }] },
            {
                "#B",
                [
                    new BasicEntity { Name = "B1", City = "A" },
                    new BasicEntity { Name = "B2", City = "K" },
                    new BasicEntity { Name = "B3", City = "Z" },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1", "B2"]);
    }
}
