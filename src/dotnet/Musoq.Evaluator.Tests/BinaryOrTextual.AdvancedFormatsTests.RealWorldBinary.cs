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
            ("s.B1", typeof(byte)),
            ("s.B2", typeof(byte)),
            ("s.B3", typeof(byte)),
            ("s.B4", typeof(byte)));
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
            ("t.Type", typeof(byte)),
            ("t.Length", typeof(short)),
            ("t.Value", typeof(byte[])));
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
            ("m.Sync", typeof(short)),
            ("m.MsgType", typeof(byte)),
            ("m.PayloadLen", typeof(short)),
            ("m.Payload", typeof(byte[])),
            ("m.Checksum", typeof(short)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [(short)0x1234, (byte)0x01, (short)3, new byte[] { 1, 2, 3 }, (short)6]);
    }

    [TestMethod]
    public void Query_SelectInterpret_StorageHeader_WithComputedFlags_ShouldExtractBits()
    {
        // Arrange: Storage header with computed boolean flags from bit fields
        var query = @"
            binary StorageHeader {
                Magic: int le check Magic = 0x53544F52,
                Version: short le,
                Flags: short le,

                IsCompressed: (Flags & 0x01) <> 0,
                HasIndex: (Flags & 0x02) <> 0,
                IsEncrypted: (Flags & 0x04) <> 0,

                RecordCount: int le
            };
            select
                h.Version,
                h.Flags,
                h.IsCompressed,
                h.HasIndex,
                h.IsEncrypted,
                h.RecordCount
            from #test.files() f
            cross apply Interpret<StorageHeader>(f.Content) h";

        // Header: Magic='STOR', Version=1, Flags=0x03 (compressed+indexed), RecordCount=100
        var testData = new byte[14];
        BitConverter.GetBytes(0x53544F52).CopyTo(testData, 0); // Magic
        BitConverter.GetBytes((short)1).CopyTo(testData, 4); // Version
        BitConverter.GetBytes((short)0x03).CopyTo(testData, 6); // Flags (compressed + has index)
        BitConverter.GetBytes(100).CopyTo(testData, 8); // RecordCount (wrong offset, fixing)

        testData = new byte[14];
        var offset = 0;
        BitConverter.GetBytes(0x53544F52).CopyTo(testData, offset);
        offset += 4;
        BitConverter.GetBytes((short)1).CopyTo(testData, offset);
        offset += 2;
        BitConverter.GetBytes((short)0x03).CopyTo(testData, offset);
        offset += 2;
        BitConverter.GetBytes(100).CopyTo(testData, offset);

        var entities = new[] { new BinaryEntity { Name = "storage.dat", Content = testData } };

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
            ("h.Version", typeof(short)),
            ("h.Flags", typeof(short)),
            ("h.IsCompressed", typeof(bool)),
            ("h.HasIndex", typeof(bool)),
            ("h.IsEncrypted", typeof(bool)),
            ("h.RecordCount", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(short)1, (short)0x03, true, true, false, 100]);
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
            ("m.VertexCount", typeof(int)),
            ("m.Vertices[0].Position.X", typeof(float)),
            ("m.Vertices[0].Position.Y", typeof(float)),
            ("m.Vertices[0].Color", typeof(byte)),
            ("m.Vertices[1].Position.X", typeof(float)),
            ("m.Vertices[1].Position.Y", typeof(float)),
            ("m.Vertices[1].Color", typeof(byte)));
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
            ("r.RecordType", typeof(byte)),
            ("r.IntValue", typeof(int?)));
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
            ("r.Category", typeof(byte)),
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
