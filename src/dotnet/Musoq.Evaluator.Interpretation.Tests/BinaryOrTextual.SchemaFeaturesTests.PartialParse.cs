using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
    [TestMethod]
    public void Query_PartialParse_CrossApplyValidText_ShouldReturnPartialResultShape()
    {
        var query = (
            "\n"
            + "            text KeyValue {\n"
            + "                Key: until '=',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select\n"
            + "                p.ParsedFields,\n"
            + "                p.ErrorField,\n"
            + "                p.ErrorMessage,\n"
            + "                p.BytesConsumed\n"
            + "            from #test.lines() f\n"
            + "            cross apply PartialParse<KeyValue>(f.Text) p"
        );

        var entities = new[]
        {
            new TextEntity { Name = "valid.txt", Text = "host=localhost" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("ParsedFields", PartialPayloadAssertions.ColumnType(table, "KeyValue")),
            ("ErrorField", typeof(string)),
            ("ErrorMessage", typeof(string)),
            ("BytesConsumed", typeof(int))
        );
        Assert.AreEqual(1, table.Count);
        var parsedFields = PartialPayloadAssertions.CompletedValues(table[0].ReadValue(0)!);
        Assert.AreEqual("host", parsedFields["Key"]);
        Assert.AreEqual("localhost", parsedFields["Value"]);
        Assert.IsNull(table[0].ReadValue(1));
        Assert.IsNull(table[0].ReadValue(2));
        Assert.AreEqual("host=localhost".Length, table[0].ReadValue(3));
    }

    [TestMethod]
    public void Query_PartialParse_CrossApplyMalformedText_ShouldReturnErrorMetadata()
    {
        var query = (
            "\n"
            + "            text KeyValue {\n"
            + "                Key: until '=',\n"
            + "                Value: chars[5]\n"
            + "            };\n"
            + "            select\n"
            + "                p.ParsedFields,\n"
            + "                p.ErrorField,\n"
            + "                p.ErrorMessage,\n"
            + "                p.BytesConsumed\n"
            + "            from #test.lines() f\n"
            + "            cross apply PartialParse<KeyValue>(f.Text) p"
        );

        var entities = new[]
        {
            new TextEntity { Name = "truncated.txt", Text = "host=" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("ParsedFields", PartialPayloadAssertions.ColumnType(table, "KeyValue")),
            ("ErrorField", typeof(string)),
            ("ErrorMessage", typeof(string)),
            ("BytesConsumed", typeof(int))
        );
        Assert.AreEqual(1, table.Count);
        var parsedFields = PartialPayloadAssertions.CompletedValues(table[0].ReadValue(0)!);
        Assert.AreEqual("host", parsedFields["Key"]);
        Assert.IsFalse(parsedFields.ContainsKey("Value"));
        Assert.AreEqual("Value", table[0].ReadValue(1));
        StringAssert.Contains((string)table[0].ReadValue(2)!, "ISE0001");
        Assert.AreEqual("host=".Length, table[0].ReadValue(3));
    }

    [TestMethod]
    public void Query_PartialParse_OutsideApply_ShouldReportInterpretFunctionOutsideApply()
    {
        var query = (
            "\n"
            + "            text KeyValue {\n"
            + "                Key: until '=',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select PartialParse<KeyValue>(f.Text)\n"
            + "            from #test.lines() f"
        );

        var entities = new[]
        {
            new TextEntity { Name = "valid.txt", Text = "host=localhost" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver,
                TestCompilationOptions
            )
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3033_InterpretFunctionOutsideApply, DiagnosticPhase.Bind);
    }
}
