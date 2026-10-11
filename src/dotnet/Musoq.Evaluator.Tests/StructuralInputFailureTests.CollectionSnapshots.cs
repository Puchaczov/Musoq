using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public sealed partial class StructuralInputFailureTests
{
    [TestMethod]
    public void NativeCollectionSnapshot_ShouldOwnTheValuesConsumedBySql()
    {
        const string sql = "param(values: int[]) select Position from #schema.first() where Position in $values order by Position";
        using var compiled = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString("N"),
            LibraryExecutionFixture.CreateProvider(new[] { new SnapshotRow(7), new SnapshotRow(9), new SnapshotRow(70), new SnapshotRow(90) }),
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        var values = new List<int> { 7, 9 };
        compiled.TypedParameters.SetCollection("values", values);
        var snapshot = compiled.TypedParameters.Capture(TestContext.CancellationToken);
        values[0] = 70;
        values[1] = 90;
        using var result = compiled.Run(new TypedQueryRunOptions(TestContext.CancellationToken, snapshot));
        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)));
        CollectionAssert.AreEqual(new[] { 7, 9 }, result.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new[] { 70, 90 }, values);
    }

    public sealed record SnapshotRow(int Position);
}
