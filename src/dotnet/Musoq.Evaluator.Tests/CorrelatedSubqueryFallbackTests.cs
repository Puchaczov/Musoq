using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CorrelatedSubqueryFallbackTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("in", true, null, false)]
    [DataRow("not in", false, null, true)]
    public void MembershipWithRangeCorrelation_ShouldPreserveUnknownAndEmptyResults(string operation, bool? first, bool? second, bool? last)
    {
        var query = $"select o.Id, o.NullableValue {operation} (select i.NullableValue from #B.entities() i where i.Id > o.Id) as Matched " +
                    "from #A.entities() o order by o.Id";
        var sources = Sources();
        sources["#A"] = [new BasicEntity { Id = 1, NullableValue = 2 }, new BasicEntity { Id = 2 }, new BasicEntity { Id = 4, NullableValue = 3 }];
        sources["#B"] = [new BasicEntity { Id = 2, NullableValue = 2 }, new BasicEntity { Id = 3 }];
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Matched", typeof(bool?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, first], [2, second], [4, last]);
    }

    [TestMethod]
    public void ScalarFallbackWithTwoRows_ShouldUseTheExistingCardinalityDiagnostic()
    {
        const string query = "select (select i.Id + o.Id from #B.entities() i where i.Id > o.Id) as Value from #A.entities() o";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());

        var exception = Assert.Throws<QueryExecutionException>(() => _ = compiled.Run(TestContext.CancellationToken).Count);

        AssertRuntimeError(exception, DiagnosticCode.MQ7016_ScalarSubqueryCardinalityViolation);
    }

    [TestMethod]
    public void NestedFallback_ShouldCaptureBothImmediateAndDistantOuterBindings()
    {
        const string query = "select o.Id, (select (select j.Id + i.Id + o.Id from #B.entities() j " +
                             "where j.Id > i.Id order by j.Id take 1) from #B.entities() i " +
                             "where i.Id > o.Id order by i.Id take 1) as Value from #A.entities() o order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 6], [2, null], [4, null]);
    }

    [TestMethod]
    public void FallbackDerivedInput_ShouldRetainItsLexicalScope()
    {
        const string query = "select o.Id, (select c.Id + o.Id from (select i.Id from #B.entities() i) c where c.Id > o.Id order by c.Id take 1) as Value " +
                             "from #A.entities() o order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 3], [2, 5], [4, null]);
    }

    [TestMethod]
    public void Fallback_ShouldReadAnOuterCteWithoutRebindingItsColumns()
    {
        const string query = "with candidates as (select i.Id from #B.entities() i) " +
                             "select o.Id, (select c.Id + o.Id from candidates c where c.Id > o.Id order by c.Id take 1) as Value " +
                             "from #A.entities() o order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 3], [2, 5], [4, null]);
    }

    [TestMethod]
    public void FallbackDistinctAndSlicing_ShouldShapeEachOuterBinding()
    {
        const string query = "select o.Id, (select distinct i.Id + o.Id as Value from #B.entities() i " +
                             "where i.Id > o.Id order by Value skip 1 take 1) as Value from #A.entities() o order by o.Id";
        var sources = Sources();
        sources["#B"] = [new BasicEntity { Id = 2 }, new BasicEntity { Id = 2 }, new BasicEntity { Id = 3 }];
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 4], [2, null], [4, null]);
    }

    [TestMethod]
    public void ScalarWithOuterProjectionAndSlicing_ShouldExecuteForTheCurrentBinding()
    {
        const string query = "select o.Id, (select i.Id + o.Id from #B.entities() i where i.Id > o.Id order by i.Id take 1) as Value " +
                             "from #A.entities() o order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 3], [2, 5], [4, null]);
    }

    [TestMethod]
    public void ExistsWithDisjunctiveCorrelation_ShouldRemainAValueExpression()
    {
        const string query = "select o.Id, exists(select i.Id from #B.entities() i where i.Id = o.Id or i.Id > o.Id + 1) as Matched " +
                             "from #A.entities() o order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, true], [2, true], [4, false]);
    }

    [TestMethod]
    public void GuardedScalar_ShouldSkipTheFailingInnerProjection()
    {
        const string query = "select o.Id, case when o.Id = 1 then " +
                             "(select i.Name::int from #B.entities() i where i.Id = o.Id + 1 order by i.Id take 1) " +
                             "else 0 end as Value from #A.entities() o order by o.Id";
        var sources = Sources();
        sources["#B"] = [new BasicEntity("7") { Id = 2 }, new BasicEntity("bad") { Id = 3 }];
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 7], [2, 0], [4, 0]);
    }

    [TestMethod]
    public void FallbackInsideCte_ShouldPreserveGuardsAndUseStaticNativeHelpers()
    {
        const string query = "with valuesByRow as (select o.Id, case when o.Id = 1 then " +
            "(select i.Name::int from #B.entities() i where i.Id = o.Id + 1 order by i.Id take 1) else 0 end as Value " +
            "from #A.entities() o) select c.Id, c.Value from valuesByRow c order by c.Id";
        var sources = Sources();
        sources["#B"] = [new BasicEntity("7") { Id = 2 }, new BasicEntity("bad") { Id = 3 }];
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 7], [2, 0], [4, 0]);
    }

    [TestMethod]
    public void SiblingDerivedFallbacks_ShouldKeepIndependentLocalCteStorage()
    {
        const string query = "select o.Id, " +
            "(select c.Id + o.Id from (select i.Id from #B.entities() i) c where c.Id > o.Id order by c.Id take 1) as FirstValue, " +
            "(select d.Id + o.Id from (select j.Id from #B.entities() j) d where d.Id > o.Id order by d.Id desc take 1) as LastValue " +
            "from #A.entities() o order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 3, 4], [2, 5, 5], [4, null, null]);
    }

    [TestMethod]
    public void Fallback_ShouldReadTheEnclosingScriptVariable()
    {
        const string query = "let offset: int = 10; select o.Id, (select i.Id + o.Id + $offset from #B.entities() i " +
            "where i.Id > o.Id order by i.Id take 1) as Value from #A.entities() o order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 13], [2, 15], [4, null]);
    }

    [TestMethod]
    public void UnqualifiedOuterColumn_ShouldBindToTheNearestVisibleQuery()
    {
        const string query = "select o.OuterId, (select i.Id + OuterId from #B.entities() i where i.Id > OuterId " +
            "order by i.Id take 1) as Value from (select a.Id as OuterId from #A.entities() a) o order by o.OuterId";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 3], [2, 5], [4, null]);
    }

    [TestMethod]
    public void UnqualifiedLocalColumn_ShouldShadowAnOuterColumnWithTheSameName()
    {
        const string query = "select o.Id, (select Id from #B.entities() i where i.Id > o.Id " +
            "order by i.Id take 1) as Value from #A.entities() o order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 2], [2, 3], [4, null]);
    }

    [TestMethod]
    public void JoinConditionFallback_ShouldBindBothJoinInputs()
    {
        const string query = "select o.Id, b.Id from #A.entities() o inner join #B.entities() b " +
            "on b.Id = (select i.Id + o.Id - b.Id from #B.entities() i where i.Id >= b.Id order by i.Id take 1) " +
            "order by o.Id, b.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [2, 2]);
    }

    [TestMethod]
    public void NestedFallback_ShouldCarryAnOuterCteThroughTheIntermediateBinding()
    {
        const string query = "with candidates as (select b.Id from #B.entities() b) " +
            "select o.Id, (select (select c.Id + i.Id + o.Id from candidates c where c.Id > i.Id " +
            "order by c.Id take 1) from #B.entities() i where i.Id > o.Id order by i.Id take 1) as Value " +
            "from #A.entities() o order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 6], [2, null], [4, null]);
    }

    [TestMethod]
    public void ExistsFallback_ShouldBindButNotEvaluateAnUnusedProjection()
    {
        const string query = "select o.Id, exists(select i.Name::int from #B.entities() i " +
            "where i.Id > o.Id or i.Id = o.Id + 2) as Matched from #A.entities() o order by o.Id";
        var sources = Sources();
        sources["#B"] = [new BasicEntity("bad") { Id = 2 }, new BasicEntity("bad") { Id = 3 }];
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, true], [2, true], [4, false]);
    }

    [TestMethod]
    public void WindowArgumentFallback_ShouldKeepTheInnerAggregateOutsideTheWindowScope()
    {
        const string query = "select o.Id, FirstValue((select Count(i.Id) from #B.entities() i " +
            "where i.Id > o.Id or i.Id = o.Id + 100)) over (order by o.Id rows between current row and current row) as Value " +
            "from #A.entities() o order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 2L], [2, 1L], [4, 0L]);
    }

    [TestMethod]
    public void HavingFallback_ShouldUseTheCurrentGroupingKey()
    {
        const string query = "select o.Id, Count(o.Id) as Total from #A.entities() o group by o.Id " +
            "having exists(select i.Id from #B.entities() i where i.Id = o.Id + 1 or i.Id > o.Id + 1) order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 1L], [2, 1L]);
    }

    [TestMethod]
    public void QualifyFallback_ShouldRunAfterTheWindowAndPreserveItsGuard()
    {
        const string query = "select o.Id, RowNumber() over (order by o.Id) as RowId from #A.entities() o " +
            "qualify RowId = 1 or exists(select i.Id from #B.entities() i where i.Id > o.Id + 1 or i.Id = o.Id + 1) order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, Sources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 1L], [2, 2L]);
    }

    [TestMethod]
    [DataRow("(select Count(*) from #B.entities() i where i.Id = o.Id) > 0")]
    [DataRow("exists(select i.Id from #B.entities() i where i.Id = o.Id)")]
    [DataRow("o.Id in (select i.Id from #B.entities() i where i.Id = o.Id)")]
    [DataRow("o.Id > ANY (select i.Id from #B.entities() i where i.Id = o.Id)")]
    [DataRow("o.Id > SOME (select i.Id from #B.entities() i where i.Id = o.Id)")]
    [DataRow("o.Id > ALL (select i.Id from #B.entities() i where i.Id = o.Id)")]
    public void GuardedSubquery_ShouldNotEnumerateAnUndemandedSource(string expression)
    {
        var query = $"select o.Id, case when o.Id < 0 then {expression} else false end as Value from #A.entities() o order by o.Id";
        var sources = Sources();
        var innerRows = new UndemandedRows();
        sources["#B"] = innerRows;
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        innerRows.ThrowOnRead = true;
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, false], [2, false], [4, false]);
    }

    private sealed class UndemandedRows : IEnumerable<BasicEntity>
    {
        internal bool ThrowOnRead { get; set; }
        public IEnumerator<BasicEntity> GetEnumerator()
        {
            if (ThrowOnRead)
                throw new InvalidOperationException("Undemanded source was enumerated.");
            return Enumerable.Empty<BasicEntity>().GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> Sources() => new()
    {
        ["#A"] = [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }, new BasicEntity { Id = 4 }],
        ["#B"] = [new BasicEntity { Id = 2 }, new BasicEntity { Id = 3 }]
    };
}
