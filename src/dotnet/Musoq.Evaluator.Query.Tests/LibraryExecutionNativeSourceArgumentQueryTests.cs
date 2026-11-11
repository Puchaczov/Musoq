using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using static Musoq.Evaluator.Tests.LibraryExecutionNativeSourceArgumentQueryTestsFixtures;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionNativeSourceArgumentQueryTests : MSTestContextTestBase
{
    [TestMethod]
    public void ApplyArguments_ShouldRetainConcreteNullableValuesForEveryInvocation()
    {
        const string sql = ApplyQuery;
        var schema = new NativeSchema();
        using var query = InstanceCreator.CompileForExecution(
            sql,
            Guid.NewGuid().ToString(),
            new NativeProvider(schema),
            new TestsLoggerResolver()
        );
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int)),
            ("Amount", typeof(decimal?)),
            ("Label", typeof(string))
        );
        var rows = table
            .Select(row => (row.GetValue<int>(0), row.GetValue<decimal?>(1), row.GetValue<string>(2)))
            .ToArray();
        CollectionAssert.AreEqual(
            new (int, decimal?, string)[] { (1, 1.25m, "first"), (2, null, "empty"), (3, -4m, "last") },
            rows
        );
        CollectionAssert.AreEqual(
            new (decimal?, string)[] { (1.25m, "first"), (null, "empty"), (-4m, "last") },
            schema.Invocations.ToArray()
        );
    }

    [TestMethod]
    [DataRow("1.25", "value")]
    [DataRow("null", "empty")]
    public void SourceLiteralArguments_ShouldUseDeclaredNullableSignature(string amount, string label)
    {
        var schema = new NativeSchema();
        using var query = InstanceCreator.CompileForExecution(
            $"select Amount, Label from #native.echo({amount}, '{label}')",
            Guid.NewGuid().ToString(),
            new NativeProvider(schema),
            new TestsLoggerResolver()
        );
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Amount", typeof(decimal?)), ("Label", typeof(string)));
        var rows = table.Select(row => (row.GetValue<decimal?>(0), row.GetValue<string>(1))).ToArray();
        Assert.HasCount(1, rows);
        Assert.AreEqual(amount == "null" ? (decimal?)null : 1.25m, rows[0].Item1);
        Assert.AreEqual(label, rows[0].Item2);
        CollectionAssert.AreEqual(rows, schema.Invocations.ToArray());
    }
}
