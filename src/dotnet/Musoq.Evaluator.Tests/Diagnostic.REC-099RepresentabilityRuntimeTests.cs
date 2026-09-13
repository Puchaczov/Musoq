using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC099RepresentabilityRuntimeTests : GenericEntityTestBase
{
    [TestMethod]
    public void UnknownRepresentableNativeEnumValue_ShouldRemainPrimitiveAndPreserveCarrier()
    {
        var table = CreateAndRunVirtualMachine(
            "select e.Status as Status, EnumValue(e.Status) as StatusValue, EnumName(e.Status) as StatusName, IsDefined(e.Status) as StatusDefined from #schema.first() e",
            [new RuntimeEnumEntity((RuntimeStatus)99)]).Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(
            new object?[] { (short)99, (short)99, null, false },
            table[0].Values);
        Assert.IsFalse(table.SelectMany(static row => row.Values).Any(static value => value is Enum));
        var statusColumn = table.Columns.Single(static column => column.ColumnName == "Status");
        Assert.AreEqual(typeof(short), statusColumn.ColumnType);
        Assert.AreEqual(typeof(short), statusColumn.SourceReadType);
        Assert.IsNotNull(statusColumn.EnumType);
    }

    public sealed record RuntimeEnumEntity(RuntimeStatus Status);

    public enum RuntimeStatus : short
    {
        Known = 10
    }
}
