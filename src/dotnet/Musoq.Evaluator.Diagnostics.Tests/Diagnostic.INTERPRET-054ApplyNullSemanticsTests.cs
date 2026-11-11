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
        const string crossApplyQuery = (
            "\n"
            + "            binary Packet {\n"
            + "                Value: int le\n"
            + "            };\n"
            + "            select\n"
            + "                f.Name,\n"
            + "                p.Value\n"
            + "            from #test.files() f\n"
            + "            cross apply TryInterpret<Packet>(f.Content) p\n"
            + "            order by f.Name"
        );
        const string outerApplyQuery = (
            "\n"
            + "            binary Packet {\n"
            + "                Value: int le\n"
            + "            };\n"
            + "            select\n"
            + "                f.Name,\n"
            + "                p.Value\n"
            + "            from #test.files() f\n"
            + "            outer apply TryInterpret<Packet>(f.Content) p\n"
            + "            order by f.Name"
        );

        var entities = new[]
        {
            new BinaryEntity { Name = "empty.bin", Content = [] },
            new BinaryEntity { Name = "malformed.bin", Content = [0x2A] },
            new BinaryEntity { Name = "valid.bin", Content = [0x2A, 0x00, 0x00, 0x00] },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { ["#test"] = entities }
        );

        var crossApplyTable = CompileGeneratedQuery(
                crossApplyQuery,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions
            )
            .Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(crossApplyTable, ("Name", typeof(string)), ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(crossApplyTable, ["valid.bin", 42]);

        var outerApplyTable = CompileGeneratedQuery(
                outerApplyQuery,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions
            )
            .Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            outerApplyTable,
            ("Name", typeof(string)),
            ("Value", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            outerApplyTable,
            new object?[] { "empty.bin", null },
            new object?[] { "malformed.bin", null },
            ["valid.bin", 42]
        );
    }

    [TestMethod]
    public void TryParse_OuterApply_ShouldPreserveMalformedSourceRowWithoutExceptionLeakage()
    {
        const string query = (
            "\n"
            + "            text KeyValue {\n"
            + "                Key: until ':',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select\n"
            + "                f.Name,\n"
            + "                p.Key,\n"
            + "                p.Value\n"
            + "            from #test.lines() f\n"
            + "            outer apply TryParse<KeyValue>(f.Text) p\n"
            + "            order by f.Name"
        );

        var entities = new[]
        {
            new TextEntity { Name = "bad.txt", Text = "missing-delimiter" },
            new TextEntity { Name = "good.txt", Text = "host:localhost" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { ["#test"] = entities }
        );

        var table = CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions
            )
            .Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Key", typeof(string)),
            ("Value", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            new object?[] { "bad.txt", null, null },
            ["good.txt", "host", "localhost"]
        );
    }
}
