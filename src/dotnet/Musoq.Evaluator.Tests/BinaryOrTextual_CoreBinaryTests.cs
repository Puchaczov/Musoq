#nullable enable annotations

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryOrTextual_CoreBinaryTests : BinaryOrTextualEvaluatorTestBase
{
    #region Mixed Binary-Text Composition Tests

    [TestMethod]
    public void Query_SelectInterpret_BinaryWithTextPayload_AsClause_ShouldChainParsing()
    {
        // Arrange: Binary container with embedded text that gets parsed
        // Note: 'until' consumes the delimiter, so no 'literal' needed after
        var query = @"
            text KeyValue {
                Key: until ':',
                Value: rest trim
            };
            binary ConfigPacket {
                Version: byte,
                Config: string[20] utf8 as KeyValue,
                Checksum: byte
            };
            select
                p.Version,
                p.Config.Key,
                p.Config.Value,
                p.Checksum
            from #test.files() f
            cross apply Interpret(f.Content, 'ConfigPacket') p";

        // Build packet: Version=1, Config="host:localhost      " (20 bytes), Checksum=0xFF
        var testData = new byte[22];
        testData[0] = 1;
        var configText = "host:localhost".PadRight(20);
        Encoding.UTF8.GetBytes(configText).CopyTo(testData, 1);
        testData[21] = 0xFF;

        var entities = new[] { new BinaryEntity { Name = "config.bin", Content = testData } };

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
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual("host", table[0][1]);
        Assert.AreEqual("localhost", table[0][2]);
        Assert.AreEqual((byte)0xFF, table[0][3]);
    }

    #endregion

    #region Binary Schema Query Tests

    [TestMethod]
    public void Query_SelectInterpret_WithBinarySchema_ShouldParseData()
    {
        // Arrange
        var query = @"
            binary HeaderFormat {
                Magic: int le,
                Version: short le
            };
            select
                h.Magic,
                h.Version
            from #test.files() f
            cross apply Interpret(f.Content, 'HeaderFormat') h";

        // Create test data: Magic=0x12345678, Version=0x0100
        var testData = new byte[] { 0x78, 0x56, 0x34, 0x12, 0x00, 0x01 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

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
        Assert.AreEqual(0x12345678, table[0][0]);
        Assert.AreEqual((short)0x0100, table[0][1]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMultipleRows_ShouldParseAllRows()
    {
        // Arrange
        var query = @"
            binary SimpleInt {
                Value: int le
            };
            select
                f.Name,
                h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'SimpleInt') h
            order by f.Name asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "file1.bin", Content = BitConverter.GetBytes(100) },
            new BinaryEntity { Name = "file2.bin", Content = BitConverter.GetBytes(200) },
            new BinaryEntity { Name = "file3.bin", Content = BitConverter.GetBytes(300) }
        };

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
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("file1.bin", table[0][0]);
        Assert.AreEqual(100, table[0][1]);
        Assert.AreEqual("file2.bin", table[1][0]);
        Assert.AreEqual(200, table[1][1]);
        Assert.AreEqual("file3.bin", table[2][0]);
        Assert.AreEqual(300, table[2][1]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithFilter_ShouldApplyWhere()
    {
        // Arrange
        var query = @"
            binary FlagData {
                Flags: byte
            };
            select
                f.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'FlagData') h
            where h.Flags = 1
            order by f.Name asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "enabled.bin", Content = [0x01] },
            new BinaryEntity { Name = "disabled.bin", Content = [0x00] },
            new BinaryEntity { Name = "also_enabled.bin", Content = [0x01] }
        };

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
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("also_enabled.bin", table[0][0]); // 'a' comes before 'e'
        Assert.AreEqual("enabled.bin", table[1][0]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithByteArray_ShouldParseFixedSizeBytes()
    {
        // Arrange
        var query = @"
            binary Packet {
                Header: byte[4],
                Data: byte[2]
            };
            select
                h.Header,
                h.Data
            from #test.files() f
            cross apply Interpret(f.Content, 'Packet') h";

        // Test data: 4-byte header + 2-byte data
        var testData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x12, 0x34 };
        var entities = new[] { new BinaryEntity { Name = "packet.bin", Content = testData } };

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
        var header = (byte[])table[0][0];
        var data = (byte[])table[0][1];
        Assert.HasCount(4, header);
        Assert.HasCount(2, data);
        Assert.AreEqual((byte)0xDE, header[0]);
        Assert.AreEqual((byte)0xAD, header[1]);
        Assert.AreEqual((byte)0xBE, header[2]);
        Assert.AreEqual((byte)0xEF, header[3]);
        Assert.AreEqual((byte)0x12, data[0]);
        Assert.AreEqual((byte)0x34, data[1]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithString_ShouldParseUtf8String()
    {
        // Arrange
        var query = @"
            binary NameRecord {
                Name: string[5] utf8
            };
            select
                h.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'NameRecord') h";

        var testData = Encoding.UTF8.GetBytes("Hello");
        var entities = new[] { new BinaryEntity { Name = "name.bin", Content = testData } };

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
        Assert.AreEqual("Hello", table[0][0]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithStringTrim_ShouldTrimWhitespace()
    {
        // Arrange
        var query = @"
            binary PaddedRecord {
                Value: string[10] utf8 trim
            };
            select
                h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'PaddedRecord') h";

        // "Hello" followed by spaces to make 10 bytes
        var testData = Encoding.UTF8.GetBytes("Hello     ");
        var entities = new[] { new BinaryEntity { Name = "padded.bin", Content = testData } };

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
        Assert.AreEqual("Hello", table[0][0]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithDynamicByteArray_ShouldUsePreviousField()
    {
        // Arrange
        var query = @"
            binary LengthPrefixedData {
                Length: short le,
                Data: byte[Length]
            };
            select
                h.Length,
                h.Data
            from #test.files() f
            cross apply Interpret(f.Content, 'LengthPrefixedData') h";

        // Length=4 (little-endian), followed by 4 bytes of data
        var testData = new byte[] { 0x04, 0x00, 0xAA, 0xBB, 0xCC, 0xDD };
        var entities = new[] { new BinaryEntity { Name = "prefixed.bin", Content = testData } };

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
        Assert.AreEqual((short)4, table[0][0]);
        var data = (byte[])table[0][1];
        Assert.HasCount(4, data);
        Assert.AreEqual((byte)0xAA, data[0]);
        Assert.AreEqual((byte)0xBB, data[1]);
        Assert.AreEqual((byte)0xCC, data[2]);
        Assert.AreEqual((byte)0xDD, data[3]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithDynamicString_ShouldUsePreviousField()
    {
        // Arrange
        var query = @"
            binary StringRecord {
                NameLen: byte,
                Name: string[NameLen] utf8
            };
            select
                h.NameLen,
                h.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'StringRecord') h";

        // NameLen=5, followed by "Hello"
        var testData = new byte[] { 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var entities = new[] { new BinaryEntity { Name = "name.bin", Content = testData } };

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
        Assert.AreEqual((byte)5, table[0][0]);
        Assert.AreEqual("Hello", table[0][1]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithNullTermString_ShouldStopAtNull()
    {
        // Arrange
        var query = @"
            binary CStringRecord {
                Name: string[10] utf8 nullterm
            };
            select
                h.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'CStringRecord') h";

        // "Hello" followed by null and garbage bytes
        var testData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF };
        var entities = new[] { new BinaryEntity { Name = "cstring.bin", Content = testData } };

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
        Assert.AreEqual("Hello", table[0][0]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithAsciiString_ShouldParseCorrectly()
    {
        // Arrange
        var query = @"
            binary AsciiRecord {
                Label: string[4] ascii
            };
            select
                h.Label
            from #test.files() f
            cross apply Interpret(f.Content, 'AsciiRecord') h";

        var testData = Encoding.ASCII.GetBytes("TEST");
        var entities = new[] { new BinaryEntity { Name = "ascii.bin", Content = testData } };

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
        Assert.AreEqual("TEST", table[0][0]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithNestedSchema_ShouldParseNestedFields()
    {
        // Arrange: Vertex contains nested Point with deep property access (v.Position.X)
        var query = @"
            binary Point {
                X: float le,
                Y: float le
            };
            binary Vertex {
                Id: int le,
                Position: Point
            };
            select
                v.Id,
                v.Position.X,
                v.Position.Y
            from #test.files() f
            cross apply Interpret(f.Content, 'Vertex') v";

        // Test data: Id (4 bytes) + Position.X (4 bytes) + Position.Y (4 bytes)
        using var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(42)); // Id = 42
        ms.Write(BitConverter.GetBytes(1.5f)); // Position.X = 1.5
        ms.Write(BitConverter.GetBytes(2.5f)); // Position.Y = 2.5
        var testData = ms.ToArray();

        var entities = new[] { new BinaryEntity { Name = "vertex.bin", Content = testData } };

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
        Assert.AreEqual(42, table[0][0]); // Id
        Assert.AreEqual(1.5f, (float)table[0][1], 0.001f); // Position.X
        Assert.AreEqual(2.5f, (float)table[0][2], 0.001f); // Position.Y
    }

    [TestMethod]
    public void Query_SelectInterpret_WithSchemaArray_ShouldParseArrayOfSchemas()
    {
        // Arrange: Mesh contains array of Points
        var query = @"
            binary Point {
                X: float le,
                Y: float le
            };
            binary Mesh {
                VertexCount: int le,
                Vertices: Point[VertexCount]
            };
            select
                m.VertexCount,
                m.Vertices
            from #test.files() f
            cross apply Interpret(f.Content, 'Mesh') m";

        // Test data: VertexCount = 2, followed by 2 Points
        using var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(2)); // VertexCount = 2
        ms.Write(BitConverter.GetBytes(1.0f)); // Vertices[0].X
        ms.Write(BitConverter.GetBytes(2.0f)); // Vertices[0].Y
        ms.Write(BitConverter.GetBytes(3.0f)); // Vertices[1].X
        ms.Write(BitConverter.GetBytes(4.0f)); // Vertices[1].Y
        var testData = ms.ToArray();

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

        var vertices = table[0][1] as object[];
        Assert.IsNotNull(vertices);
        Assert.HasCount(2, vertices);
    }

    #endregion

    #region Bit Fields E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithBitFields_ShouldParseBits()
    {
        // Arrange: Parse TCP-like header with bit fields
        var query = @"
            binary TcpFlags {
                Reserved: bits[4],
                DataOffset: bits[4],
                FIN: bits[1],
                SYN: bits[1],
                RST: bits[1],
                PSH: bits[1],
                ACK: bits[1],
                URG: bits[1],
                ECE: bits[1],
                CWR: bits[1]
            };
            select
                f.DataOffset,
                f.SYN,
                f.ACK
            from #test.files() fl
            cross apply Interpret(fl.Content, 'TcpFlags') f";

        // Byte 0: Reserved=0, DataOffset=5 -> 0x50
        // Byte 1: Flags CWR=0,ECE=0,URG=0,ACK=1,PSH=0,RST=0,SYN=1,FIN=0 -> 0b00010010 = 0x12
        var testData = new byte[] { 0x50, 0x12 };
        var entities = new[] { new BinaryEntity { Name = "tcp.bin", Content = testData } };

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
        Assert.AreEqual((byte)5, table[0][0]); // DataOffset
        Assert.AreEqual((byte)1, table[0][1]); // SYN flag is set
        Assert.AreEqual((byte)1, table[0][2]); // ACK flag is set
    }

    [TestMethod]
    public void Query_SelectInterpret_WithBitFieldsAndAlignment_ShouldAlignToByte()
    {
        // Arrange: Parse structure with bit fields followed by alignment
        var query = @"
            binary PackedHeader {
                Version: bits[4],
                Type: bits[4],
                Flags: bits[3],
                Reserved: align[8],
                Length: int le
            };
            select
                h.Version,
                h.Type,
                h.Flags,
                h.Length
            from #test.files() f
            cross apply Interpret(f.Content, 'PackedHeader') h";

        // Bits are read LSB-first:
        // Byte 0 = 0x21: bits[0-3]=1 (Version), bits[4-7]=2 (Type)
        // Byte 1 = 0x05: bits[0-2]=5 (Flags), align[8] skips remaining bits
        // Bytes 2-5: Length = 0x12345678 (little-endian)
        var testData = new byte[] { 0x21, 0x05, 0x78, 0x56, 0x34, 0x12 };
        var entities = new[] { new BinaryEntity { Name = "packed.bin", Content = testData } };

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
        Assert.AreEqual((byte)1, table[0][0]); // Version
        Assert.AreEqual((byte)2, table[0][1]); // Type
        Assert.AreEqual((byte)5, table[0][2]); // Flags
        Assert.AreEqual(0x12345678, table[0][3]); // Length
    }

    [TestMethod]
    public void Query_SelectInterpret_WithBitFieldsFiltered_ShouldFilterByBitValue()
    {
        // Arrange: Filter files by bit flag values
        var query = @"
            binary StatusByte {
                Active: bits[1],
                Ready: bits[1],
                Error: bits[1],
                Reserved: bits[5]
            };
            select
                f.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'StatusByte') s
            where s.Active = 1 and s.Error = 0
            order by f.Name";

        var entities = new[]
        {
            // Active=1, Ready=0, Error=0 -> 0b00000001 = 0x01
            new BinaryEntity { Name = "good1.bin", Content = [0x01] },
            // Active=1, Ready=1, Error=1 -> 0b00000111 = 0x07
            new BinaryEntity { Name = "bad.bin", Content = [0x07] },
            // Active=1, Ready=1, Error=0 -> 0b00000011 = 0x03
            new BinaryEntity { Name = "good2.bin", Content = [0x03] },
            // Active=0, Ready=0, Error=0 -> 0x00
            new BinaryEntity { Name = "inactive.bin", Content = [0x00] }
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert: Only files with Active=1 and Error=0
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("good1.bin", table[0][0]);
        Assert.AreEqual("good2.bin", table[1][0]);
    }

    #endregion

    #region Computed Fields E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithComputedField_ShouldCalculateValue()
    {
        // Arrange: Rectangle with computed Area field
        var query = @"
            binary Rectangle {
                Width: int le,
                Height: int le,
                Area: Width * Height
            };
            select
                r.Width,
                r.Height,
                r.Area
            from #test.files() f
            cross apply Interpret(f.Content, 'Rectangle') r";

        var testData = new byte[8];
        BitConverter.GetBytes(10).CopyTo(testData, 0); // Width = 10
        BitConverter.GetBytes(5).CopyTo(testData, 4); // Height = 5
        var entities = new[] { new BinaryEntity { Name = "rect.bin", Content = testData } };

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
        Assert.AreEqual(10, table[0][0]); // Width
        Assert.AreEqual(5, table[0][1]); // Height
        Assert.AreEqual(50, table[0][2]); // Area = 10 * 5
    }

    [TestMethod]
    public void Query_SelectInterpret_WithComputedBoolField_ShouldFilterByComputed()
    {
        // Arrange: Packet with computed IsLarge field used in WHERE
        var query = @"
            binary Packet {
                Size: int le,
                IsLarge: Size > 1000
            };
            select
                f.Name,
                p.Size
            from #test.files() f
            cross apply Interpret(f.Content, 'Packet') p
            where p.IsLarge = true
            order by p.Size desc";

        var entities = new[]
        {
            new BinaryEntity { Name = "small.bin", Content = BitConverter.GetBytes(100) },
            new BinaryEntity { Name = "medium.bin", Content = BitConverter.GetBytes(500) },
            new BinaryEntity { Name = "large.bin", Content = BitConverter.GetBytes(2000) },
            new BinaryEntity { Name = "huge.bin", Content = BitConverter.GetBytes(5000) }
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert: Only packets with Size > 1000
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("huge.bin", table[0][0]);
        Assert.AreEqual(5000, table[0][1]);
        Assert.AreEqual("large.bin", table[1][0]);
        Assert.AreEqual(2000, table[1][1]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMultipleComputedFields_ShouldCalculateAll()
    {
        // Arrange: Multiple derived calculations
        var query = @"
            binary Metrics {
                ValueA: int le,
                ValueB: int le,
                Sum: ValueA + ValueB,
                Diff: ValueA - ValueB,
                Product: ValueA * ValueB
            };
            select
                m.ValueA,
                m.ValueB,
                m.Sum,
                m.Diff,
                m.Product
            from #test.files() f
            cross apply Interpret(f.Content, 'Metrics') m";

        var testData = new byte[8];
        BitConverter.GetBytes(15).CopyTo(testData, 0); // ValueA = 15
        BitConverter.GetBytes(7).CopyTo(testData, 4); // ValueB = 7
        var entities = new[] { new BinaryEntity { Name = "metrics.bin", Content = testData } };

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
        Assert.AreEqual(15, table[0][0]); // ValueA
        Assert.AreEqual(7, table[0][1]); // ValueB
        Assert.AreEqual(22, table[0][2]); // Sum = 15 + 7
        Assert.AreEqual(8, table[0][3]); // Diff = 15 - 7
        Assert.AreEqual(105, table[0][4]); // Product = 15 * 7
    }

    [TestMethod]
    public void Query_SelectInterpret_WithBitwiseOperators_ShouldCalculateFlags()
    {
        // Arrange: Flag parsing using bitwise operators
        var query = @"
            binary FlaggedData {
                Flags: byte,
                Value: int le,
                IsEnabled: (Flags & 0x01) = 0x01,
                IsReadOnly: (Flags & 0x02) = 0x02,
                Priority: (Flags >> 4) & 0x0F,
                CombinedFlags: Flags | 0x80
            };
            select
                d.Flags,
                d.Value,
                d.IsEnabled,
                d.IsReadOnly,
                d.Priority,
                d.CombinedFlags
            from #test.files() f
            cross apply Interpret(f.Content, 'FlaggedData') d";

        // Flags = 0x53 = 0101 0011 binary
        // - Bit 0 (0x01) = 1 => IsEnabled = true
        // - Bit 1 (0x02) = 1 => IsReadOnly = true
        // - Bits 4-7 = 0101 = 5 => Priority = 5
        // - CombinedFlags = 0x53 | 0x80 = 0xD3 = 211
        var testData = new byte[5];
        testData[0] = 0x53; // Flags
        BitConverter.GetBytes(12345).CopyTo(testData, 1); // Value = 12345
        var entities = new[] { new BinaryEntity { Name = "flags.bin", Content = testData } };

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
        Assert.AreEqual((byte)0x53, table[0][0]); // Flags
        Assert.AreEqual(12345, table[0][1]); // Value
        Assert.IsTrue((bool?)table[0][2]); // IsEnabled (0x53 & 0x01 = 0x01)
        Assert.IsTrue((bool?)table[0][3]); // IsReadOnly (0x53 & 0x02 = 0x02)
        Assert.AreEqual(5, table[0][4]); // Priority (0x53 >> 4 = 5)
        Assert.AreEqual(0xD3, table[0][5]); // CombinedFlags (0x53 | 0x80 = 0xD3)
    }

    [TestMethod]
    public void Query_SelectInterpret_WithBitwiseXorAndShift_ShouldCalculate()
    {
        // Arrange: Test XOR and shift operations
        var query = @"
            binary BitwiseData {
                A: int le,
                B: int le,
                Xor: A ^ B,
                LeftShift: A << 2,
                RightShift: B >> 1,
                Combined: (A & 0xFF) | ((B & 0xFF) << 8)
            };
            select
                d.A,
                d.B,
                d.Xor,
                d.LeftShift,
                d.RightShift,
                d.Combined
            from #test.files() f
            cross apply Interpret(f.Content, 'BitwiseData') d";

        var testData = new byte[8];
        BitConverter.GetBytes(0x0F).CopyTo(testData, 0); // A = 15 (0x0F)
        BitConverter.GetBytes(0xF0).CopyTo(testData, 4); // B = 240 (0xF0)
        var entities = new[] { new BinaryEntity { Name = "bitwise.bin", Content = testData } };

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
        Assert.AreEqual(0x0F, table[0][0]); // A = 15
        Assert.AreEqual(0xF0, table[0][1]); // B = 240
        Assert.AreEqual(0xFF, table[0][2]); // Xor = 15 ^ 240 = 255
        Assert.AreEqual(0x3C, table[0][3]); // LeftShift = 15 << 2 = 60
        Assert.AreEqual(0x78, table[0][4]); // RightShift = 240 >> 1 = 120
        Assert.AreEqual(0xF00F, table[0][5]); // Combined = 15 | (240 << 8) = 61455
    }

    #endregion
}
