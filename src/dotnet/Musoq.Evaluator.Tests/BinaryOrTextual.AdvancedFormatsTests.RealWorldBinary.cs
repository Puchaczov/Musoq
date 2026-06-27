using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

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
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x89, table[0][0]);
        Assert.AreEqual((byte)0x50, table[0][1]);
        Assert.AreEqual((byte)0x4E, table[0][2]);
        Assert.AreEqual((byte)0x47, table[0][3]);
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
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((short)5, table[0][1]);
        var valueBytes = (byte[])table[0][2];
        Assert.HasCount(5, valueBytes);
        Assert.AreEqual((byte)0x10, valueBytes[0]);
        Assert.AreEqual((byte)0x50, valueBytes[4]);
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
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)0x1234, table[0][0]);
        Assert.AreEqual((byte)0x01, table[0][1]);
        Assert.AreEqual((short)3, table[0][2]);
        var payload = (byte[])table[0][3];
        Assert.HasCount(3, payload);
        Assert.AreEqual((short)6, table[0][4]);
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
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)1, table[0][0]); // Version
        Assert.AreEqual((short)0x03, table[0][1]); // Flags
        Assert.IsTrue((bool?)table[0][2]); // IsCompressed
        Assert.IsTrue((bool?)table[0][3]); // HasIndex
        Assert.IsFalse((bool?)table[0][4]); // IsEncrypted
        Assert.AreEqual(100, table[0][5]); // RecordCount
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
                m.Vertices
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
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0][0]); // VertexCount
        var vertices = (Array)table[0][1];
        Assert.HasCount(2, vertices);
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
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)2, table[0][0]); // RecordType
        Assert.AreEqual(12345, table[0][1]); // IntValue
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


        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(350, table[0][1]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual(400, table[1][1]);
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
