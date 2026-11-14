using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class QueryFeatureInteractionTests : BasicEntityTestBase
{
    [TestMethod]
    public void FullJoinBooleanOnParameter_ShouldRebindWithoutChangingPreviousResults()
    {
        const string sql = """
            param(enabled: bool)
            select l.Id as LeftId, r.Id as RightId
            from #A.Entities() l
            full join #B.Entities() r on l.Id = r.Id and $enabled
            """;
        using var query = CreateAndRunVirtualMachine(sql, FullJoinSources());
        NativeParameterFixture.Set(query.TypedParameters, query.ParameterDefinitions, "enabled", true);
        using var first = TableMaterializationTestHelper.Materialize(query.Run(TestContext.CancellationToken));

        TableMaterializationTestHelper.AssertColumns(first, ("LeftId", typeof(int?)), ("RightId", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(first, [1, null], [2, 2], [null, 3]);

        NativeParameterFixture.Set(query.TypedParameters, query.ParameterDefinitions, "enabled", false);
        using var second = TableMaterializationTestHelper.Materialize(query.Run(TestContext.CancellationToken));

        TableMaterializationTestHelper.AssertColumns(second, ("LeftId", typeof(int?)), ("RightId", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(second, [1, null], [2, null], [null, 2], [null, 3]);
        TableMaterializationTestHelper.AssertRowsUnordered(first, [1, null], [2, 2], [null, 3]);

        NativeParameterFixture.Set(query.TypedParameters, query.ParameterDefinitions, "enabled", true);
        using var rebound = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(rebound, ("LeftId", typeof(int?)), ("RightId", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(rebound, [1, null], [2, 2], [null, 3]);
    }

    [TestMethod]
    [DataRow(null, 1)]
    [DataRow(null, 2)]
    [DataRow(null, 3)]
    [DataRow(null, 99)]
    public void FullJoinNullableFilter_ShouldSnapshotEachRuntimeBinding(int? initialKey, int? nextKey)
    {
        const string sql = """
            param(key: int? = null)
            select l.Id as LeftId, r.Id as RightId
            from #A.Entities() l
            full join #B.Entities() r on l.Id = r.Id
            where $key is null or l.Id = $key or r.Id = $key
            """;
        using var query = CreateAndRunVirtualMachine(sql, FullJoinSources());
        var initialExpected = ExpectedFullJoinRows(initialKey);
        var nextExpected = ExpectedFullJoinRows(nextKey);
        NativeParameterFixture.Set(query.TypedParameters, query.ParameterDefinitions, "key", initialKey);
        using var first = TableMaterializationTestHelper.Materialize(query.Run(TestContext.CancellationToken));

        TableMaterializationTestHelper.AssertColumns(first, ("LeftId", typeof(int?)), ("RightId", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(first, initialExpected);

        NativeParameterFixture.Set(query.TypedParameters, query.ParameterDefinitions, "key", nextKey);
        using var second = TableMaterializationTestHelper.Materialize(query.Run(TestContext.CancellationToken));

        TableMaterializationTestHelper.AssertColumns(second, ("LeftId", typeof(int?)), ("RightId", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(second, nextExpected);
        TableMaterializationTestHelper.AssertRowsUnordered(first, initialExpected);

        NativeParameterFixture.Set(query.TypedParameters, query.ParameterDefinitions, "key", initialKey);
        using var rebound = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(rebound, ("LeftId", typeof(int?)), ("RightId", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(rebound, initialExpected);
    }

    [TestMethod]
    public void RecursiveInt32Casts_ShouldPreserveMatchingAnchorAndMemberTypes()
    {
        const string sql = """
            with recursive counter (Value) as (
                select seed.Value::Int32 from values { (Value: 1) } seed
                union all
                select (c.Value + 1)::Int32 from counter c where c.Value < 3
            )
            select Value from counter order by Value
            """;
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, [1], [2], [3]);
    }

    [TestMethod]
    public void RecursiveInt32Casts_WithNullAnchor_ShouldKeepNullAndTerminateItsBranch()
    {
        const string sql = """
            with recursive counter (Value) as (
                select e.NullableValue::Int32 from #A.Entities() e
                union all
                select (c.Value + 1)::Int32 from counter c where c.Value < 3
            )
            select Value from counter order by Value nulls first
            """;
        var source = CreateSingleSource(
            new BasicEntity { NullableValue = null },
            new BasicEntity { NullableValue = 1 }
        );
        using var query = CreateAndRunVirtualMachine(sql, source);
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, [null], [1], [2], [3]);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> FullJoinSources() =>
        new()
        {
            ["#A"] = [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            ["#B"] = [new BasicEntity { Id = 2 }, new BasicEntity { Id = 3 }],
        };

    private static object?[][] ExpectedFullJoinRows(int? key)
    {
        object?[][] rows =
        [
            [1, null],
            [2, 2],
            [null, 3],
        ];
        if (!key.HasValue)
            return rows;
        return rows.Where(row => (int?)row[0] == key || (int?)row[1] == key).ToArray();
    }
}
