using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
    [TestMethod]
    public void Query_InterpretAt_ValidOffset_ShouldPreserveFieldTypesAndValues()
    {
        var query = @"
            binary PositionedPacket {
                Magic: int le,
                Version: byte
            };
            select p.Magic, p.Version
            from #test.files() f
            cross apply InterpretAt<PositionedPacket>(f.Content, 3) p";

        var data = new byte[] { 0xAA, 0xBB, 0xCC, 0x78, 0x56, 0x34, 0x12, 0x09 };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                { "#test", [new BinaryEntity { Name = "positioned.bin", Content = data }] }
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
            ("p.Magic", typeof(int)),
            ("p.Version", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [0x12345678, (byte)9]);
    }

    [TestMethod]
    public void Query_InterpretAt_InvalidOffsets_ShouldExposeStructuredPositionErrors()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var negative = ExecuteInterpretAtWithOffset(-1, data);
        Assert.AreEqual(ParseErrorCode.InvalidPosition, negative.ErrorCode);
        Assert.AreEqual("PositionedPacket", negative.SchemaName);
        Assert.IsNull(negative.FieldName);
        Assert.AreEqual(-1, negative.Position);
        StringAssert.Contains(negative.Details, "negative");

        var pastEnd = ExecuteInterpretAtWithOffset(data.Length + 1, data);
        Assert.AreEqual(ParseErrorCode.InvalidPosition, pastEnd.ErrorCode);
        Assert.AreEqual("PositionedPacket", pastEnd.SchemaName);
        Assert.IsNull(pastEnd.FieldName);
        Assert.AreEqual(data.Length + 1, pastEnd.Position);
        StringAssert.Contains(pastEnd.Details, "past the end");
    }

    [TestMethod]
    public void Query_PartialInterpret_Failure_ShouldRetainFieldsBeforeFailure()
    {
        var query = @"
            binary DebugPacket {
                Magic: int le,
                Version: byte
            };
            select
                f.Name,
                p.ParsedFields,
                p.ErrorField,
                p.ErrorMessage,
                p.BytesConsumed
            from #test.files() f
            cross apply PartialInterpret<DebugPacket>(f.Content) p
            order by f.Name";

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                {
                    "#test",
                    [
                        new BinaryEntity
                        {
                            Name = "01-valid.bin",
                            Content = [0x78, 0x56, 0x34, 0x12, 0x07]
                        },
                        new BinaryEntity
                        {
                            Name = "02-truncated.bin",
                            Content = [0x78, 0x56, 0x34, 0x12]
                        }
                    ]
                }
            });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var successfulFields = (Dictionary<string, object?>)table[0][1]!;
        Assert.AreEqual(0x12345678, successfulFields["Magic"]);
        Assert.AreEqual((byte)7, successfulFields["Version"]);
        Assert.IsNull(table[0][2]);
        Assert.IsNull(table[0][3]);
        Assert.AreEqual(5, table[0][4]);

        var failedFields = (Dictionary<string, object?>)table[1][1]!;
        Assert.AreEqual(0x12345678, failedFields["Magic"]);
        Assert.IsFalse(failedFields.ContainsKey("Version"));
        Assert.AreEqual("Version", table[1][2]);
        StringAssert.Contains((string)table[1][3]!, "ISE0001");
        Assert.AreEqual(4, table[1][4]);
    }

    [TestMethod]
    public void Query_PartialParse_Failure_ShouldRetainFieldsBeforeFailure()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: chars[5]
            };
            select
                f.Name,
                p.ParsedFields,
                p.ErrorField,
                p.ErrorMessage,
                p.BytesConsumed
            from #test.lines() f
            cross apply PartialParse<KeyValue>(f.Text) p
            order by f.Name";

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>>
            {
                {
                    "#test",
                    [
                        new TextEntity { Name = "01-valid.txt", Text = "host=abcde" },
                        new TextEntity { Name = "02-truncated.txt", Text = "host=" }
                    ]
                }
            });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var successfulFields = (Dictionary<string, object?>)table[0][1]!;
        Assert.AreEqual("host", successfulFields["Key"]);
        Assert.AreEqual("abcde", successfulFields["Value"]);
        Assert.IsNull(table[0][2]);
        Assert.IsNull(table[0][3]);
        Assert.AreEqual("host=abcde".Length, table[0][4]);

        var failedFields = (Dictionary<string, object?>)table[1][1]!;
        Assert.AreEqual("host", failedFields["Key"]);
        Assert.IsFalse(failedFields.ContainsKey("Value"));
        Assert.AreEqual("Value", table[1][2]);
        StringAssert.Contains((string)table[1][3]!, "ISE0001");
        Assert.AreEqual("host=".Length, table[1][4]);
    }

    [TestMethod]
    public void Query_PartialInterpret_NestedFailure_ShouldReportQualifiedFieldPath()
    {
        var query = @"
            binary InnerPacket {
                Value: int le
            };
            binary OuterPacket {
                Header: byte,
                Payload: InnerPacket,
                Tail: byte
            };
            select p.ParsedFields, p.ErrorField, p.ErrorMessage, p.BytesConsumed
            from #test.files() f
            cross apply PartialInterpret<OuterPacket>(f.Content) p";

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                { "#test", [new BinaryEntity { Name = "nested.bin", Content = [0x07, 0x2A] }] }
            });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        var parsedFields = (Dictionary<string, object?>)table[0][0]!;
        Assert.AreEqual((byte)7, parsedFields["Header"]);
        Assert.IsFalse(parsedFields.ContainsKey("Payload"));
        Assert.AreEqual("Payload.Value", table[0][1]);
        StringAssert.Contains((string)table[0][2]!, "ISE0001");
        StringAssert.Contains((string)table[0][2]!, "Payload.Value");
        Assert.AreEqual(1, table[0][3]);
    }

    [TestMethod]
    public void Query_PartialParse_NestedFailure_ShouldReportQualifiedFieldPath()
    {
        var query = @"
            text InnerText {
                Key: until ':',
                Value: chars[3]
            };
            text OuterText {
                Prefix: until '|',
                Payload: InnerText,
                Tail: rest
            };
            select p.ParsedFields, p.ErrorField, p.ErrorMessage, p.BytesConsumed
            from #test.lines() f
            cross apply PartialParse<OuterText>(f.Text) p";

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>>
            {
                { "#test", [new TextEntity { Name = "nested.txt", Text = "root|k:ab" }] }
            });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        var parsedFields = (Dictionary<string, object?>)table[0][0]!;
        Assert.AreEqual("root", parsedFields["Prefix"]);
        Assert.IsFalse(parsedFields.ContainsKey("Payload"));
        Assert.AreEqual("Payload.Value", table[0][1]);
        StringAssert.Contains((string)table[0][2]!, "ISE0001");
        StringAssert.Contains((string)table[0][2]!, "Payload.Value");
        Assert.AreEqual(7, table[0][3]);
    }

    private ParseException ExecuteInterpretAtWithOffset(int offset, byte[] data)
    {
        var query = $@"
            binary PositionedPacket {{
                Value: int le
            }};
            select p.Value
            from #test.files() f
            cross apply InterpretAt<PositionedPacket>(f.Content, {offset}) p";
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                { "#test", [new BinaryEntity { Name = "invalid-offset.bin", Content = data }] }
            });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        return Assert.ThrowsExactly<ParseException>(() =>
        {
            var table = vm.Run(CancellationToken.None);
            _ = table.Count;
        });
    }
}
