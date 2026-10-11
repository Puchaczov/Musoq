using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class FinalOutputOwnershipTests : GenericEntityTestBase
{
    private const string SerialSource = "from #schema.first() a where a.Id > 0";
    private const string BufferedJoinSource =
        "from #schema.first() a inner join #schema.first() b on a.Id = b.Id where a.Id > 0";

    [TestMethod]
    [DataRow(SerialSource)]
    [DataRow(BufferedJoinSource)]
    public void FinalProjection_PreservesVolatileEvaluationOrderAndNullableValuesAcrossRuns(string source)
    {
        var entity = new OutputEntity { Id = 1 };
        using var query = CreateAndRunVirtualMachine<OutputEntity, OutputLibrary>(
            "param(offset: int = 10) select a.Id, a.Next() + $offset as First, a.Next() as Second, " +
            $"a.Optional {source}", [entity]);

        using var first = query.Run(TestContext.CancellationToken);
        Assert.AreEqual(0, entity.Calls);
        TableMaterializationTestHelper.AssertRowsInOrder(first, [1, 11, 2, null]);
        Assert.AreEqual(2, entity.Calls);

        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(query.TypedParameters, query.ParameterDefinitions, "offset", 20);
        using var second = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(second, [1, 23, 4, null]);
        TableMaterializationTestHelper.AssertRowsInOrder(first, [1, 11, 2, null]);
        Assert.AreEqual(4, entity.Calls);
    }

    [TestMethod]
    public void TypedResultAccess_WhenColumnNamesMatchHelpersAndDiscard_ShouldKeepConcreteValues()
    {
        using var query = CreateAndRunVirtualMachine(
            "select Id as _, Id as TypedColumnRead, Id as TypedColumnVisit from #schema.first()",
            [new OutputEntity { Id = 7 }]);
        using var table = query.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);
        TableMaterializationTestHelper.AssertColumns(table, ("_", typeof(int)), ("TypedColumnRead", typeof(int)),
            ("TypedColumnVisit", typeof(int)));
        for (var index = 0; index < 3; index++)
        {
            Assert.AreEqual(7, table[0].GetValue<int>(index));
            table[0].AssignValue(index, 11);
            Assert.AreEqual(11, table[0].GetValue<int>(index));
        }
    }

    [TestMethod]
    public void TypedResultMutation_ShouldRejectDifferentTypesAndAvoidNumericBoxing()
    {
        using var query = CreateAndRunVirtualMachine(
            "select a.Id, a.Optional from #schema.first() a", [new OutputEntity { Id = 1, Optional = 7 }]);
        using var table = query.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);
        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Optional", typeof(int?)));
        var row = table[0];
        Assert.Throws<InvalidOperationException>(() => row.AssignValue(0, 99L));
        Assert.AreEqual(1, row.GetValue<int>(0));
        row.AssignValue<int?>(1, null);
        Assert.IsNull(row.GetValue<int?>(1));
        for (var index = 0; index < 10_000; index++)
            row.AssignValue(0, index);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
            row.AssignValue(0, index);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(0L, allocated);
        Assert.AreEqual(9_999, row.GetValue<int>(0));
    }

    [TestMethod]
    public void SerialProjection_MutatingOneResultDoesNotChangeOtherRowsOrLaterRuns()
    {
        var entity = new OutputEntity { Id = 1, Optional = 7 };
        using var query = CreateAndRunVirtualMachine(
            "select a.Id, a.Optional from #schema.first() a where a.Id > 0", new[] { entity, entity });
        using var first = query.Run(TestContext.CancellationToken);
        first[0].AssignValue(0, 99);

        TableMaterializationTestHelper.AssertRowsInOrder(first, [99, 7], [1, 7]);
        TableMaterializationTestHelper.AssertRowsInOrder(query.Run(TestContext.CancellationToken), [1, 7], [1, 7]);
        Assert.AreEqual(1, entity.Id);
        Assert.AreNotSame(first[0], first[1]);
    }

    [TestMethod]
    [DataRow(SerialSource)]
    [DataRow(BufferedJoinSource)]
    public void FinalProjection_CancellationBeforeMaterializationDoesNotEvaluateExpressions(string source)
    {
        var entity = new OutputEntity { Id = 1 };
        using var query = CreateAndRunVirtualMachine<OutputEntity, OutputLibrary>(
            $"select a.Id, a.Next() {source}", [entity]);
        using var cancellation = new CancellationTokenSource();
        using var table = query.Run(cancellation.Token);
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => _ = table.Count);
        Assert.AreEqual(0, entity.Calls);
    }

    [TestMethod]
    [DataRow(SerialSource)]
    [DataRow(BufferedJoinSource)]
    public void FinalProjection_DiscardingDeferredResultDoesNotEvaluateExpressions(string source)
    {
        var entity = new OutputEntity { Id = 1 };
        using var query = CreateAndRunVirtualMachine<OutputEntity, OutputLibrary>(
            $"select a.Id, a.Next() {source}", [entity]);

        query.Run(TestContext.CancellationToken).Dispose();

        Assert.AreEqual(0, entity.Calls);
    }

    public sealed class OutputEntity
    {
        public int Id { get; init; }
        public int? Optional { get; init; }
        public int Calls { get; private set; }
        public int Next() => ++Calls;
    }

    public sealed class OutputLibrary : LibraryBase
    {
        [BindableMethod]
        [NonDeterministic]
        public int Next([InjectSpecificSource(typeof(OutputEntity))] OutputEntity entity) => entity.Next();
    }
}
