using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public sealed partial class FirstClassEnumExecutionTests
{
    [TestMethod]
    [DataRow("select e.Status as State from #schema.first() e order by EnumValue(e.Status)")]
    [DataRow("select e.Status as State, EnumValue(e.Status) as Ordinal from #schema.first() e order by Ordinal")]
    [DataRow("select e.Status as State from #schema.first() e order by EnumValue(e.Status) skip 1 take 1")]
    [DataRow(
        "select e.Status as State, RowNumber() over (partition by e.Status order by EnumValue(e.Status)) as RowNo from #schema.first() e order by RowNo"
    )]
    public void OrderedNativeEnumProjection_ShouldPreservePortableDescriptor(string query)
    {
        using var table = CreateAndRunVirtualMachine(
                query,
                [
                    new EnumExecutionEntity(NativeJobStatus.Running, NativeFileAccess.None, null, null),
                    new EnumExecutionEntity(NativeJobStatus.Queued, NativeFileAccess.Read, null, null),
                ]
            )
            .Run(TestContext.CancellationToken);

        var column = table.Columns.Single(column => column.ColumnName == "State");
        Assert.AreEqual(typeof(short), column.ColumnType);
        Assert.AreEqual(typeof(short), column.SourceReadType);
        Assert.IsNotNull(column.EnumType);
        Assert.AreEqual(typeof(NativeJobStatus).FullName, column.EnumType.DisplayName);
        Assert.AreEqual(EnumTypeOrigin.NativeClr, column.EnumType.Origin);
        Assert.AreEqual(EnumUnderlyingKind.Int16, column.EnumType.UnderlyingKind);
        Assert.IsTrue(table.All(row => row.ReadValue(0) is short));
        Assert.IsFalse(table.SelectMany(row => row.ReadValues()).Any(value => value is Enum));
    }
}
