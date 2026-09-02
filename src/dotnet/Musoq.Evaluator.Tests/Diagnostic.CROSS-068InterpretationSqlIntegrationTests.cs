using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using ComponentBinaryEntity = Musoq.Evaluator.Tests.Components.BinaryEntity;
using ComponentBinarySchemaProvider = Musoq.Evaluator.Tests.Components.BinarySchemaProvider;
using ComponentTextEntity = Musoq.Evaluator.Tests.Components.TextEntity;
using ComponentTextSchemaProvider = Musoq.Evaluator.Tests.Components.TextSchemaProvider;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
    [TestMethod]
    public void InterpretationSchemas_WithTableCoupleCtesSetsAndWindows_ShouldComposeSqlPipeline()
    {
        const string query = """
            table TextSourceRow { Name: string, Text: string };
            couple #txt.lines with table TextSourceRow as Lines;
            binary Packet {
                Id: byte,
                Score: short le
            };
            text LabelRecord {
                Id: until ':',
                Label: rest trim
            };
            with binaryRows as (
                select ToString(p.Id) as BinaryId, p.Score as Score
                from #bin.files() f
                cross apply Interpret<Packet>(f.Content) p
            ),
            textRows as (
                select p.Id as TextId, p.Label as Label
                from Lines() l
                cross apply Parse<LabelRecord>(l.Text) p
                where p.Label <> ''
            ),
            combined as (
                select t.Label as Label, ToInt32(b.Score) as Score
                from binaryRows b
                inner join textRows t on b.BinaryId = t.TextId
                union all (Label, Score)
                select 'Alpha' as Label, ToInt32(0::Short) as Score
                from values { { Marker: 1 } } marker
            )
            select c.Label,
                   Sum(c.Score) as TotalScore,
                   Count(c.Label) as Records,
                   RowNumber() over (order by Sum(c.Score) desc) as Rank
            from combined c
            group by c.Label
            having Sum(c.Score) > 0
            qualify RowNumber() over (order by Sum(c.Score) desc) <= 2
            order by TotalScore desc
            """;

        var schemaProvider = new MixedSchemaProvider(
            new Dictionary<string, IEnumerable<ComponentBinaryEntity>>
            {
                ["#bin"] =
                [
                    new ComponentBinaryEntity { Name = "1.bin", Content = [1, 100, 0] },
                    new ComponentBinaryEntity { Name = "2.bin", Content = [2, 200, 0] },
                    new ComponentBinaryEntity { Name = "3.bin", Content = [3, 50, 0] }
                ]
            },
            new Dictionary<string, IEnumerable<ComponentTextEntity>>
            {
                ["#txt"] =
                [
                    new ComponentTextEntity { Name = "1.txt", Text = "1:Alpha" },
                    new ComponentTextEntity { Name = "2.txt", Text = "2:Alpha" },
                    new ComponentTextEntity { Name = "3.txt", Text = "3:Beta" }
                ]
            });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.Label", typeof(string)),
            ("TotalScore", typeof(int?)),
            ("Records", typeof(long)),
            ("Rank", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alpha", 300, 3L, 1L],
            ["Beta", 50, 1L, 2L]);
    }

    [TestMethod]
    public void SafeInterpretation_WithOuterApplyCtesAndAggregate_ShouldPreserveFailedRows()
    {
        const string binaryQuery = """
            binary Packet {
                Id: byte,
                Value: short le
            };
            text LabelRecord {
                Id: until ':',
                Value: rest trim
            };
            with binarySafe as (
                select f.Name as Name, ToString(p.Id) as Key, ToString(p.Value) as Value
                from #bin.files() f
                outer apply TryInterpret<Packet>(f.Content) p
            )
            select r.Name, r.Key, Count(r.Value) as ParsedValues,
                   RowNumber() over (order by r.Name) as Sequence
            from binarySafe r
            group by r.Name, r.Key
            order by Sequence
            """;

        var binaryVm = CompileGeneratedQuery(
            binaryQuery,
            Guid.NewGuid().ToString(),
            new ComponentBinarySchemaProvider(
                new Dictionary<string, IEnumerable<ComponentBinaryEntity>>
                {
                    ["#bin"] =
                    [
                        new ComponentBinaryEntity { Name = "01-invalid.bin", Content = [1] },
                        new ComponentBinaryEntity { Name = "02-valid.bin", Content = [1, 10, 0] }
                    ]
                }),
            LoggerResolver,
            TestCompilationOptions);
        var binaryTable = binaryVm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            binaryTable,
            ("r.Name", typeof(string)),
            ("r.Key", typeof(string)),
            ("ParsedValues", typeof(long)),
            ("Sequence", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            binaryTable,
            ["01-invalid.bin", null, 0L, 1L],
            ["02-valid.bin", "1", 1L, 2L]);

        const string textQuery = """
            text LabelRecord {
                Id: until ':',
                Value: rest trim
            };
            with textSafe as (
                select f.Name as Name, p.Id as Key, p.Value as Value
                from #txt.files() f
                outer apply TryParse<LabelRecord>(f.Text) p
            )
            select r.Name, r.Key, Count(r.Value) as ParsedValues,
                   RowNumber() over (order by r.Name) as Sequence
            from textSafe r
            group by r.Name, r.Key
            order by Sequence
            """;

        var textVm = CompileGeneratedQuery(
            textQuery,
            Guid.NewGuid().ToString(),
            new ComponentTextSchemaProvider(
                new Dictionary<string, IEnumerable<ComponentTextEntity>>
                {
                    ["#txt"] =
                    [
                        new ComponentTextEntity { Name = "01-invalid.txt", Text = "malformed" },
                        new ComponentTextEntity { Name = "02-valid.txt", Text = "1:Alpha" }
                    ]
                }),
            LoggerResolver,
            TestCompilationOptions);
        var textTable = textVm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            textTable,
            ("r.Name", typeof(string)),
            ("r.Key", typeof(string)),
            ("ParsedValues", typeof(long)),
            ("Sequence", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            textTable,
            ["01-invalid.txt", null, 0L, 1L],
            ["02-valid.txt", "1", 1L, 2L]);
    }

    [TestMethod]
    public void PartialInterpretation_WithCteProjectionAndWindows_ShouldRetainBinaryAndTextDiagnostics()
    {
        const string binaryQuery = """
            binary Packet {
                Id: byte,
                Value: short le
            };
            with diagnostics as (
                select f.Name as Name,
                       p.ParsedFields as ParsedFields,
                       p.ErrorField as ErrorField,
                       p.ErrorMessage as ErrorMessage,
                       p.BytesConsumed as BytesConsumed
                from #test.files() f
                cross apply PartialInterpret<Packet>(f.Content) p
            )
            select d.Name, d.ParsedFields, d.ErrorField, d.ErrorMessage, d.BytesConsumed,
                   RowNumber() over (order by d.Name) as Sequence
            from diagnostics d
            order by Sequence
            """;

        var binaryVm = CompileGeneratedQuery(
            binaryQuery,
            Guid.NewGuid().ToString(),
            new ComponentBinarySchemaProvider(
                new Dictionary<string, IEnumerable<ComponentBinaryEntity>>
                {
                    ["#test"] =
                    [
                        new ComponentBinaryEntity { Name = "01-valid.bin", Content = [1, 10, 0] },
                        new ComponentBinaryEntity { Name = "02-truncated.bin", Content = [2, 0x10] }
                    ]
                }),
            LoggerResolver,
            TestCompilationOptions);
        var binaryTable = binaryVm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            binaryTable,
            ("d.Name", typeof(string)),
            ("d.ParsedFields", typeof(Dictionary<string, object?>)),
            ("d.ErrorField", typeof(string)),
            ("d.ErrorMessage", typeof(string)),
            ("d.BytesConsumed", typeof(int)),
            ("Sequence", typeof(long)));

        var binaryRows = TableMaterializationTestHelper.Materialize(binaryTable);
        Assert.AreEqual(2, binaryRows.Count);
        Assert.AreEqual("01-valid.bin", binaryRows[0][0]);
        var binarySuccessFields = (Dictionary<string, object?>)binaryRows[0][1]!;
        Assert.AreEqual((byte)1, binarySuccessFields["Id"]);
        Assert.AreEqual((short)10, binarySuccessFields["Value"]);
        Assert.IsNull(binaryRows[0][2]);
        Assert.IsNull(binaryRows[0][3]);
        Assert.AreEqual(3, binaryRows[0][4]);
        Assert.AreEqual(1L, binaryRows[0][5]);

        Assert.AreEqual("02-truncated.bin", binaryRows[1][0]);
        var binaryFailureFields = (Dictionary<string, object?>)binaryRows[1][1]!;
        Assert.AreEqual((byte)2, binaryFailureFields["Id"]);
        Assert.IsFalse(binaryFailureFields.ContainsKey("Value"));
        Assert.AreEqual("Value", binaryRows[1][2]);
        StringAssert.Contains((string)binaryRows[1][3]!, "ISE0001");
        Assert.AreEqual(1, binaryRows[1][4]);
        Assert.AreEqual(2L, binaryRows[1][5]);

        const string textQuery = """
            text Record {
                Id: until ':',
                Value: chars[3]
            };
            with diagnostics as (
                select f.Name as Name,
                       p.ParsedFields as ParsedFields,
                       p.ErrorField as ErrorField,
                       p.ErrorMessage as ErrorMessage,
                       p.BytesConsumed as BytesConsumed
                from #test.lines() f
                cross apply PartialParse<Record>(f.Text) p
            )
            select d.Name, d.ParsedFields, d.ErrorField, d.ErrorMessage, d.BytesConsumed,
                   RowNumber() over (order by d.Name) as Sequence
            from diagnostics d
            order by Sequence
            """;

        var textVm = CompileGeneratedQuery(
            textQuery,
            Guid.NewGuid().ToString(),
            new ComponentTextSchemaProvider(
                new Dictionary<string, IEnumerable<ComponentTextEntity>>
                {
                    ["#test"] =
                    [
                        new ComponentTextEntity { Name = "01-valid.txt", Text = "1:abc" },
                        new ComponentTextEntity { Name = "02-truncated.txt", Text = "2:x" }
                    ]
                }),
            LoggerResolver,
            TestCompilationOptions);
        var textTable = textVm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            textTable,
            ("d.Name", typeof(string)),
            ("d.ParsedFields", typeof(Dictionary<string, object?>)),
            ("d.ErrorField", typeof(string)),
            ("d.ErrorMessage", typeof(string)),
            ("d.BytesConsumed", typeof(int)),
            ("Sequence", typeof(long)));

        var textRows = TableMaterializationTestHelper.Materialize(textTable);
        Assert.AreEqual(2, textRows.Count);
        Assert.AreEqual("01-valid.txt", textRows[0][0]);
        var textSuccessFields = (Dictionary<string, object?>)textRows[0][1]!;
        Assert.AreEqual("1", textSuccessFields["Id"]);
        Assert.AreEqual("abc", textSuccessFields["Value"]);
        Assert.IsNull(textRows[0][2]);
        Assert.IsNull(textRows[0][3]);
        Assert.AreEqual(5, textRows[0][4]);
        Assert.AreEqual(1L, textRows[0][5]);

        Assert.AreEqual("02-truncated.txt", textRows[1][0]);
        var textFailureFields = (Dictionary<string, object?>)textRows[1][1]!;
        Assert.AreEqual("2", textFailureFields["Id"]);
        Assert.IsFalse(textFailureFields.ContainsKey("Value"));
        Assert.AreEqual("Value", textRows[1][2]);
        StringAssert.Contains((string)textRows[1][3]!, "ISE0001");
        Assert.AreEqual(2, textRows[1][4]);
        Assert.AreEqual(2L, textRows[1][5]);
    }

    [TestMethod]
    public void CrossDomainInterpretation_ShouldReportUnsupportedSyntaxWithPreciseSpan()
    {
        const string query = """
            binary Packet {
                Id: byte
            };
            text LabelRecord {
                Id: until ':'
            };
            select p.Id
            from #test.files() f
            cross apply Interpret<LabelRecord>(f.Content) p
            """;

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                new ComponentBinarySchemaProvider(
                    new Dictionary<string, IEnumerable<ComponentBinaryEntity>>
                    {
                        ["#test"] =
                        [new ComponentBinaryEntity { Name = "row.bin", Content = [1] }]
                    }),
                LoggerResolver,
                TestCompilationOptions));

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticPhase.Parse,
            "binary interpretation schema");
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
