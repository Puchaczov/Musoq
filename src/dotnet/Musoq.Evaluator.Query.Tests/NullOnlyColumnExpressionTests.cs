using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class NullOnlyColumnExpressionTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("case when v.K=1 then true and false else true end")]
    [DataRow("case when v.K=2 then true else false or false end")]
    public void CaseBranches_ShouldAcceptBooleanConnectives(string expression)
    {
        var sql = $$"""select {{expression}} as M from values {(K:1),(K:1),(K:2)} v""";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [false], [false], [true]);
    }

    [TestMethod]
    [DataRow("null", "null", null)]
    [DataRow("v.N", "null", null)]
    [DataRow("v.N", "2", 2)]
    [DataRow("(select b.X from values {(X:null)} b)", "null", null)]
    public void CaseWithNullOnlyResults_ShouldKeepEverySelectedNull(string first, string otherwise, object? last)
    {
        var sql = $$"""
            select case when v.K=1 then {{first}} else {{otherwise}} end as M
            from values {(K:1,N:null),(K:1,N:null),(K:2,N:null)} v
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { null },
            new object?[] { null },
            [last]
        );
    }

    [TestMethod]
    [DataRow("select v.* from values {(I:1,N:null),(I:1,N:null)} v")]
    [DataRow("with v as (select w.I,null as N from values {(I:1),(I:1)} w) select * from v")]
    public void StarProjection_ShouldKeepNullOnlyScalarColumns(string sql)
    {
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, null], [1, null]);
    }

    [TestMethod]
    [DataRow("v.N + 1", null)]
    [DataRow("v.N ?? 1", 1)]
    [DataRow("v.N in (1,2)", null)]
    [DataRow("1 in (v.N,2)", null)]
    [DataRow("v.N contains (1,null)", true)]
    [DataRow("v.N is distinct from 1", true)]
    [DataRow("v.N and false", false)]
    [DataRow("v.N or true", true)]
    [DataRow("not v.N", null)]
    public void NullOnlyValuesColumn_ShouldUseItsExpressionContext(string expression, object? expected)
    {
        var sql = $$"""select {{expression}} as M from values { (N:null),(N:null) } v""";
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected], [expected]);
    }

    [TestMethod]
    [DataRow("v.N = 1", null)]
    [DataRow("1 is not distinct from v.N", false)]
    [DataRow("not v.N", null)]
    [DataRow("v.N and false", false)]
    [DataRow("v.N or true", true)]
    public void ProjectedNullColumn_ShouldUseItsExpressionContext(string expression, object? expected)
    {
        var sql = $$"""
            with q as (select null as N from values { (K:1),(K:1) } r)
            select {{expression}} as M from q v
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected], [expected]);
    }
}
