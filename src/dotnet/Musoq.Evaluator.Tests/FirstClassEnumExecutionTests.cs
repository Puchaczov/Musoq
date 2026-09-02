using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class FirstClassEnumExecutionTests : GenericEntityTestBase
{
    [TestMethod]
    public void NativeEnumProjectionAndHelpers_ShouldUsePrimitiveRuntimeValues()
    {
        var table = CreateAndRunVirtualMachine(
            """
            select
                e.Status as Status,
                EnumValue(e.Status) as StatusValue,
                EnumName(e.Status) as StatusName,
                IsDefined(e.Status) as StatusDefined,
                HasAnyFlags(e.Access, 'Read') as HasRead,
                HasAllFlags(e.Access, 'Read', 'Write') as HasReadWrite,
                HasAnyFlags(e.Access) as HasAnyZero,
                HasAllFlags(e.Access) as HasAllZero,
                EnumName(e.OptionalStatus) as OptionalName,
                IsDefined(e.OptionalStatus) as OptionalDefined,
                HasAnyFlags(e.OptionalAccess, 'Read') as OptionalHasRead
            from #schema.first() e
            """,
            [
                new EnumExecutionEntity(
                    NativeJobStatus.Running,
                    NativeFileAccess.Read | NativeFileAccess.Write,
                    NativeJobStatus.Queued,
                    NativeFileAccess.Read),
                new EnumExecutionEntity(
                    (NativeJobStatus)99,
                    (NativeFileAccess)8,
                    null,
                    null)
            ]).Run(TestContext.CancellationToken);

        Assert.HasCount(2, table);
        CollectionAssert.AreEqual(
            new object?[] { (short)20, (short)20, "Running", true, true, true, false, true, "Queued", true, true },
            table[0].Values);
        CollectionAssert.AreEqual(
            new object?[] { (short)99, (short)99, null, false, false, false, false, true, null, false, false },
            table[1].Values);
        Assert.IsFalse(table.SelectMany(static row => row.Values).Any(static value => value is Enum));

        var statusColumn = table.Columns.Single(static column => column.ColumnName == "Status");
        Assert.AreEqual(typeof(short), statusColumn.ColumnType);
        Assert.AreEqual(typeof(short), statusColumn.SourceReadType);
        Assert.IsNotNull(statusColumn.EnumType);
        Assert.AreEqual(typeof(NativeJobStatus).FullName, statusColumn.EnumType.DisplayName);
        Assert.AreEqual(EnumTypeOrigin.NativeClr, statusColumn.EnumType.Origin);
        Assert.AreEqual(EnumUnderlyingKind.Int16, statusColumn.EnumType.UnderlyingKind);
        Assert.IsTrue(table.Columns
            .Where(static column => column.ColumnName != "Status")
            .All(static column => column.EnumType == null));
    }

    [TestMethod]
    public void NativeEnumPredicatesAndCase_ShouldRetainNominalIdentityUntilProjection()
    {
        var table = CreateAndRunVirtualMachine(
            """
            select
                case when e.Status = 'Running' then e.Status else 'Queued' end as Normalized
            from #schema.first() e
            where e.Status in ('Queued', 'Running')
            """,
            [
                new EnumExecutionEntity(NativeJobStatus.Queued, NativeFileAccess.None, null, null),
                new EnumExecutionEntity(NativeJobStatus.Running, NativeFileAccess.Read, null, null),
                new EnumExecutionEntity(NativeJobStatus.Finished, NativeFileAccess.Write, null, null)
            ]).Run(TestContext.CancellationToken);

        Assert.HasCount(2, table);
        CollectionAssert.AreEqual(new object?[] { (short)10 }, table[0].Values);
        CollectionAssert.AreEqual(new object?[] { (short)20 }, table[1].Values);
        var column = table.Columns.Single();
        Assert.AreEqual(typeof(short), column.ColumnType);
        Assert.AreEqual(typeof(short), column.SourceReadType);
        Assert.IsNotNull(column.EnumType);
        Assert.AreEqual(typeof(NativeJobStatus).FullName, column.EnumType.DisplayName);
    }

    [TestMethod]
    public void NativeEnumCteGroupingDistinctAndSet_ShouldExecuteOnPrimitiveCarriers()
    {
        var rows = new[]
        {
            new EnumExecutionEntity(NativeJobStatus.Queued, NativeFileAccess.None, null, null),
            new EnumExecutionEntity(NativeJobStatus.Running, NativeFileAccess.Read, null, null),
            new EnumExecutionEntity(NativeJobStatus.Running, NativeFileAccess.Write, null, null)
        };
        var cte = CreateAndRunVirtualMachine(
            "with states as (select e.Status from #schema.first() e) " +
            "select Status from states where Status = 'Running'",
            rows).Run(TestContext.CancellationToken);
        var grouped = CreateAndRunVirtualMachine(
            "select e.Status, Count(e.Status) as Total from #schema.first() e group by e.Status",
            rows).Run(TestContext.CancellationToken);
        var distinct = CreateAndRunVirtualMachine(
            "select distinct e.Status from #schema.first() e",
            rows).Run(TestContext.CancellationToken);
        var set = CreateAndRunVirtualMachine(
            "select e.Status from #schema.first() e union select e.Status from #schema.first() e",
            rows).Run(TestContext.CancellationToken);

        Assert.HasCount(2, cte);
        Assert.IsTrue(cte.All(static row => row.Values[0] is short value && value == 20));
        Assert.IsNotNull(cte.Columns.Single().EnumType);
        Assert.HasCount(2, grouped);
        Assert.IsTrue(grouped.All(static row => row.Values[0] is short));
        Assert.IsNotNull(grouped.Columns.ElementAt(0).EnumType);
        Assert.IsNull(grouped.Columns.ElementAt(1).EnumType);
        Assert.HasCount(2, distinct);
        Assert.IsTrue(distinct.All(static row => row.Values[0] is short));
        Assert.IsNotNull(distinct.Columns.Single().EnumType);
        Assert.HasCount(2, set);
        Assert.IsTrue(set.All(static row => row.Values[0] is short));
        Assert.IsNotNull(set.Columns.Single().EnumType);
    }

    [TestMethod]
    public void NativeEnumEqualityJoin_ShouldUseCarrierKeys()
    {
        var table = CreateAndRunVirtualMachine(
            "select a.Status from #schema.first() a inner join #schema.second() b on a.Status = b.Status",
            [
                new EnumExecutionEntity(NativeJobStatus.Queued, NativeFileAccess.None, null, null),
                new EnumExecutionEntity(NativeJobStatus.Running, NativeFileAccess.Read, null, null)
            ],
            [
                new EnumExecutionEntity(NativeJobStatus.Running, NativeFileAccess.Write, null, null),
                new EnumExecutionEntity(NativeJobStatus.Finished, NativeFileAccess.None, null, null)
            ]).Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual((short)20, table[0].Values[0]);
        Assert.IsNotNull(table.Columns.Single().EnumType);
    }

    [TestMethod]
    public void NativeEnumNotIn_ShouldPreserveNegationThroughExecutionLowering()
    {
        var table = CreateAndRunVirtualMachine(
            "select e.Status from #schema.first() e where e.Status not in ('Finished')",
            [
                new EnumExecutionEntity(NativeJobStatus.Queued, NativeFileAccess.None, null, null),
                new EnumExecutionEntity(NativeJobStatus.Running, NativeFileAccess.Read, null, null),
                new EnumExecutionEntity(NativeJobStatus.Finished, NativeFileAccess.Write, null, null)
            ]).Run(TestContext.CancellationToken);

        Assert.HasCount(2, table);
        CollectionAssert.AreEqual(new object?[] { (short)10, (short)20 },
            table.Select(static row => row.Values[0]).ToArray());
    }

    public sealed record EnumExecutionEntity(
        NativeJobStatus Status,
        NativeFileAccess Access,
        NativeJobStatus? OptionalStatus,
        NativeFileAccess? OptionalAccess);

    public enum NativeJobStatus : short
    {
        Queued = 10,
        Running = 20,
        Active = 20,
        Finished = 30
    }

    [Flags]
    public enum NativeFileAccess : uint
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReadWrite = Read | Write
    }
}
