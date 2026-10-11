using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionProviderQueryTests
{
    internal const string OuterContainsQuery = "select a.Id as Id from #schema.first() a left join #schema.first() b " +
        "on b.Id = a.Id + 1 where b.Label contains ('match', null) order by a.Id";

    internal const string FilteredNotInQuery = "select Count(*) filter (where Label not in ('match', null)) as Present from #schema.first()";

    internal const string AggregateMembershipInputQuery = "select Count(Label in ('match')) as Present from #schema.first()";

    internal static OuterMembershipRow[] CreateOuterMembershipRows() =>
        [new(1, "match"), new(2, "other"), new(3, null)];

    [TestMethod]
    public void OuterJoin_ConstantContainsFilter_ShouldIncludeNullExtendedRows()
    {
        using var compiled = InstanceCreator.CompileForExecution(OuterContainsQuery, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(CreateOuterMembershipRows()), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)));
        Assert.HasCount(2, rows);
        Assert.AreEqual(2, rows[0].GetValue<int>(0));
        Assert.AreEqual(3, rows[1].GetValue<int>(0));
    }

    [TestMethod]
    public void OuterJoin_ConstantSqlInFilter_ShouldExcludeUnknownNullExtendedRows()
    {
        const string query = "select a.Id as Id from #schema.first() a left join #schema.first() b " +
            "on b.Id = a.Id + 1 where b.Id in (2, 3) order by a.Id";
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(CreateOuterMembershipRows()), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)));
        Assert.HasCount(2, rows);
        Assert.AreEqual(1, rows[0].GetValue<int>(0));
        Assert.AreEqual(2, rows[1].GetValue<int>(0));
    }

    [TestMethod]
    public void AggregateFilter_NotInWithNullMember_ShouldCountNoRows()
    {
        using var compiled = InstanceCreator.CompileForExecution(FilteredNotInQuery, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(CreateOuterMembershipRows()), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Present", typeof(long)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(0L, rows[0].GetValue<long>(0));
    }

    [TestMethod]
    public void AggregateInput_ConstantSqlMembership_ShouldCountOnlyKnownValues()
    {
        using var compiled = InstanceCreator.CompileForExecution(AggregateMembershipInputQuery, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(CreateOuterMembershipRows()), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Present", typeof(long)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(2L, rows[0].GetValue<long>(0));
    }

    public sealed record OuterMembershipRow(int Id, string? Label);
}
