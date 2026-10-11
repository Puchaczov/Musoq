using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class FullOuterMatchTrackingTests : GenericEntityTestBase
{
    [TestMethod]
    public void VolatilePredicate_TrueThenFalse_PreservesOriginalMatch()
    {
        AssertVolatilePredicateRows(1, [1, 2]);
    }

    [TestMethod]
    public void VolatilePredicate_FalseThenTrue_PreservesBothUnmatchedRows()
    {
        AssertVolatilePredicateRows(2, [1, null], [null, 2]);
    }

    private void AssertVolatilePredicateRows(int matchingCall, params object?[][] expected)
    {
        var entity = new MatchTrackingEntity { Id = 1 };
        using var query = CreateAndRunVirtualMachine<MatchTrackingEntity, MatchTrackingLibrary>(
            $"select a.Id, b.Id from #schema.first() a full outer join values {{ (Id: 2) }} b " +
            $"on a.Id < b.Id and a.Matches({matchingCall})", [entity]);

        var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(table, expected);
        Assert.AreEqual(1, entity.Calls);
    }

    [TestMethod]
    public void DuplicateRightOccurrences_AreTrackedIndividually()
    {
        var entity = new MatchTrackingEntity { Id = 1 };
        using var query = CreateAndRunVirtualMachine<MatchTrackingEntity, MatchTrackingLibrary>(
            "select a.Id, b.Id from #schema.first() a " +
            "full outer join values { (Id: 2), (Id: 2) } b on a.Id < b.Id and a.Matches(1)", [entity]);

        TableMaterializationTestHelper.AssertRowsInOrder(query.Run(TestContext.CancellationToken), [1, 2], [null, 2]);
        Assert.AreEqual(2, entity.Calls);
    }

    [TestMethod]
    public void FilteredMatch_DoesNotEmitUnmatchedRightRow()
    {
        var entity = new MatchTrackingEntity { Id = 1 };
        using var query = CreateAndRunVirtualMachine<MatchTrackingEntity, MatchTrackingLibrary>(
            "select a.Id, b.Id from #schema.first() a full outer join values { (Id: 2) } b " +
            "on a.Id < b.Id and a.Matches(1) where a.Id is null", [entity]);

        TableMaterializationTestHelper.AssertRowsInOrder(query.Run(TestContext.CancellationToken));
        Assert.AreEqual(1, entity.Calls);
    }

    [TestMethod]
    public void EmptyLeft_DoesNotEvaluatePredicate()
    {
        using var query = CreateAndRunVirtualMachine<MatchTrackingEntity, MatchTrackingLibrary>(
            "select a.Id, b.Id from #schema.first() a full outer join values { (Id: 2) } b " +
            "on a.Id < b.Id and a.Matches(1)", []);

        TableMaterializationTestHelper.AssertRowsInOrder(query.Run(TestContext.CancellationToken), [null, 2]);
    }

    public sealed class MatchTrackingEntity
    {
        public int Id { get; init; }
        public int Calls { get; private set; }
        public bool Matches(int call) => ++Calls == call;
    }

    public sealed class MatchTrackingLibrary : LibraryBase
    {
        [BindableMethod]
        [NonDeterministic]
        public bool Matches([InjectSpecificSource(typeof(MatchTrackingEntity))] MatchTrackingEntity entity, int call)
            => entity.Matches(call);
    }
}
