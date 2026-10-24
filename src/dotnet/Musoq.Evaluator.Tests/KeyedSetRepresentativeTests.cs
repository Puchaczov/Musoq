using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class KeyedSetRepresentativeTests : BasicEntityTestBase
{
    [TestMethod]
    public void ExceptWithoutExplicitKeys_ShouldPreserveDuplicateLeftRows()
    {
        const string query = "select Name from values { (Name: 'keep'), (Name: 'keep'), (Name: 'remove') } l " +
                             "except select Name from values { (Name: 'remove'), (Name: 'remove') } r";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["keep"], ["keep"]);
    }

    [TestMethod]
    [DataRow("NullableValue")]
    [DataRow("ToDouble(City)")]
    public void Union_ShouldRetainTheFirstLeftRepresentativeIncludingNullKeys(string key)
    {
        var query = OrderedInputs + $"""
            , combined as (
                select {key} as Key, Name as Label from leftRows
                union (Key)
                select {key} as Key, Name as Label from rightRows
            ) select Label from combined
            """;
        using var compiled = CreateAndRunVirtualMachine(query, CreateSources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table,
            ["left-first"], ["left-null"], ["left-unmatched"], ["right-first"]);
    }

    [TestMethod]
    [DataRow("NullableValue", "intersect", new[] { "left-first", "left-first", "left-later", "left-null", "left-null" })]
    [DataRow("ToDouble(City)", "intersect", new[] { "left-first", "left-first", "left-later", "left-null", "left-null" })]
    [DataRow("NullableValue", "except", new[] { "left-unmatched", "left-unmatched" })]
    [DataRow("ToDouble(City)", "except", new[] { "left-unmatched", "left-unmatched" })]
    public void MembershipOperations_ShouldPreserveEveryAdmittedLeftRow(string key, string operation, string[] expected)
    {
        var query = OrderedInputs + $"""
            , combined as (
                select {key} as Key, Name as Label from leftRows
                {operation} (Key)
                select {key} as Key, Name as Label from rightRows
            ) select Label from combined
            """;
        using var compiled = CreateAndRunVirtualMachine(query, CreateSources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, expected.Select(static label => new object?[] { label }).ToArray());
    }

    [TestMethod]
    [DataRow("NullableValue")]
    [DataRow("ToDouble(City)")]
    public void ChainedOperations_ShouldRetainUnionRepresentativesThroughMembershipFiltering(string key)
    {
        var query = OrderedInputs + $"""
            , combined as (
                select {key} as Key, Name as Label from leftRows
                union (Key) select {key} as Key, Name as Label from rightRows
                except (Key) select {key} as Key, Name as Label from rightRows where NullableValue = 2
                intersect (Key) select {key} as Key, Name as Label from rightRows
            ) select Label from combined
            """;
        using var compiled = CreateAndRunVirtualMachine(query, CreateSources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["left-first"], ["left-null"]);
    }

    private const string OrderedInputs = """
        with leftRows as (select Id, NullableValue, City, Name from #A.entities() order by Id),
        rightRows as (select Id, NullableValue, City, Name from #B.entities() order by Id)
        """;

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources() => new()
    {
        ["#A"] =
        [
            Row(1, 1, "left-first"), Row(2, 1, "left-first"), Row(3, 1, "left-later"),
            Row(4, null, "left-null"), Row(5, null, "left-null"),
            Row(6, 3, "left-unmatched"), Row(7, 3, "left-unmatched")
        ],
        ["#B"] =
        [
            Row(1, 1, "right-conflict"), Row(2, null, "right-null"),
            Row(3, 2, "right-first"), Row(4, 2, "right-later"), Row(5, 1, "right-duplicate")
        ]
    };

    private static BasicEntity Row(int position, int? key, string label) => new(label)
    {
        Id = position,
        NullableValue = key,
        City = key?.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };
}
