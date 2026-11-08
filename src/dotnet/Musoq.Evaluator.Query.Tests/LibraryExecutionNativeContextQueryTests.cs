using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionNativeContextQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void JoinedEqualEntities_ShouldPreserveInjectedReferenceIdentity()
    {
        const string sql = """
            select a.SameSource(a.Self) as SameA, b.SameSource(b.Self) as SameB,
                a.SameSource(b.Self) as CrossSource
            from #schema.first() a inner join #schema.first() b on a.Id = b.Id
            """;
        ContextEntity[] input = [new(1), new(1)];
        using var query = CreateAndRunVirtualMachine<ContextEntity, ContextLibrary>(sql, input);
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table,
            ("SameA", typeof(bool)), ("SameB", typeof(bool)), ("CrossSource", typeof(bool)));
        var rows = table.Select(row => (row.GetValue<bool>(0), row.GetValue<bool>(1), row.GetValue<bool>(2))).ToArray();
        Assert.HasCount(4, rows);
        Assert.IsTrue(rows.All(row => row.Item1 && row.Item2));
        Assert.AreEqual(2, rows.Count(row => row.Item3));
        Assert.AreEqual(2, rows.Count(row => !row.Item3));
    }

    [TestMethod]
    public void LeftJoinedValueSource_ShouldDistinguishPresentDefaultValueFromMissingSource()
    {
        const string sql = """
            select a.ValueSourcePosition() as Position, b.ValueSourcePosition() as OtherPosition
            from #schema.first() a left join #schema.first() b
                on a.Position = b.Position and b.Position = 0
            order by Position
            """;
        LibraryExecutionValueEntity[] input = [new(0, 0, 0m), new(1, 0, 1m), new(2, 0, 2m)];
        using var query = CreateAndRunVirtualMachine<LibraryExecutionValueEntity, LibraryExecutionLibrary>(sql, input);
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("OtherPosition", typeof(int?)));
        var rows = table.Select(row => (row.GetValue<int>(0), row.GetValue<int?>(1))).ToArray();
        CollectionAssert.AreEqual(new (int, int?)[] { (0, 0), (1, null), (2, null) }, rows);
    }

    [TestMethod]
    public void MaterializedInjectedResults_ShouldUseNativeRowsWithoutErasedContextAccess()
    {
        const string sql = """
            with identities as (
                select Id, SameSource(Self) as Same from #schema.first())
            select Id, Same from identities order by Id
            """;
        using var query = CreateAndRunVirtualMachine<ContextEntity, ContextLibrary>(sql, [new(0), new(1)]);
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Same", typeof(bool)));
        var rows = table.Select(row => (row.GetValue<int>(0), row.GetValue<bool>(1))).ToArray();
        CollectionAssert.AreEqual(new[] { (0, true), (1, true) }, rows);
        Assert.IsNull(typeof(Row).GetProperty("Contexts"));
        Assert.IsNull(typeof(Row).Assembly.GetType("Musoq.Evaluator.Tables.ContextMaterializer"));
    }

    public sealed record ContextEntity(int Id)
    {
        public ContextEntity Self => this;
    }

    public sealed class ContextLibrary : LibraryBase
    {
        [BindableMethod]
        public bool SameSource([InjectSpecificSource(typeof(ContextEntity))] ContextEntity source, ContextEntity candidate)
            => ReferenceEquals(source, candidate);
    }
}
