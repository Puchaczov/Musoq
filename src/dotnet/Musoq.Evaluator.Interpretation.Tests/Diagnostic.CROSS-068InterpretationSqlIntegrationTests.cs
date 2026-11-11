using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;
using ComponentBinaryEntity = Musoq.Evaluator.Tests.Components.BinaryEntity;
using ComponentBinarySchemaProvider = Musoq.Evaluator.Tests.Components.BinarySchemaProvider;
using ComponentTextEntity = Musoq.Evaluator.Tests.Components.TextEntity;
using ComponentTextSchemaProvider = Musoq.Evaluator.Tests.Components.TextSchemaProvider;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
    [TestMethod]
    public void InterpretationSchemas_WithTableCoupleCtesSetsAndWindows_ShouldComposeSqlPipeline()
    {
        const string query = (
            "table TextSourceRow { Name: string, Text: string };\n"
            + "couple #txt.lines with table TextSourceRow as Lines;\n"
            + "binary Packet {\n"
            + "    Id: byte,\n"
            + "    Score: short le\n"
            + "};\n"
            + "text LabelRecord {\n"
            + "    Id: until ':',\n"
            + "    Label: rest trim\n"
            + "};\n"
            + "with binaryRows as (\n"
            + "    select ToString(p.Id) as BinaryId, p.Score as Score\n"
            + "    from #bin.files() f\n"
            + "    cross apply Interpret<Packet>(f.Content) p\n"
            + "),\n"
            + "textRows as (\n"
            + "    select p.Id as TextId, p.Label as Label\n"
            + "    from Lines() l\n"
            + "    cross apply Parse<LabelRecord>(l.Text) p\n"
            + "    where p.Label <> ''\n"
            + "),\n"
            + "combined as (\n"
            + "    select t.Label as Label, ToInt32(b.Score) as Score\n"
            + "    from binaryRows b\n"
            + "    inner join textRows t on b.BinaryId = t.TextId\n"
            + "    union all (Label, Score)\n"
            + "    select 'Alpha' as Label, ToInt32(0::Short) as Score\n"
            + "    from values { ( Marker: 1 ) } marker\n"
            + ")\n"
            + "select c.Label,\n"
            + "       Sum(c.Score) as TotalScore,\n"
            + "       Count(c.Label) as Records,\n"
            + "       RowNumber() over (order by Sum(c.Score) desc) as Rank\n"
            + "from combined c\n"
            + "group by c.Label\n"
            + "having Sum(c.Score) > 0\n"
            + "qualify RowNumber() over (order by Sum(c.Score) desc) <= 2\n"
            + "order by TotalScore desc"
        );

        var schemaProvider = new MixedSchemaProvider(
            new Dictionary<string, IEnumerable<ComponentBinaryEntity>>
            {
                ["#bin"] =
                [
                    new ComponentBinaryEntity { Name = "1.bin", Content = [1, 100, 0] },
                    new ComponentBinaryEntity { Name = "2.bin", Content = [2, 200, 0] },
                    new ComponentBinaryEntity { Name = "3.bin", Content = [3, 50, 0] },
                ],
            },
            new Dictionary<string, IEnumerable<ComponentTextEntity>>
            {
                ["#txt"] =
                [
                    new ComponentTextEntity { Name = "1.txt", Text = "1:Alpha" },
                    new ComponentTextEntity { Name = "2.txt", Text = "2:Alpha" },
                    new ComponentTextEntity { Name = "3.txt", Text = "3:Beta" },
                ],
            }
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
            ("Label", typeof(string)),
            ("TotalScore", typeof(int?)),
            ("Records", typeof(long)),
            ("Rank", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Alpha", 300, 3L, 1L], ["Beta", 50, 1L, 2L]);
    }

    [TestMethod]
    public void SafeInterpretation_WithOuterApplyCtesAndAggregate_ShouldPreserveFailedRows()
    {
        const string binaryQuery = (
            "binary Packet {\n"
            + "    Id: byte,\n"
            + "    Value: short le\n"
            + "};\n"
            + "text LabelRecord {\n"
            + "    Id: until ':',\n"
            + "    Value: rest trim\n"
            + "};\n"
            + "with binarySafe as (\n"
            + "    select f.Name as Name, ToString(p.Id) as Key, ToString(p.Value) as Value\n"
            + "    from #bin.files() f\n"
            + "    outer apply TryInterpret<Packet>(f.Content) p\n"
            + ")\n"
            + "select r.Name, r.Key, Count(r.Value) as ParsedValues,\n"
            + "       RowNumber() over (order by r.Name) as Sequence\n"
            + "from binarySafe r\n"
            + "group by r.Name, r.Key\n"
            + "order by Sequence"
        );

        var binaryVm = CompileGeneratedQuery(
            binaryQuery,
            Guid.NewGuid().ToString(),
            new ComponentBinarySchemaProvider(
                new Dictionary<string, IEnumerable<ComponentBinaryEntity>>
                {
                    ["#bin"] =
                    [
                        new ComponentBinaryEntity { Name = "01-invalid.bin", Content = [1] },
                        new ComponentBinaryEntity { Name = "02-valid.bin", Content = [1, 10, 0] },
                    ],
                }
            ),
            LoggerResolver,
            TestCompilationOptions
        );
        var binaryTable = binaryVm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            binaryTable,
            ("Name", typeof(string)),
            ("Key", typeof(string)),
            ("ParsedValues", typeof(long)),
            ("Sequence", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            binaryTable,
            ["01-invalid.bin", null, 0L, 1L],
            ["02-valid.bin", "1", 1L, 2L]
        );

        const string textQuery = (
            "text LabelRecord {\n"
            + "    Id: until ':',\n"
            + "    Value: rest trim\n"
            + "};\n"
            + "with textSafe as (\n"
            + "    select f.Name as Name, p.Id as Key, p.Value as Value\n"
            + "    from #txt.files() f\n"
            + "    outer apply TryParse<LabelRecord>(f.Text) p\n"
            + ")\n"
            + "select r.Name, r.Key, Count(r.Value) as ParsedValues,\n"
            + "       RowNumber() over (order by r.Name) as Sequence\n"
            + "from textSafe r\n"
            + "group by r.Name, r.Key\n"
            + "order by Sequence"
        );

        var textVm = CompileGeneratedQuery(
            textQuery,
            Guid.NewGuid().ToString(),
            new ComponentTextSchemaProvider(
                new Dictionary<string, IEnumerable<ComponentTextEntity>>
                {
                    ["#txt"] =
                    [
                        new ComponentTextEntity { Name = "01-invalid.txt", Text = "malformed" },
                        new ComponentTextEntity { Name = "02-valid.txt", Text = "1:Alpha" },
                    ],
                }
            ),
            LoggerResolver,
            TestCompilationOptions
        );
        var textTable = textVm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            textTable,
            ("Name", typeof(string)),
            ("Key", typeof(string)),
            ("ParsedValues", typeof(long)),
            ("Sequence", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            textTable,
            ["01-invalid.txt", null, 0L, 1L],
            ["02-valid.txt", "1", 1L, 2L]
        );
    }

    [TestMethod]
    public void PartialInterpretation_WithCteProjectionAndWindows_ShouldRetainBinaryAndTextDiagnostics()
    {
        const string binaryQuery = (
            "binary Packet {\n"
            + "    Id: byte,\n"
            + "    Value: short le\n"
            + "};\n"
            + "with diagnostics as (\n"
            + "    select f.Name as Name,\n"
            + "           p.ParsedFields as ParsedFields,\n"
            + "           p.ErrorField as ErrorField,\n"
            + "           p.ErrorMessage as ErrorMessage,\n"
            + "           p.BytesConsumed as BytesConsumed\n"
            + "    from #test.files() f\n"
            + "    cross apply PartialInterpret<Packet>(f.Content) p\n"
            + ")\n"
            + "select d.Name, d.ParsedFields, d.ErrorField, d.ErrorMessage, d.BytesConsumed,\n"
            + "       RowNumber() over (order by d.Name) as Sequence\n"
            + "from diagnostics d\n"
            + "order by Sequence"
        );

        var binaryVm = CompileGeneratedQuery(
            binaryQuery,
            Guid.NewGuid().ToString(),
            new ComponentBinarySchemaProvider(
                new Dictionary<string, IEnumerable<ComponentBinaryEntity>>
                {
                    ["#test"] =
                    [
                        new ComponentBinaryEntity { Name = "01-valid.bin", Content = [1, 10, 0] },
                        new ComponentBinaryEntity { Name = "02-truncated.bin", Content = [2, 0x10] },
                    ],
                }
            ),
            LoggerResolver,
            TestCompilationOptions
        );
        var binaryTable = binaryVm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            binaryTable,
            ("Name", typeof(string)),
            ("ParsedFields", PartialPayloadAssertions.ColumnType(binaryTable)),
            ("ErrorField", typeof(string)),
            ("ErrorMessage", typeof(string)),
            ("BytesConsumed", typeof(int)),
            ("Sequence", typeof(long))
        );

        var binaryRows = TableMaterializationTestHelper.Materialize(binaryTable);
        Assert.AreEqual(2, binaryRows.Count);
        Assert.AreEqual("01-valid.bin", binaryRows[0].ReadValue(0));
        var binarySuccessFields = PartialPayloadAssertions.CompletedValues(binaryRows[0].ReadValue(1)!);
        Assert.AreEqual((byte)1, binarySuccessFields["Id"]);
        Assert.AreEqual((short)10, binarySuccessFields["Value"]);
        Assert.IsNull(binaryRows[0].ReadValue(2));
        Assert.IsNull(binaryRows[0].ReadValue(3));
        Assert.AreEqual(3, binaryRows[0].ReadValue(4));
        Assert.AreEqual(1L, binaryRows[0].ReadValue(5));

        Assert.AreEqual("02-truncated.bin", binaryRows[1].ReadValue(0));
        var binaryFailureFields = PartialPayloadAssertions.CompletedValues(binaryRows[1].ReadValue(1)!);
        Assert.AreEqual((byte)2, binaryFailureFields["Id"]);
        Assert.IsFalse(binaryFailureFields.ContainsKey("Value"));
        Assert.AreEqual("Value", binaryRows[1].ReadValue(2));
        StringAssert.Contains((string)binaryRows[1].ReadValue(3)!, "ISE0001");
        Assert.AreEqual(1, binaryRows[1].ReadValue(4));
        Assert.AreEqual(2L, binaryRows[1].ReadValue(5));

        const string textQuery = (
            "text Record {\n"
            + "    Id: until ':',\n"
            + "    Value: chars[3]\n"
            + "};\n"
            + "with diagnostics as (\n"
            + "    select f.Name as Name,\n"
            + "           p.ParsedFields as ParsedFields,\n"
            + "           p.ErrorField as ErrorField,\n"
            + "           p.ErrorMessage as ErrorMessage,\n"
            + "           p.BytesConsumed as BytesConsumed\n"
            + "    from #test.lines() f\n"
            + "    cross apply PartialParse<Record>(f.Text) p\n"
            + ")\n"
            + "select d.Name, d.ParsedFields, d.ErrorField, d.ErrorMessage, d.BytesConsumed,\n"
            + "       RowNumber() over (order by d.Name) as Sequence\n"
            + "from diagnostics d\n"
            + "order by Sequence"
        );

        var textVm = CompileGeneratedQuery(
            textQuery,
            Guid.NewGuid().ToString(),
            new ComponentTextSchemaProvider(
                new Dictionary<string, IEnumerable<ComponentTextEntity>>
                {
                    ["#test"] =
                    [
                        new ComponentTextEntity { Name = "01-valid.txt", Text = "1:abc" },
                        new ComponentTextEntity { Name = "02-truncated.txt", Text = "2:x" },
                    ],
                }
            ),
            LoggerResolver,
            TestCompilationOptions
        );
        var textTable = textVm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            textTable,
            ("Name", typeof(string)),
            ("ParsedFields", PartialPayloadAssertions.ColumnType(textTable)),
            ("ErrorField", typeof(string)),
            ("ErrorMessage", typeof(string)),
            ("BytesConsumed", typeof(int)),
            ("Sequence", typeof(long))
        );

        var textRows = TableMaterializationTestHelper.Materialize(textTable);
        Assert.AreEqual(2, textRows.Count);
        Assert.AreEqual("01-valid.txt", textRows[0].ReadValue(0));
        var textSuccessFields = PartialPayloadAssertions.CompletedValues(textRows[0].ReadValue(1)!);
        Assert.AreEqual("1", textSuccessFields["Id"]);
        Assert.AreEqual("abc", textSuccessFields["Value"]);
        Assert.IsNull(textRows[0].ReadValue(2));
        Assert.IsNull(textRows[0].ReadValue(3));
        Assert.AreEqual(5, textRows[0].ReadValue(4));
        Assert.AreEqual(1L, textRows[0].ReadValue(5));

        Assert.AreEqual("02-truncated.txt", textRows[1].ReadValue(0));
        var textFailureFields = PartialPayloadAssertions.CompletedValues(textRows[1].ReadValue(1)!);
        Assert.AreEqual("2", textFailureFields["Id"]);
        Assert.IsFalse(textFailureFields.ContainsKey("Value"));
        Assert.AreEqual("Value", textRows[1].ReadValue(2));
        StringAssert.Contains((string)textRows[1].ReadValue(3)!, "ISE0001");
        Assert.AreEqual(2, textRows[1].ReadValue(4));
        Assert.AreEqual(2L, textRows[1].ReadValue(5));
    }

    [TestMethod]
    public void CrossDomainInterpretation_ShouldReportUnsupportedSyntaxWithPreciseSpan()
    {
        const string query = (
            "binary Packet {\n"
            + "    Id: byte\n"
            + "};\n"
            + "text LabelRecord {\n"
            + "    Id: until ':'\n"
            + "};\n"
            + "select p.Id\n"
            + "from #test.files() f\n"
            + "cross apply Interpret<LabelRecord>(f.Content) p"
        );

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                new ComponentBinarySchemaProvider(
                    new Dictionary<string, IEnumerable<ComponentBinaryEntity>>
                    {
                        ["#test"] = [new ComponentBinaryEntity { Name = "row.bin", Content = [1] }],
                    }
                ),
                LoggerResolver,
                TestCompilationOptions
            )
        );

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticPhase.Parse,
            "binary interpretation schema"
        );
        var envelope = exception.PrimaryEnvelope;
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.IsNotNull(envelope.Offset);
        Assert.IsNotNull(envelope.Length);
        var argumentStart = query.IndexOf("(f.Content)", StringComparison.Ordinal);
        var expectedSpan = new TextSpan(argumentStart, "(f.Content)".Length);
        Assert.AreEqual(expectedSpan.Start, envelope.Offset);
        Assert.AreEqual(expectedSpan.Length, envelope.Length);
        Assert.AreEqual(expectedSpan.End, envelope.EndOffset);
        Assert.IsNotNull(envelope.Snippet);
        StringAssert.Contains(envelope.Snippet, "(f.Content)");
        StringAssert.Contains(envelope.Message, "binary interpretation schema");
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
    }
}
