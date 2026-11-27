using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class ScalarSubqueryProjectionTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("(select b.X from values {(X:4)} b)+1", 5)]
    [DataRow("(select b.X from values {(X:true)} b) and true", true)]
    public void ScalarOperand_ShouldUseBoundProjectionTypeInBinaryExpressions(string expression, object expected)
    {
        var sql = $$"""select {{expression}} as M from values {(I:1),(I:1)} o""";
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected], [expected]);
    }

    [TestMethod]
    [DataRow("o.K in (9,(select b.N from values {(N:null),(N:null)} b))")]
    [DataRow("(select b.N from values {(N:null),(N:null)} b) contains (1,2)")]
    [DataRow("(select b.N from values {(N:null),(N:null)} b) ?? 7")]
    [DataRow("(select b.N from values {(N:null),(N:null)} b)+1")]
    [DataRow("(select b.N from values {(N:null),(N:null)} b) and true")]
    [DataRow("(select b.N from values {(N:null),(N:null)} b)+null")]
    [DataRow("null+(select b.N from values {(N:null),(N:null)} b)")]
    [DataRow("(select b.N from values {(N:null),(N:null)} b) ?? null")]
    public void DemandedNullOnlyScalarOperand_ShouldEnforceCardinality(string expression)
    {
        var sql = $$"""select case when o.K=1 then {{expression}} else null end as M from values {(K:1)} o""";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        var failure = Assert.Throws<Musoq.Evaluator.Exceptions.QueryExecutionException>(() =>
            _ = query.Run(TestContext.CancellationToken).Count
        );
        MusoqExceptionAssertions.AssertRuntimeError(
            failure,
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ7016_ScalarSubqueryCardinalityViolation
        );
    }

    [TestMethod]
    public void ParallelScalarJoinSubqueries_ShouldOwnDisjointIntermediateTables()
    {
        const string sql = """
            select (select r.X from values {(K:1)} l inner join values {(K:1,X:7)} r on l.K=r.K) as A,
                   (select r.X from values {(K:1)} l inner join values {(K:1,X:7ul)} r on l.K=r.K) as B
            from values {(I:1),(I:1)} o
            """;
        var data = new Dictionary<string, IEnumerable<BasicEntity>>();
        var plan = InstanceCreator.CompileForInspection(
            sql,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(data),
            LoggerResolver,
            TestCompilationOptions
        );
        var parallel = plan.ExecutionPlan!.Body.Nodes.OfType<ExecutionParallelBlock>().Single();
        Assert.AreEqual(2, parallel.Tasks.Count);
        var taskSlots = parallel
            .Tasks.Select(task =>
                task.Body.Nodes.OfType<ExecutionStoreTable>().Select(store => store.TableIndex).ToHashSet()
            )
            .ToArray();
        Assert.IsFalse(taskSlots[0].Overlaps(taskSlots[1]), "Parallel CTE tasks must own distinct scratch tables.");
        using var compiled = CreateAndRunVirtualMachine(sql, data);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [7, 7UL], [7, 7UL]);
    }

    [TestMethod]
    public void SiblingScalarJoinSources_ShouldKeepDifferentNumericWidths()
    {
        const string query = """
            select (select r.X from values {(K:1)} l inner join values {(K:1,X:7)} r on l.K=r.K) as A,
                   (select r.X from values {(K:1)} l inner join values {(K:1,X:7ul)} r on l.K=r.K) as B
            from values {(I:1),(I:1)} o
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("A", typeof(int?)), ("B", typeof(ulong?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [7, 7UL], [7, 7UL]);
    }

    [TestMethod]
    [DataRow("class")]
    [DataRow("namespace")]
    public void ScalarResultAlias_ShouldAllowCSharpKeywords(string alias)
    {
        var sql = $$"""
            select case when o.K=1 then (select b.X as {{alias}} from values {(X:7)} b) else null end as M
            from values {(K:1),(K:1),(K:2)} o
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [7], [7], new object?[] { null });
    }

    [TestMethod]
    [DataRow("case when o.K=1 then (select b.N from values {(N:null),(N:null)} b) else 7 end")]
    [DataRow("case when o.K=1 then ((select b.N from values {(N:null),(N:null)} b))::int else 7 end")]
    public void SelectedNullOnlySubquery_ShouldEnforceCardinalityBeforeConvertingItsResult(string expression)
    {
        var sql = $$"""select {{expression}} as V from values {(K:1)} o""";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        var failure = Assert.Throws<Musoq.Evaluator.Exceptions.QueryExecutionException>(() =>
            _ = query.Run(TestContext.CancellationToken).Count
        );
        MusoqExceptionAssertions.AssertRuntimeError(
            failure,
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ7016_ScalarSubqueryCardinalityViolation
        );
    }

    [TestMethod]
    public void ScalarSubquery_ShouldPreserveArrayResultAndNullForMissingRows()
    {
        const string query = """
            select (select Split(b.X, ',') from values {(K:1,X:'a,b')} b where b.K=o.K) as C
            from values {(K:1),(K:1),(K:2)} o order by o.K
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("C", typeof(string[])));
        Assert.AreEqual(3, table.Count);
        CollectionAssert.AreEqual(new[] { "a", "b" }, table[0].GetValue<string[]>(0));
        CollectionAssert.AreEqual(new[] { "a", "b" }, table[1].GetValue<string[]>(0));
        Assert.IsNull(table[2].GetValue<string[]>(0));
    }

    [TestMethod]
    [DataRow("null")]
    [DataRow("b.N")]
    public void NullOnlyScalarSubquery_ShouldPreserveCardinality(string projection)
    {
        var query = $$"""
            select (select {{projection}} from values {(K:1,N:null)} b where b.K=o.K) as C,
                   (select {{projection}} from values {(K:1,N:null)} b) as U
            from values {(K:1),(K:1),(K:2)} o
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [null, null], [null, null], [null, null]);
    }

    [TestMethod]
    [DataRow("float", typeof(float?))]
    [DataRow("ulong", typeof(ulong?))]
    [DataRow("byte", typeof(byte?))]
    public void ScalarSubquery_ShouldPreserveItsProjectedTypeAndNullForMissingRows(
        string sqlType,
        System.Type expectedType
    )
    {
        var query = $$"""
            select (select b.X::{{sqlType}} from values {(K:1,X:7)} b where b.K=o.K) as C,
                   (select b.X::{{sqlType}} from values {(K:1,X:7)} b) as U
            from values {(K:1),(K:1),(K:2)} o
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("C", expectedType), ("U", expectedType));
        var seven = System.Convert.ChangeType(7, System.Nullable.GetUnderlyingType(expectedType)!);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [seven, seven], [seven, seven], [null, seven]);
    }

    [TestMethod]
    public void SiblingScalarValues_ShouldPreserveEachSourceLiteralAndNumericType()
    {
        const string query = """
            select (select b.X from values {(X:7)} b) as A,
                   (select b.X from values {(X:9)} b) as B,
                   (select b.X from values {(X:7ul)} b) as U
            from values {(I:1),(I:1)} o
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("A", typeof(int?)),
            ("B", typeof(int?)),
            ("U", typeof(ulong?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, [7, 9, 7UL], [7, 9, 7UL]);
    }

    [TestMethod]
    public void InferredGroupingKeys_ShouldKeepDifferentScalarSubqueryLiterals()
    {
        const string query = """
            select (select b.X from values {(X:'A')} b) as A,(select b.X from values {(X:'a')} b) as B,Count(1) as C
            from values {(I:1),(I:1)} o group by all
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A", "a", 2L]);
    }

    [TestMethod]
    public void QualifiedOuterKey_ShouldRemainVisibleBesideAnInnerScalarWithTheSameNameAsItsAlias()
    {
        const string query = """
            select case when v.K is null then 0 else (
                select b.V from values {(K:1,V:7),(K:2,V:8)} b where b.K=v.K
            ) end as M from values {(K:1),(K:1),(K:2),(K:null)} v
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [7], [7], [8], [0]);
    }

    [TestMethod]
    public void GroupedCteFollowingLeftJoin_ShouldOwnTheJoinRowsInItsBuildMethod()
    {
        const string query = """
            with r as (select b.I,Sum(b.X) as X from values {(I:1,X:4)} b group by b.I),
            q as (select Sum(r.X) as M from values {(I:1),(I:1),(I:2)} o left outer join r on r.I=o.I group by o.I)
            select q.M from q
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [8], new object?[] { null });
    }

    [TestMethod]
    [DataRow("(select b.X from values {(K:1,X:7),(K:2,X:8)} b where b.K=o.K)", 7, 8, null)]
    [DataRow("(select b.X from values {(K:1,X:7),(K:2,X:8)} b where b.K=1)", 7, 7, 7)]
    [DataRow("exists (select b.K from values {(K:1),(K:2)} b where b.K=o.K)", true, true, false)]
    [DataRow("o.K in (select b.K from values {(K:1),(K:2)} b)", true, true, null)]
    public void GroupedProjection_ShouldEvaluateSubqueriesForEachCompletedGroup(
        string expression,
        object first,
        object second,
        object? nullKey
    )
    {
        var query = $"select o.K,{expression} as M from values {{(K:1),(K:1),(K:2),(K:null)}} o group by o.K";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, first], [2, second], [null, nullKey]);
    }

    [TestMethod]
    [DataRow("1=any (select b.X from values {(X:1ul)} b)", true)]
    [DataRow("1<>all (select b.X from values {(X:1ul)} b)", false)]
    [DataRow("0.10000000000000000000000001d=any (select b.X::double from values {(X:0.1d)} b)", false)]
    [DataRow("'a'::char in (select b.X from values {(X:'a')} b)", true)]
    public void PredicateSubquery_ShouldUseScalarOperandCoercion(string expression, bool expected)
    {
        var query = $"select {expression} as M from values {{(N:1)}} v";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected]);
    }

    [TestMethod]
    public void GroupedCorrelatedSum_InCte_ShouldAccumulateEveryOuterRow()
    {
        const string query = """
            with q as (
                select Sum((select b.X from values {(I:1,X:4)} b where b.I=v.I)) as M
                from values {(I:1),(I:1)} v group by v.I
            ) select q.M from q
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [8]);
    }

    [TestMethod]
    [DataRow("in", true)]
    [DataRow("not in", false)]
    public void MatchingMember_ShouldSkipLaterScalarSubquery(string operation, bool expected)
    {
        var query = $$"""
            select v.K {{operation}} (v.K,(select b.K from values {(K:7),(K:8)} b)) as M
            from values {(K:1),(K:1),(K:2)} v
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected], [expected], [expected]);
    }

    [TestMethod]
    public void SiblingScalarSubqueries_ShouldHaveIndependentInlineRows()
    {
        const string query = """
            select case when v.K=1 then (select b.K from values {(K:7)} b) else 0 end as A,
                   case when v.K=2 then (select b.K from values {(K:9)} b) else 0 end as B
            from values {(K:1),(K:1),(K:2)} v
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [7, 0], [7, 0], [0, 9]);
    }

    [TestMethod]
    public void DistinctGuardedScalar_ShouldDeduplicateTheProjectedValues()
    {
        const string query = """
            select distinct case when v.K=1 then (select b.K from values {(K:7)} b) else v.K end as A
            from values {(K:1),(K:1),(K:2)} v
            """;
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [7], [2]);
    }
}
