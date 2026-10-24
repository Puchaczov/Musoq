using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualAdvancedFormatsTests
{
    #region Advanced Real-World Binary Format Tests

    [TestMethod]
    public void Query_SelectInterpret_PngLikeSignature_WithCheckConstraint_ShouldValidate()
    {
        // Arrange: PNG-like file signature validation (simplified)
        var query = @"
            binary PngSignature {
                B1: byte check B1 = 0x89,
                B2: byte check B2 = 0x50,
                B3: byte check B3 = 0x4E,
                B4: byte check B4 = 0x47
            };
            select
                s.B1,
                s.B2,
                s.B3,
                s.B4
            from #test.files() f
            cross apply Interpret<PngSignature>(f.Content) s";

        // PNG signature bytes: 0x89 P N G
        var testData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var entities = new[] { new BinaryEntity { Name = "test.png", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("B1", typeof(byte)),
            ("B2", typeof(byte)),
            ("B3", typeof(byte)),
            ("B4", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)0x89, (byte)0x50, (byte)0x4E, (byte)0x47]);
    }

    [TestMethod]
    public void Query_SelectInterpret_TlvProtocol_WithVariablePayload_ShouldParse()
    {
        // Arrange: TLV (Type-Length-Value) protocol format
        var query = @"
            binary TlvRecord {
                Type: byte,
                Length: short le,
                Value: byte[Length]
            };
            select
                t.Type,
                t.Length,
                t.Value
            from #test.files() f
            cross apply Interpret<TlvRecord>(f.Content) t";

        // TLV: Type=0x01, Length=5, Value=[0x10, 0x20, 0x30, 0x40, 0x50]
        var testData = new byte[] { 0x01, 0x05, 0x00, 0x10, 0x20, 0x30, 0x40, 0x50 };
        var entities = new[] { new BinaryEntity { Name = "tlv.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Type", typeof(byte)),
            ("Length", typeof(short)),
            ("Value", typeof(byte[])));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [(byte)0x01, (short)5, new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50 }]);
    }

    [TestMethod]
    public void Query_SelectInterpret_MessageFrameWithSync_ChecksumValidation_ShouldParse()
    {
        // Arrange: Protocol frame with sync word and checksum
        var query = @"
            binary MessageFrame {
                Sync: short le check Sync = 0x1234,
                MsgType: byte,
                PayloadLen: short le,
                Payload: byte[PayloadLen],
                Checksum: short le
            };
            select
                m.Sync,
                m.MsgType,
                m.PayloadLen,
                m.Payload,
                m.Checksum
            from #test.files() f
            cross apply Interpret<MessageFrame>(f.Content) m";

        // Frame: Sync=0x1234 (LE), MsgType=1, PayloadLen=3, Payload=[1,2,3], Checksum=0x0006
        var testData = new byte[] { 0x34, 0x12, 0x01, 0x03, 0x00, 0x01, 0x02, 0x03, 0x06, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "frame.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Sync", typeof(short)),
            ("MsgType", typeof(byte)),
            ("PayloadLen", typeof(short)),
            ("Payload", typeof(byte[])),
            ("Checksum", typeof(short)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [(short)0x1234, (byte)0x01, (short)3, new byte[] { 1, 2, 3 }, (short)6]);
    }

    [TestMethod]
    public void Query_SelectInterpret_ToySettings_WithComputedOptions_ShouldExtractBits()
    {
        const string query = """
            binary ToySettings {
                Revision: short le,
                Options: byte,
                UsesFirstValue: (Options & 0x04) <> 0,
                UsesSecondValue: (Options & 0x20) <> 0,
                UsesThirdValue: (Options & 0x80) <> 0,
                SampleCount: int le
            };
            select settings.Revision, settings.Options, settings.UsesFirstValue,
                settings.UsesSecondValue, settings.UsesThirdValue, settings.SampleCount
            from #test.files() input
            cross apply Interpret<ToySettings>(input.Content) settings
            """;
        var entities = new[] { new BinaryEntity { Name = "invented-options", Content = [3, 0, 0x24, 3, 0, 0, 0] } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });
        using var queryResult = CompileGeneratedQuery(
            query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver, TestCompilationOptions);
        using var table = queryResult.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Revision", typeof(short)),
            ("Options", typeof(byte)),
            ("UsesFirstValue", typeof(bool)),
            ("UsesSecondValue", typeof(bool)),
            ("UsesThirdValue", typeof(bool)),
            ("SampleCount", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(short)3, (byte)0x24, true, true, false, 3]);
    }

    [TestMethod]
    public void Query_SelectInterpret_NestedSchemaWithArrays_ShouldParseHierarchy()
    {
        // Arrange: Mesh-like structure with vertex arrays
        var query = @"
            binary Point {
                X: float le,
                Y: float le
            };

            binary Vertex {
                Position: Point,
                Color: byte
            };

            binary Mesh {
                VertexCount: int le,
                Vertices: Vertex[VertexCount]
            };
            select
                m.VertexCount,
                m.Vertices[0].Position.X,
                m.Vertices[0].Position.Y,
                m.Vertices[0].Color,
                m.Vertices[1].Position.X,
                m.Vertices[1].Position.Y,
                m.Vertices[1].Color
            from #test.files() f
            cross apply Interpret<Mesh>(f.Content) m";

        // Mesh: 2 vertices, each with Point (2 floats) + Color (1 byte)
        var testData = new byte[4 + 2 * (8 + 1)]; // VertexCount + 2 * (2*float + byte)
        var offset = 0;
        BitConverter.GetBytes(2).CopyTo(testData, offset);
        offset += 4;
        // Vertex 1: Point(1.0, 2.0), Color=255
        BitConverter.GetBytes(1.0f).CopyTo(testData, offset);
        offset += 4;
        BitConverter.GetBytes(2.0f).CopyTo(testData, offset);
        offset += 4;
        testData[offset++] = 255;
        // Vertex 2: Point(3.0, 4.0), Color=128
        BitConverter.GetBytes(3.0f).CopyTo(testData, offset);
        offset += 4;
        BitConverter.GetBytes(4.0f).CopyTo(testData, offset);
        offset += 4;
        testData[offset++] = 128;

        var entities = new[] { new BinaryEntity { Name = "mesh.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("VertexCount", typeof(int)),
            ("m.Vertices[0].Position.X", typeof(float?)),
            ("m.Vertices[0].Position.Y", typeof(float?)),
            ("m.Vertices[0].Color", typeof(byte?)),
            ("m.Vertices[1].Position.X", typeof(float?)),
            ("m.Vertices[1].Position.Y", typeof(float?)),
            ("m.Vertices[1].Color", typeof(byte?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [2, 1.0f, 2.0f, (byte)255, 3.0f, 4.0f, (byte)128]);
    }

    [TestMethod]
    public void Query_SelectInterpret_ConditionalRecordTypes_ShouldParseBasedOnType()
    {
        // Arrange: Records with different structures based on type field
        var query = @"
            binary NumericRecord {
                RecordType: byte,
                IntValue: int le when RecordType = 2
            };
            select
                r.RecordType,
                r.IntValue
            from #test.files() f
            cross apply Interpret<NumericRecord>(f.Content) r";

        // Record Type 2 (numeric): RecordType=2, IntValue=12345
        var testData = new byte[5];
        testData[0] = 2; // RecordType = 2
        BitConverter.GetBytes(12345).CopyTo(testData, 1);

        var entities = new[] { new BinaryEntity { Name = "record.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("RecordType", typeof(byte)),
            ("IntValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)2, 12345]);
    }

    [TestMethod]
    public void Query_SelectInterpret_MultipleFiles_WithSum_ShouldAggregate()
    {
        var query = @"
            binary DataRecord {
                Category: byte,
                Value: int le
            };
            select
                r.Category,
                Sum(r.Value) as TotalValue
            from #test.files() f
            cross apply Interpret<DataRecord>(f.Content) r
            group by r.Category
            order by r.Category";

        var entities = new[]
        {
            new BinaryEntity { Name = "file1.bin", Content = CreateDataRecord(1, 100) },
            new BinaryEntity { Name = "file2.bin", Content = CreateDataRecord(1, 200) },
            new BinaryEntity { Name = "file3.bin", Content = CreateDataRecord(2, 150) },
            new BinaryEntity { Name = "file4.bin", Content = CreateDataRecord(2, 250) },
            new BinaryEntity { Name = "file5.bin", Content = CreateDataRecord(1, 50) }
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });


        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Category", typeof(byte)),
            ("TotalValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [(byte)1, 350],
            [(byte)2, 400]);
    }

    private static byte[] CreateDataRecord(byte category, int value)
    {
        var data = new byte[5];
        data[0] = category;
        BitConverter.GetBytes(value).CopyTo(data, 1);
        return data;
    }

    #endregion
}
