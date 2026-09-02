using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticInterpret054ApplyNullSemanticsTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void TryInterpret_CrossAndOuterApply_ShouldDifferentiateMalformedAndEmptyRows()
    {
        const string crossApplyQuery = @"
            binary Packet {
                Value: int le
            };
            select
                f.Name,
                p.Value
            from #test.files() f
            cross apply TryInterpret<Packet>(f.Content) p
            order by f.Name";
        const string outerApplyQuery = @"
            binary Packet {
                Value: int le
            };
            select
                f.Name,
                p.Value
            from #test.files() f
            outer apply TryInterpret<Packet>(f.Content) p
            order by f.Name";

        var entities = new[]
        {
            new BinaryEntity { Name = "empty.bin", Content = [] },
            new BinaryEntity { Name = "malformed.bin", Content = [0x2A] },
            new BinaryEntity { Name = "valid.bin", Content = [0x2A, 0x00, 0x00, 0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { ["#test"] = entities });

        var crossApplyTable = CompileGeneratedQuery(
                crossApplyQuery,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions)
            .Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            crossApplyTable,
            ("f.Name", typeof(string)),
            ("p.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            crossApplyTable,
            ["valid.bin", 42]);

        var outerApplyTable = CompileGeneratedQuery(
                outerApplyQuery,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions)
            .Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            outerApplyTable,
            ("f.Name", typeof(string)),
            ("p.Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            outerApplyTable,
            new object?[] { "empty.bin", null },
            new object?[] { "malformed.bin", null },
            ["valid.bin", 42]);
    }

    [TestMethod]
    public void TryParse_OuterApply_ShouldPreserveMalformedSourceRowWithoutExceptionLeakage()
    {
        const string query = @"
            text KeyValue {
                Key: until ':',
                Value: rest
            };
            select
                f.Name,
                p.Key,
                p.Value
            from #test.lines() f
            outer apply TryParse<KeyValue>(f.Text) p
            order by f.Name";

        var entities = new[]
        {
            new TextEntity { Name = "bad.txt", Text = "missing-delimiter" },
            new TextEntity { Name = "good.txt", Text = "host:localhost" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { ["#test"] = entities });

        var table = CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions)
            .Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("f.Name", typeof(string)),
            ("p.Key", typeof(string)),
            ("p.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            new object?[] { "bad.txt", null, null },
            ["good.txt", "host", "localhost"]);
    }
}
