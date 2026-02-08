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

namespace Musoq.Evaluator.Tests;

/// <summary>
///     End-to-end tests that run full SQL queries with Interpret() function.
///     These tests validate the complete pipeline from SQL parsing through execution.
/// </summary>
[TestClass]
public class InterpretQueryE2ETests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();

    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

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

    #region TryInterpret/TryParse E2E Tests

    [TestMethod]
    public void Query_SelectTryInterpret_WithValidData_ShouldReturnResult()
    {
        // Arrange: TryInterpret with valid data
        var query = @"
            binary SimpleHeader {
                Magic: int le,
                Version: byte
            };
            select
                d.Magic,
                d.Version
            from #test.files() f
            cross apply TryInterpret(f.Content, 'SimpleHeader') d";

        var testData = new byte[5];
        BitConverter.GetBytes(0x12345678).CopyTo(testData, 0); // Magic
        testData[4] = 1; // Version
        var entities = new[] { new BinaryEntity { Name = "valid.bin", Content = testData } };

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
        Assert.AreEqual(0x12345678, table[0][0]); // Magic
        Assert.AreEqual((byte)1, table[0][1]); // Version
    }

    [TestMethod]
    public void Query_SelectTryInterpret_WithInvalidData_ShouldReturnNullValues()
    {
        // Arrange: TryInterpret with insufficient data returns null for field values
        // Using OUTER APPLY, the row is preserved with null values for the parsed fields
        var query = @"
            binary SimpleValue {
                Value: int le
            };
            select
                f.Name,
                d.Value
            from #test.files() f
            outer apply TryInterpret(f.Content, 'SimpleValue') d";

        // This data is invalid - only 2 bytes for a 4-byte int
        var testData = new byte[] { 0x01, 0x02 };
        var entities = new[] { new BinaryEntity { Name = "invalid.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert - Row is present but Value is null due to failed parse
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("invalid.bin", table[0][0]);
        Assert.IsNull(table[0][1]); // Value is null because TryInterpret failed
    }

    [TestMethod]
    public void Query_SelectTryInterpret_WithMixedData_CrossApply_ExcludesFailedParses()
    {
        // Arrange: Mix of valid and invalid files
        // With TryInterpret + CROSS APPLY, failed parses (null result) are excluded from results
        var query = @"
            binary MagicHeader {
                Magic: int le
            };
            select
                f.Name,
                d.Magic
            from #test.files() f
            cross apply TryInterpret(f.Content, 'MagicHeader') d";

        var validData = new byte[4];
        BitConverter.GetBytes(0xDEADBEEF).CopyTo(validData, 0);
        var invalidData = new byte[] { 0x01 }; // Too short - TryInterpret returns null

        var entities = new[]
        {
            new BinaryEntity { Name = "valid.bin", Content = validData },
            new BinaryEntity { Name = "invalid.bin", Content = invalidData }
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

        // Assert - Only one row (valid.bin) since invalid.bin's TryInterpret returns null
        // which is filtered out by CROSS APPLY behavior
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("valid.bin", table[0][0]);
        Assert.AreEqual(unchecked((int)0xDEADBEEF), table[0][1]);
    }

    [TestMethod]
    public void Query_SelectTryInterpret_WithMixedData_OuterApply_IncludesAllRows()
    {
        // Arrange: Mix of valid and invalid files
        // With TryInterpret + OUTER APPLY, failed parses return rows with null field values
        var query = @"
            binary MagicHeader {
                Magic: int le
            };
            select
                f.Name,
                d.Magic
            from #test.files() f
            outer apply TryInterpret(f.Content, 'MagicHeader') d
            order by f.Name asc";

        var validData = new byte[4];
        BitConverter.GetBytes(0xDEADBEEF).CopyTo(validData, 0);
        var invalidData = new byte[] { 0x01 }; // Too short - TryInterpret returns null

        var entities = new[]
        {
            new BinaryEntity { Name = "invalid.bin", Content = invalidData },
            new BinaryEntity { Name = "valid.bin", Content = validData }
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

        // Assert - Both rows present, invalid.bin has null Magic value
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("invalid.bin", table[0][0]);
        Assert.IsNull(table[0][1]); // Magic is null for failed parse
        Assert.AreEqual("valid.bin", table[1][0]);
        Assert.AreEqual(unchecked((int)0xDEADBEEF), table[1][1]);
    }

    /// <summary>
    ///     Session 5: Tests TryInterpret with empty data returns null.
    /// </summary>
    [TestMethod]
    public void Query_TryInterpret_WithEmptyData_ShouldReturnNull()
    {
        var query = @"
            binary Header {
                Magic: int le
            };
            select
                f.Name,
                d.Magic
            from #test.files() f
            outer apply TryInterpret(f.Content, 'Header') d";

        var emptyData = Array.Empty<byte>();
        var entities = new[] { new BinaryEntity { Name = "empty.bin", Content = emptyData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("empty.bin", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    /// <summary>
    ///     Session 5: Tests TryInterpret with all invalid data returns no rows with CROSS APPLY.
    /// </summary>
    [TestMethod]
    public void Query_TryInterpret_AllInvalid_CrossApply_ReturnsNoRows()
    {
        var query = @"
            binary Header {
                Value: int le
            };
            select d.Value
            from #test.files() f
            cross apply TryInterpret(f.Content, 'Header') d";

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = [0x01] },
            new BinaryEntity { Name = "b.bin", Content = [0x02, 0x03] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(0, table.Count);
    }

    /// <summary>
    ///     Session 5: Tests counting valid vs invalid parses with TryInterpret.
    /// </summary>
    [TestMethod]
    public void Query_TryInterpret_CountValidAndInvalid_ShouldCountCorrectly()
    {
        var query = @"
            binary Header {
                Value: short le
            };
            select
                Count(d.Value) as ValidCount
            from #test.files() f
            outer apply TryInterpret(f.Content, 'Header') d";

        var entities = new[]
        {
            new BinaryEntity { Name = "valid1.bin", Content = [0x01, 0x00] },
            new BinaryEntity { Name = "valid2.bin", Content = [0x02, 0x00] },
            new BinaryEntity { Name = "invalid.bin", Content = [0x01] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0][0]);
    }

    #endregion

    #region Session 8: Code Generation Coverage Tests

    /// <summary>
    ///     Tests that conditional value type fields become nullable.
    /// </summary>
    [TestMethod]
    public void Query_SelectInterpret_ConditionalValueType_ShouldBeNullable()
    {
        var query = @"
            binary Message {
                HasValue: byte,
                Value: int le when HasValue <> 0
            };
            select
                m.HasValue,
                m.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'Message') m";


        var data = new byte[] { 0x00 };
        var entities = new[] { new BinaryEntity { Name = "msg.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    /// <summary>
    ///     Tests nested schema property access in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_SelectInterpret_NestedSchema_ShouldAccessNestedProperties()
    {
        var query = @"
            binary Inner {
                X: short le,
                Y: short le
            };
            binary Outer {
                Id: byte,
                Point: Inner
            };
            select
                o.Id,
                o.Point.X,
                o.Point.Y
            from #test.files() f
            cross apply Interpret(f.Content, 'Outer') o";

        var data = new byte[]
        {
            0x42,
            0x0A, 0x00,
            0x14, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "data.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x42, table[0][0]);
        Assert.AreEqual((short)10, table[0][1]);
        Assert.AreEqual((short)20, table[0][2]);
    }

    #endregion

    #region Session 9: Real-World Format Tests

    /// <summary>
    ///     Tests parsing a simple TLV (Type-Length-Value) structure.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_TlvStructure_ShouldParse()
    {
        var query = @"
            binary TlvRecord {
                Type: byte,
                Length: byte,
                Value: byte[Length]
            };
            select
                t.Type,
                t.Length,
                t.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'TlvRecord') t";

        var data = new byte[]
        {
            0x01,
            0x03,
            0xAA, 0xBB, 0xCC
        };
        var entities = new[] { new BinaryEntity { Name = "tlv.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((byte)0x03, table[0][1]);
        var value = (byte[])table[0][2];
        Assert.HasCount(3, value);
        Assert.AreEqual((byte)0xAA, value[0]);
    }

    /// <summary>
    ///     Tests parsing a log line with timestamp and message.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SimpleLogLine_ShouldParse()
    {
        var query = @"
            text LogLine {
                Timestamp: until ' ',
                Level: until ' ',
                Message: rest
            };
            select
                l.Timestamp,
                l.Level,
                l.Message
            from #test.lines() f
            cross apply Parse(f.Line, 'LogLine') l";

        var entities = new[]
            { new TextEntity { Name = "log.txt", Text = "2024-01-15T10:30:00 INFO Application started successfully" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("2024-01-15T10:30:00", table[0][0]);
        Assert.AreEqual("INFO", table[0][1]);
        Assert.AreEqual("Application started successfully", table[0][2]);
    }

    /// <summary>
    ///     Tests parsing environment variable format.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_EnvVariable_ShouldParse()
    {
        var query = @"
            text EnvVar {
                Name: until '=',
                Value: rest
            };
            select
                e.Name,
                e.Value
            from #test.lines() f
            cross apply Parse(f.Line, 'EnvVar') e";

        var entities = new[]
        {
            new TextEntity { Name = "env1", Text = "PATH=/usr/bin:/usr/local/bin" },
            new TextEntity { Name = "env2", Text = "HOME=/home/user" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var names = new HashSet<string> { (string)table[0][0], (string)table[1][0] };
        var values = new HashSet<string> { (string)table[0][1], (string)table[1][1] };
        Assert.Contains("PATH", names);
        Assert.Contains("HOME", names);
        Assert.Contains("/usr/bin:/usr/local/bin", values);
        Assert.Contains("/home/user", values);
    }

    #endregion

    #region Schema Inheritance E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithSchemaInheritance_ShouldIncludeParentFields()
    {
        // Arrange: Child extends Parent
        var query = @"
            binary BaseHeader {
                Magic: int le,
                Version: byte
            };
            binary ExtendedHeader extends BaseHeader {
                Flags: byte,
                Length: short le
            };
            select
                h.Magic,
                h.Version,
                h.Flags,
                h.Length
            from #test.files() f
            cross apply Interpret(f.Content, 'ExtendedHeader') h";

        // Magic (4) + Version (1) + Flags (1) + Length (2) = 8 bytes
        var testData = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, // Magic = "PNG" signature bytes (0x474E5089 in LE)
            0x01, // Version = 1
            0xFF, // Flags = 0xFF
            0x00, 0x10 // Length = 4096 (little-endian)
        };
        var entities = new[] { new BinaryEntity { Name = "extended.bin", Content = testData } };

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
        Assert.AreEqual(0x474E5089, table[0][0]); // Magic (from parent)
        Assert.AreEqual((byte)1, table[0][1]); // Version (from parent)
        Assert.AreEqual((byte)0xFF, table[0][2]); // Flags (from child)
        Assert.AreEqual((short)4096, table[0][3]); // Length (from child)
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMultiLevelInheritance_ShouldIncludeAllAncestors()
    {
        var query = @"
            binary Level1 {
                A: byte
            };
            binary Level2 extends Level1 {
                B: byte
            };
            binary Level3 extends Level2 {
                C: byte
            };
            select
                l.A,
                l.B,
                l.C
            from #test.files() f
            cross apply Interpret(f.Content, 'Level3') l";

        var testData = new byte[] { 0x01, 0x02, 0x03 };
        var entities = new[] { new BinaryEntity { Name = "levels.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });


        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)2, table[0][1]);
        Assert.AreEqual((byte)3, table[0][2]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithInheritanceAndComputedField_ShouldAccessParentFields()
    {
        var query = @"
            binary BaseValue {
                Value: int le
            };
            binary DerivedValue extends BaseValue {
                Doubled: Value * 2,
                IsPositive: Value > 0
            };
            select
                d.Value,
                d.Doubled,
                d.IsPositive
            from #test.files() f
            cross apply Interpret(f.Content, 'DerivedValue') d";

        var testData = BitConverter.GetBytes(25);
        var entities = new[] { new BinaryEntity { Name = "derived.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });


        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(25, table[0][0]);
        Assert.AreEqual(50, table[0][1]);
        Assert.IsTrue((bool?)table[0][2]);
    }

    #endregion

    #region Conditional Fields E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithConditionalField_ShouldParseWhenTrue()
    {
        // Arrange: Optional field based on flag
        var query = @"
            binary OptionalData {
                HasExtra: byte,
                ExtraData: int le when HasExtra = 1,
                Value: int le
            };
            select
                o.HasExtra,
                o.ExtraData,
                o.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'OptionalData') o";

        // HasExtra=1, ExtraData=0x12345678, Value=42
        var testData = new byte[9];
        testData[0] = 1; // HasExtra = 1
        BitConverter.GetBytes(0x12345678).CopyTo(testData, 1); // ExtraData
        BitConverter.GetBytes(42).CopyTo(testData, 5); // Value

        var entities = new[] { new BinaryEntity { Name = "with_extra.bin", Content = testData } };

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
        Assert.AreEqual((byte)1, table[0][0]); // HasExtra
        Assert.AreEqual(0x12345678, table[0][1]); // ExtraData is present
        Assert.AreEqual(42, table[0][2]); // Value
    }

    [TestMethod]
    public void Query_SelectInterpret_WithConditionalField_ShouldBeNullWhenFalse()
    {
        // Arrange: Optional field skipped when condition is false
        var query = @"
            binary OptionalData {
                HasExtra: byte,
                ExtraData: int le when HasExtra = 1,
                Value: int le
            };
            select
                o.HasExtra,
                o.ExtraData,
                o.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'OptionalData') o";

        // HasExtra=0, Value=42 (no ExtraData)
        var testData = new byte[5];
        testData[0] = 0; // HasExtra = 0
        BitConverter.GetBytes(42).CopyTo(testData, 1); // Value immediately follows

        var entities = new[] { new BinaryEntity { Name = "no_extra.bin", Content = testData } };

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
        Assert.AreEqual((byte)0, table[0][0]); // HasExtra
        Assert.IsNull(table[0][1]); // ExtraData is null
        Assert.AreEqual(42, table[0][2]); // Value
    }

    [TestMethod]
    public void Query_SelectInterpret_WithConditionalField_ShouldFilterByCondition()
    {
        // Arrange: Filter records by whether optional field condition is true
        var query = @"
            binary Record {
                Type: byte,
                ExtendedInfo: short le when Type > 0,
                Data: int le
            };
            select
                f.Name,
                r.Type,
                r.ExtendedInfo
            from #test.files() f
            cross apply Interpret(f.Content, 'Record') r
            where r.Type > 0
            order by f.Name";

        var entities = new[]
        {
            // Type=0, Data only (5 bytes)
            new BinaryEntity { Name = "simple.bin", Content = [0x00, 0x01, 0x00, 0x00, 0x00] },
            // Type=1, ExtendedInfo=0x1234, Data (7 bytes)
            new BinaryEntity
                { Name = "extended.bin", Content = [0x01, 0x34, 0x12, 0x02, 0x00, 0x00, 0x00] }
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

        // Assert: Only extended.bin has ExtendedInfo
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("extended.bin", table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
        Assert.AreEqual((short)0x1234, table[0][2]);
    }

    #endregion

    #region Check Constraints E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithCheckConstraint_ShouldValidateMagicNumber()
    {
        // Arrange: File format with magic number validation
        var query = @"
            binary FileHeader {
                Magic: int le check Magic = 0x46495845,
                Version: byte,
                Size: int le
            };
            select
                h.Version,
                h.Size
            from #test.files() f
            cross apply Interpret(f.Content, 'FileHeader') h";

        // Magic = "EXIF" in little-endian (0x46495845)
        var testData = new byte[9];
        BitConverter.GetBytes(0x46495845).CopyTo(testData, 0); // Magic = "EXIF"
        testData[4] = 2; // Version = 2
        BitConverter.GetBytes(1024).CopyTo(testData, 5); // Size = 1024

        var entities = new[] { new BinaryEntity { Name = "valid.exif", Content = testData } };

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
        Assert.AreEqual((byte)2, table[0][0]); // Version
        Assert.AreEqual(1024, table[0][1]); // Size
    }

    [TestMethod]
    public void Query_SelectInterpret_WithCheckConstraint_ShouldThrowOnInvalidMagic()
    {
        // Arrange: File with invalid magic number
        var query = @"
            binary FileHeader {
                Magic: int le check Magic = 0x46495845,
                Version: byte
            };
            select
                h.Version
            from #test.files() f
            cross apply Interpret(f.Content, 'FileHeader') h";

        // Magic = wrong value
        var testData = new byte[5];
        BitConverter.GetBytes(0xDEADBEEF).CopyTo(testData, 0); // Wrong magic
        testData[4] = 1;

        var entities = new[] { new BinaryEntity { Name = "invalid.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        // Assert: Should throw validation exception
        Assert.Throws<Exception>(() => vm.Run(CancellationToken.None));
    }

    [TestMethod]
    public void Query_SelectInterpret_WithRangeCheck_ShouldValidateRange()
    {
        // Arrange: Version must be between 1 and 10
        var query = @"
            binary VersionedData {
                Version: byte check Version >= 1 and Version <= 10,
                Data: int le
            };
            select
                v.Version,
                v.Data
            from #test.files() f
            cross apply Interpret(f.Content, 'VersionedData') v";

        var testData = new byte[5];
        testData[0] = 5; // Version = 5 (valid: 1-10)
        BitConverter.GetBytes(12345).CopyTo(testData, 1);

        var entities = new[] { new BinaryEntity { Name = "v5.bin", Content = testData } };

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
        Assert.AreEqual(12345, table[0][1]);
    }

    #endregion

    #region At Positioning E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithAtPosition_ShouldSeekToOffset()
    {
        // Arrange: Read field at specific offset
        var query = @"
            binary IndexedFile {
                HeaderSize: int le,
                DataOffset: int le,
                Data: int le at DataOffset
            };
            select
                i.HeaderSize,
                i.DataOffset,
                i.Data
            from #test.files() f
            cross apply Interpret(f.Content, 'IndexedFile') i";

        // Header: HeaderSize=8, DataOffset=16
        // ...padding...
        // At offset 16: Data=42
        var testData = new byte[20];
        BitConverter.GetBytes(8).CopyTo(testData, 0); // HeaderSize
        BitConverter.GetBytes(16).CopyTo(testData, 4); // DataOffset
        BitConverter.GetBytes(42).CopyTo(testData, 16); // Data at offset 16

        var entities = new[] { new BinaryEntity { Name = "indexed.bin", Content = testData } };

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
        Assert.AreEqual(8, table[0][0]); // HeaderSize
        Assert.AreEqual(16, table[0][1]); // DataOffset
        Assert.AreEqual(42, table[0][2]); // Data read from offset 16
    }

    [TestMethod]
    public void Query_SelectInterpret_WithAtPositionAndCondition_ShouldCombineModifiers()
    {
        // Arrange: Conditional field at specific position
        var query = @"
            binary ConditionalOffset {
                HasData: byte,
                DataOffset: int le,
                Data: int le at DataOffset when HasData = 1
            };
            select
                c.HasData,
                c.DataOffset,
                c.Data
            from #test.files() f
            cross apply Interpret(f.Content, 'ConditionalOffset') c";

        // HasData=1, DataOffset=10, ...padding..., Data=999 at offset 10
        var testData = new byte[14];
        testData[0] = 1; // HasData
        BitConverter.GetBytes(10).CopyTo(testData, 1); // DataOffset
        BitConverter.GetBytes(999).CopyTo(testData, 10); // Data at offset 10

        var entities = new[] { new BinaryEntity { Name = "conditional_offset.bin", Content = testData } };

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
        Assert.AreEqual((byte)1, table[0][0]); // HasData
        Assert.AreEqual(10, table[0][1]); // DataOffset
        Assert.AreEqual(999, table[0][2]); // Data
    }

    #endregion

    #region Aggregation and Grouping E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithAggregation_ShouldSumValues()
    {
        var query = @"
            binary Amount {
                Value: int le
            };
            select
                Sum(a.Value) as TotalValue,
                Count(a.Value) as RecordCount,
                Avg(a.Value) as AvgValue
            from #test.files() f
            cross apply Interpret(f.Content, 'Amount') a";

        var entities = new[]
        {
            new BinaryEntity { Name = "a1.bin", Content = BitConverter.GetBytes(100) },
            new BinaryEntity { Name = "a2.bin", Content = BitConverter.GetBytes(200) },
            new BinaryEntity { Name = "a3.bin", Content = BitConverter.GetBytes(300) },
            new BinaryEntity { Name = "a4.bin", Content = BitConverter.GetBytes(400) }
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });


        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1000m, table[0][0]);
        Assert.AreEqual(4, table[0][1]);
        Assert.AreEqual(250m, table[0][2]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithGroupBy_ShouldGroupByField()
    {
        // Arrange: Group by category and sum values
        var query = @"
            binary Transaction {
                Category: byte,
                Amount: int le
            };
            select
                t.Category,
                Sum(t.Amount) as TotalAmount,
                Count(t.Amount) as TransactionCount
            from #test.files() f
            cross apply Interpret(f.Content, 'Transaction') t
            group by t.Category
            order by t.Category";

        var entities = new[]
        {
            // Category 1
            CreateTransactionEntity("t1.bin", 1, 100),
            CreateTransactionEntity("t2.bin", 1, 150),
            // Category 2
            CreateTransactionEntity("t3.bin", 2, 200),
            CreateTransactionEntity("t4.bin", 2, 250),
            CreateTransactionEntity("t5.bin", 2, 300),
            // Category 3
            CreateTransactionEntity("t6.bin", 3, 500)
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
        // Category 1: Sum=250, Count=2
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(250m, table[0][1]);
        Assert.AreEqual(2, table[0][2]);
        // Category 2: Sum=750, Count=3
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual(750m, table[1][1]);
        Assert.AreEqual(3, table[1][2]);
        // Category 3: Sum=500, Count=1
        Assert.AreEqual((byte)3, table[2][0]);
        Assert.AreEqual(500m, table[2][1]);
        Assert.AreEqual(1, table[2][2]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithHaving_ShouldFilterGroups()
    {
        // Arrange: Filter groups by aggregate value
        var query = @"
            binary Sale {
                Region: byte,
                Amount: int le
            };
            select
                s.Region,
                Sum(s.Amount) as TotalSales
            from #test.files() f
            cross apply Interpret(f.Content, 'Sale') s
            group by s.Region
            having Sum(s.Amount) > 500
            order by Sum(s.Amount) desc";

        var entities = new[]
        {
            // Region 1: Total = 300 (excluded by HAVING)
            CreateTransactionEntity("s1.bin", 1, 100),
            CreateTransactionEntity("s2.bin", 1, 200),
            // Region 2: Total = 900
            CreateTransactionEntity("s3.bin", 2, 400),
            CreateTransactionEntity("s4.bin", 2, 500),
            // Region 3: Total = 600
            CreateTransactionEntity("s5.bin", 3, 250),
            CreateTransactionEntity("s6.bin", 3, 350)
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

        // Assert: Only regions with total > 500
        Assert.AreEqual(2, table.Count);
        // Region 2: 900
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual(900m, table[0][1]);
        // Region 3: 600
        Assert.AreEqual((byte)3, table[1][0]);
        Assert.AreEqual(600m, table[1][1]);
    }

    private static BinaryEntity CreateTransactionEntity(string name, byte category, int amount)
    {
        var data = new byte[5];
        data[0] = category;
        BitConverter.GetBytes(amount).CopyTo(data, 1);
        return new BinaryEntity { Name = name, Content = data };
    }

    #endregion

    #region Generic Schema Query Tests

    // Note: Generic schema instantiation in SQL queries (e.g., Interpret(f.Content, 'Wrapper<Data>'))
    // is NOT yet supported in the query pipeline. The query pipeline cannot parse generic type
    // arguments from the schema name string and instantiate the generic type at runtime.
    //
    // What IS supported:
    // - Non-generic nested schemas with deep property access (e.g., v.Position.X) - see
    //   Query_SelectInterpret_WithNestedSchema_ShouldParseNestedFields test
    // - Generic schemas at the interpreter level - see BinaryInterpretationTests:
    //   Interpret_GenericSchema_* tests (11 tests covering single/multiple type parameters,
    //   arrays, computed fields, conditional fields, and nested generic instantiation)
    //
    // Missing for SQL query pipeline:
    // - Parsing 'Wrapper<Data>' to extract base schema 'Wrapper' and type argument 'Data'
    // - Looking up the generic schema in the registry by base name
    // - Using MakeGenericType to create the closed generic interpreter type
    // - Proper column type inference for fields using type parameters

    #endregion

    #region Endianness Tests

    [TestMethod]
    public void Query_SelectInterpret_WithBigEndianInt_ShouldParseBigEndian()
    {
        // Arrange: Big-endian integer
        var query = @"
            binary BigEndianData {
                Value: int be
            };
            select h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'BigEndianData') h";

        // 0x12345678 in big-endian byte order
        var testData = new byte[] { 0x12, 0x34, 0x56, 0x78 };
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
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMixedEndianness_ShouldParseEachCorrectly()
    {
        // Arrange: Mix of big and little endian fields
        var query = @"
            binary MixedEndian {
                LittleInt: int le,
                BigInt: int be,
                LittleShort: short le,
                BigShort: short be
            };
            select
                h.LittleInt,
                h.BigInt,
                h.LittleShort,
                h.BigShort
            from #test.files() f
            cross apply Interpret(f.Content, 'MixedEndian') h";

        // LittleInt=0x12345678, BigInt=0xAABBCCDD, LittleShort=0x0102, BigShort=0x0304
        var testData = new byte[]
        {
            0x78, 0x56, 0x34, 0x12, // LittleInt
            0xAA, 0xBB, 0xCC, 0xDD, // BigInt
            0x02, 0x01, // LittleShort (0x0102)
            0x03, 0x04 // BigShort (0x0304)
        };
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
        Assert.AreEqual(unchecked((int)0xAABBCCDD), table[0][1]);
        Assert.AreEqual((short)0x0102, table[0][2]);
        Assert.AreEqual((short)0x0304, table[0][3]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithBigEndianLong_ShouldParseBigEndian()
    {
        // Arrange: Big-endian long
        var query = @"
            binary BigEndianLongData {
                Value: long be
            };
            select h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'BigEndianLongData') h";

        // 0x0102030405060708 in big-endian
        var testData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
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
        Assert.AreEqual(0x0102030405060708L, table[0][0]);
    }

    #endregion

    #region Unsigned Types Tests

    [TestMethod]
    public void Query_SelectInterpret_WithUnsignedShort_ShouldParseCorrectly()
    {
        // Arrange: Unsigned short (ushort)
        var query = @"
            binary UnsignedData {
                Value: ushort le
            };
            select h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'UnsignedData') h";

        // 0xFFFF = 65535 as unsigned
        var testData = new byte[] { 0xFF, 0xFF };
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
        Assert.AreEqual((ushort)65535, table[0][0]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithUnsignedInt_ShouldParseCorrectly()
    {
        // Arrange: Unsigned int (uint)
        var query = @"
            binary UnsignedIntData {
                Value: uint le
            };
            select h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'UnsignedIntData') h";

        // 0xFFFFFFFF = 4294967295 as unsigned
        var testData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
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
        Assert.AreEqual(4294967295u, table[0][0]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithUnsignedLong_ShouldParseCorrectly()
    {
        // Arrange: Unsigned long (ulong)
        var query = @"
            binary UnsignedLongData {
                Value: ulong le
            };
            select h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'UnsignedLongData') h";

        // Large unsigned value
        var testData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }; // 2^63
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
        Assert.AreEqual(0x8000000000000000UL, table[0][0]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithSignedByte_ShouldParseCorrectly()
    {
        // Arrange: Signed byte (sbyte)
        var query = @"
            binary SignedByteData {
                Value: sbyte
            };
            select h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'SignedByteData') h";

        // 0xFF = -1 as signed byte
        var testData = new byte[] { 0xFF };
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
        Assert.AreEqual((sbyte)-1, table[0][0]);
    }

    #endregion

    #region Floating Point Tests

    [TestMethod]
    public void Query_SelectInterpret_WithFloatLittleEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary FloatData {
                Value: float le
            };
            select h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'FloatData') h";

        var testData = BitConverter.GetBytes(3.14159f);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });


        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3.14159f, (float)table[0][0], 0.00001f);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithDoubleLittleEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary DoubleData {
                Value: double le
            };
            select h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'DoubleData') h";

        var testData = BitConverter.GetBytes(3.141592653589793);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });


        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3.141592653589793, (double)table[0][0], 0.0000000001);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithFloatBigEndian_ShouldParseCorrectly()
    {
        // Arrange: Float big-endian
        var query = @"
            binary FloatBigEndian {
                Value: float be
            };
            select h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'FloatBigEndian') h";

        // 3.14159f in big-endian
        var leBytes = BitConverter.GetBytes(3.14159f);
        var beBytes = new byte[4];
        beBytes[0] = leBytes[3];
        beBytes[1] = leBytes[2];
        beBytes[2] = leBytes[1];
        beBytes[3] = leBytes[0];
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = beBytes } };

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
        Assert.AreEqual(3.14159f, (float)table[0][0], 0.00001f);
    }

    #endregion

    #region Discard Field Tests

    [TestMethod]
    public void Query_SelectInterpret_WithDiscardField_ShouldSkipBytes()
    {
        // Arrange: Discard fields to skip bytes
        var query = @"
            binary SkippedData {
                Magic: int le,
                _: byte[4],
                Value: int le
            };
            select
                h.Magic,
                h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'SkippedData') h";

        // Magic=0x12345678, 4 skipped bytes, Value=0xDEADBEEF
        var testData = new byte[]
        {
            0x78, 0x56, 0x34, 0x12, // Magic
            0x00, 0x00, 0x00, 0x00, // Discarded
            0xEF, 0xBE, 0xAD, 0xDE // Value
        };
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
        Assert.AreEqual(unchecked((int)0xDEADBEEF), table[0][1]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithSingleDiscardField_ShouldSkipBytes()
    {
        // Arrange: Single discard field skips bytes
        var query = @"
            binary SkipData {
                A: byte,
                _: byte[4],
                B: byte
            };
            select
                h.A,
                h.B
            from #test.files() f
            cross apply Interpret(f.Content, 'SkipData') h";

        // A=0x01, skip 4 bytes, B=0x06
        var testData = new byte[] { 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0x06 };
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
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((byte)0x06, table[0][1]);
    }

    #endregion

    #region String Encoding Tests

    [TestMethod]
    public void Query_SelectInterpret_WithUtf8String_ShouldParseCorrectly()
    {
        // Arrange: UTF-8 string parsing
        var query = @"
            binary Utf8Data {
                Message: string[5] utf8
            };
            select h.Message
            from #test.files() f
            cross apply Interpret(f.Content, 'Utf8Data') h";

        var testData = Encoding.UTF8.GetBytes("Hello");
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
        Assert.AreEqual("Hello", table[0][0]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithTrimmedString_ShouldRemoveTrailingSpaces()
    {
        // Arrange: String with trim modifier
        var query = @"
            binary TrimmedData {
                Value: string[10] utf8 trim
            };
            select h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'TrimmedData') h";

        // "Hello" padded to 10 bytes with spaces
        var testData = Encoding.UTF8.GetBytes("Hello     ");
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
        Assert.AreEqual("Hello", table[0][0]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithNullTerminatedString_ShouldStopAtNull()
    {
        // Arrange: Null-terminated string
        var query = @"
            binary NullTermData {
                Value: string[10] utf8 nullterm
            };
            select h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'NullTermData') h";

        // "Hi" followed by null and garbage
        var testData = new byte[]
            { (byte)'H', (byte)'i', 0x00, (byte)'X', (byte)'X', (byte)'X', (byte)'X', (byte)'X', (byte)'X', (byte)'X' };
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
        Assert.AreEqual("Hi", table[0][0]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithAsciiEncoding_ShouldParseCorrectly()
    {
        // Arrange: ASCII string parsing
        var query = @"
            binary AsciiData {
                Value: string[5] ascii
            };
            select h.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'AsciiData') h";

        var testData = Encoding.ASCII.GetBytes("World");
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
        Assert.AreEqual("World", table[0][0]);
    }

    #endregion

    #region Complex Expression Tests

    [TestMethod]
    public void Query_SelectInterpret_WithComputedArraySize_ShouldUseExpression()
    {
        // Arrange: Array size computed from expression
        var query = @"
            binary ExpressionSize {
                Count: byte,
                Data: byte[Count * 2]
            };
            select
                h.Count,
                h.Data
            from #test.files() f
            cross apply Interpret(f.Content, 'ExpressionSize') h";

        // Count=3, Data should be 6 bytes (3*2)
        var testData = new byte[] { 0x03, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
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
        Assert.AreEqual((byte)3, table[0][0]);
        var data = (byte[])table[0][1];
        Assert.HasCount(6, data);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithFieldReferenceArraySize_ShouldWork()
    {
        // Arrange: Array size from single field reference
        // Note: Complex arithmetic expressions in array sizes (e.g., Total - Offset)
        // are tested at the interpreter level in BinaryInterpretationTests.
        var query = @"
            binary SimpleHeader {
                Count: byte,
                Data: byte[Count]
            };
            select
                h.Count,
                h.Data
            from #test.files() f
            cross apply Interpret(f.Content, 'SimpleHeader') h";

        // Count=4, Data should be 4 bytes
        var testData = new byte[] { 0x04, 0x11, 0x22, 0x33, 0x44 };
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
        Assert.AreEqual((byte)4, table[0][0]);
        var data = (byte[])table[0][1];
        Assert.HasCount(4, data);
        Assert.AreEqual((byte)0x11, data[0]);
        Assert.AreEqual((byte)0x44, data[3]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithArithmeticSizeExpression_ShouldWork()
    {
        // Arrange: Array size from arithmetic expression (Total - HeaderSize)
        var query = @"
            binary DynamicPacket {
                Total: byte,
                HeaderSize: byte,
                Data: byte[Total - HeaderSize]
            };
            select
                h.Total,
                h.HeaderSize,
                h.Data
            from #test.files() f
            cross apply Interpret(f.Content, 'DynamicPacket') h";

        // Total=10, HeaderSize=2, so Data should be 10 - 2 = 8 bytes
        var testData = new byte[] { 10, 2, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
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
        Assert.AreEqual((byte)10, table[0][0]); // Total
        Assert.AreEqual((byte)2, table[0][1]); // HeaderSize
        var data = (byte[])table[0][2];
        Assert.HasCount(8, data); // 10 - 2 = 8 bytes
        Assert.AreEqual((byte)0x01, data[0]);
        Assert.AreEqual((byte)0x08, data[7]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMultiplicationSizeExpression_ShouldWork()
    {
        // Arrange: Array of shorts with size from field reference
        var query = @"
            binary ArrayPacket {
                Count: byte,
                Values: short[Count] le
            };
            select
                h.Count,
                h.Values
            from #test.files() f
            cross apply Interpret(f.Content, 'ArrayPacket') h";

        // Count=3, so Values should be 3 short values = 6 bytes
        var testData = new byte[] { 3, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00 }; // 3 little-endian shorts: 1, 2, 3
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
        Assert.AreEqual((byte)3, table[0][0]); // Count
        var values = (short[])table[0][1];
        Assert.HasCount(3, values);
        Assert.AreEqual((short)1, values[0]);
        Assert.AreEqual((short)2, values[1]);
        Assert.AreEqual((short)3, values[2]);
    }

    #endregion

    #region CTE With Interpret Tests

    [TestMethod]
    public void Query_SelectInterpret_WithCte_ShouldWorkWithCommonTableExpression()
    {
        // Arrange: Use CTE with Interpret - aliases must be used in WHERE clause
        var query = @"
            binary Header {
                Id: int le,
                FileSize: int le
            };
            with ParsedHeaders as (
                select
                    f.Name as FileName,
                    h.Id as HeaderId,
                    h.FileSize as HeaderSize
                from #test.files() f
                cross apply Interpret(f.Content, 'Header') h
            )
            select
                FileName,
                HeaderId,
                HeaderSize
            from ParsedHeaders
            where HeaderSize > 100
            order by HeaderId";

        var entities = new[]
        {
            new BinaryEntity { Name = "small.bin", Content = CreateHeaderData(1, 50) },
            new BinaryEntity { Name = "medium.bin", Content = CreateHeaderData(2, 150) },
            new BinaryEntity { Name = "large.bin", Content = CreateHeaderData(3, 500) }
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

        // Assert: Only files with Size > 100
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("medium.bin", table[0][0]);
        Assert.AreEqual(2, table[0][1]);
        Assert.AreEqual(150, table[0][2]);
        Assert.AreEqual("large.bin", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
        Assert.AreEqual(500, table[1][2]);
    }

    private static byte[] CreateHeaderData(int id, int size)
    {
        var data = new byte[8];
        BitConverter.GetBytes(id).CopyTo(data, 0);
        BitConverter.GetBytes(size).CopyTo(data, 4);
        return data;
    }

    #endregion

    #region Complex Nested Schema Tests

    [TestMethod]
    public void Query_SelectInterpret_WithNestedSchema_ShouldReturnNestedObject()
    {
        // Arrange: Nested schema - accessing the nested object itself
        var query = @"
            binary InnerData {
                Value: int le
            };
            binary OuterData {
                Id: byte,
                Child: InnerData
            };
            select
                h.Id,
                h.Child
            from #test.files() f
            cross apply Interpret(f.Content, 'OuterData') h";

        // Id=0xAB, Value=0x12345678
        var testData = new byte[] { 0xAB, 0x78, 0x56, 0x34, 0x12 };
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
        Assert.AreEqual((byte)0xAB, table[0][0]);
        // h.Child is the InnerData interpreter object
        Assert.IsNotNull(table[0][1]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithDeepNestedPropertyAccess_ShouldWork()
    {
        // Arrange: Deep property access through nested schemas (h.Child.Value)
        var query = @"
            binary InnerData {
                Value: int le
            };
            binary OuterData {
                Id: byte,
                Child: InnerData
            };
            select
                h.Id,
                h.Child.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'OuterData') h";

        // Id=0xAB, Value=0x12345678
        var testData = new byte[] { 0xAB, 0x78, 0x56, 0x34, 0x12 };
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
        Assert.AreEqual((byte)0xAB, table[0][0]);
        Assert.AreEqual(0x12345678, table[0][1]); // The nested Value field
    }

    [TestMethod]
    public void Query_SelectInterpret_WithThreeLevelDeepPropertyAccess_ShouldWork()
    {
        // Arrange: Three levels deep property access (h.Middle.Inner.Value)
        var query = @"
            binary InnerData {
                Value: int le
            };
            binary MiddleData {
                Id: byte,
                Inner: InnerData
            };
            binary OuterData {
                Flags: byte,
                Middle: MiddleData
            };
            select
                h.Flags,
                h.Middle.Id,
                h.Middle.Inner.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'OuterData') h";

        // Flags=0xFF, Id=0xAB, Value=0x12345678
        var testData = new byte[] { 0xFF, 0xAB, 0x78, 0x56, 0x34, 0x12 };
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
        Assert.AreEqual((byte)0xFF, table[0][0]); // h.Flags
        Assert.AreEqual((byte)0xAB, table[0][1]); // h.Middle.Id
        Assert.AreEqual(0x12345678, table[0][2]); // h.Middle.Inner.Value
    }

    [TestMethod]
    public void Query_SelectInterpret_WithArrayOfNestedSchemas_ShouldParseAll()
    {
        // Arrange: Array of nested schemas
        var query = @"
            binary Item {
                Value: byte
            };
            binary Container {
                ItemCount: byte,
                Items: Item[ItemCount]
            };
            select
                h.ItemCount,
                h.Items
            from #test.files() f
            cross apply Interpret(f.Content, 'Container') h";

        // ItemCount=3, Items: [0xAA, 0xBB, 0xCC]
        var testData = new byte[] { 0x03, 0xAA, 0xBB, 0xCC };
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
        Assert.AreEqual((byte)3, table[0][0]);
        var items = (object[])table[0][1];
        Assert.HasCount(3, items);
    }

    #endregion

    #region Edge Case Tests

    [TestMethod]
    public void Query_SelectInterpret_WithEmptyByteArray_ShouldParseEmpty()
    {
        // Arrange: Zero-length byte array
        var query = @"
            binary EmptyArrayData {
                Count: byte,
                Data: byte[Count]
            };
            select
                h.Count,
                h.Data
            from #test.files() f
            cross apply Interpret(f.Content, 'EmptyArrayData') h";

        // Count=0, no data bytes
        var testData = new byte[] { 0x00 };
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
        Assert.AreEqual((byte)0, table[0][0]);
        var data = (byte[])table[0][1];
        Assert.IsEmpty(data);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMaxValues_ShouldHandleCorrectly()
    {
        // Arrange: Maximum values for various types
        var query = @"
            binary MaxValues {
                MaxByte: byte,
                MaxShort: short le,
                MaxInt: int le
            };
            select
                h.MaxByte,
                h.MaxShort,
                h.MaxInt
            from #test.files() f
            cross apply Interpret(f.Content, 'MaxValues') h";

        // Max values: byte=255, short=32767, int=2147483647
        var testData = new byte[]
        {
            0xFF, // byte max
            0xFF, 0x7F, // short max (little-endian)
            0xFF, 0xFF, 0xFF, 0x7F // int max (little-endian)
        };
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
        Assert.AreEqual((byte)255, table[0][0]);
        Assert.AreEqual((short)32767, table[0][1]);
        Assert.AreEqual(2147483647, table[0][2]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMinValues_ShouldHandleCorrectly()
    {
        // Arrange: Minimum values for signed types
        var query = @"
            binary MinValues {
                MinSByte: sbyte,
                MinShort: short le,
                MinInt: int le
            };
            select
                h.MinSByte,
                h.MinShort,
                h.MinInt
            from #test.files() f
            cross apply Interpret(f.Content, 'MinValues') h";

        // Min values: sbyte=-128, short=-32768, int=-2147483648
        var testData = new byte[]
        {
            0x80, // sbyte min (-128)
            0x00, 0x80, // short min (-32768)
            0x00, 0x00, 0x00, 0x80 // int min (-2147483648)
        };
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
        Assert.AreEqual((sbyte)-128, table[0][0]);
        Assert.AreEqual((short)-32768, table[0][1]);
        Assert.AreEqual(-2147483648, table[0][2]);
    }

    #endregion

    #region Text Schema Query Tests

    [TestMethod]
    public void Query_SelectParse_WithTextSchema_ShouldParseData()
    {
        // Arrange
        var query = @"
            text LogEntry {
                Level: until ':',
                _: literal ' ',
                Message: rest
            };
            select
                p.Level,
                p.Message
            from #test.logs() f
            cross apply Parse(f.Text, 'LogEntry') p
            order by p.Level";

        var entities = new[]
        {
            new TextEntity { Name = "log1", Text = "INFO: Application started" },
            new TextEntity { Name = "log2", Text = "ERROR: Failed to connect" }
        };

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(2, table.Count);
        // Results ordered by Level (ERROR before INFO alphabetically)
        Assert.AreEqual("ERROR", table[0][0]);
        Assert.AreEqual("Failed to connect", table[0][1]);
        Assert.AreEqual("INFO", table[1][0]);
        Assert.AreEqual("Application started", table[1][1]);
    }

    [TestMethod]
    public void Query_SelectParse_WithCsvSchema_ShouldParseDelimitedData()
    {
        // Arrange
        var query = @"
            text CsvRow {
                Name: until ',',
                Age: until ',',
                City: rest
            };
            select
                p.Name,
                p.Age,
                p.City
            from #test.csv() f
            cross apply Parse(f.Line, 'CsvRow') p
            order by p.Name";

        var entities = new[]
        {
            new TextEntity { Name = "row1", Text = "John Doe,30,New York" },
            new TextEntity { Name = "row2", Text = "Jane Smith,25,Los Angeles" }
        };

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert - ordered by Name (Jane before John alphabetically)
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Jane Smith", table[0][0]);
        Assert.AreEqual("25", table[0][1]);
        Assert.AreEqual("Los Angeles", table[0][2]);
        Assert.AreEqual("John Doe", table[1][0]);
        Assert.AreEqual("30", table[1][1]);
        Assert.AreEqual("New York", table[1][2]);
    }

    #endregion

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
            cross apply Interpret(f.Content, 'PngSignature') s";

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
            cross apply Interpret(f.Content, 'TlvRecord') t";

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
            cross apply Interpret(f.Content, 'MessageFrame') m";

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
            cross apply Interpret(f.Content, 'StorageHeader') h";

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
            cross apply Interpret(f.Content, 'Mesh') m";

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
            cross apply Interpret(f.Content, 'NumericRecord') r";

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
            cross apply Interpret(f.Content, 'DataRecord') r
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
        Assert.AreEqual(350m, table[0][1]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual(400m, table[1][1]);
    }

    private static byte[] CreateDataRecord(byte category, int value)
    {
        var data = new byte[5];
        data[0] = category;
        BitConverter.GetBytes(value).CopyTo(data, 1);
        return data;
    }

    #endregion

    #region Advanced Text Schema Tests

    [TestMethod]
    public void Query_SelectParse_KeyValueConfig_WithTrim_ShouldParse()
    {
        // Arrange: Simple key=value configuration parsing
        // Note: 'until' consumes the delimiter, so no 'literal' needed after
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest trim
            };
            select
                kv.Key,
                kv.Value
            from #test.lines() l
            cross apply Parse(l.Text, 'KeyValue') kv
            order by kv.Key";

        var entities = new[]
        {
            new TextEntity { Name = "config.txt", Text = "host=localhost" },
            new TextEntity { Name = "config.txt", Text = "port=8080" },
            new TextEntity { Name = "config.txt", Text = "debug=true" }
        };

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("debug", table[0][0]);
        Assert.AreEqual("true", table[0][1]);
        Assert.AreEqual("host", table[1][0]);
        Assert.AreEqual("localhost", table[1][1]);
        Assert.AreEqual("port", table[2][0]);
        Assert.AreEqual("8080", table[2][1]);
    }

    [TestMethod]
    public void Query_SelectParse_CsvLikeFormat_MultipleFields_ShouldParse()
    {
        // Arrange: CSV-like format with multiple delimiters
        // Note: 'until' consumes the delimiter, so no 'literal' needed after
        var query = @"
            text CsvRecord {
                Id: until ',',
                Name: until ',',
                Amount: rest trim
            };
            select
                r.Id,
                r.Name,
                r.Amount
            from #test.lines() l
            cross apply Parse(l.Text, 'CsvRecord') r
            order by r.Id";

        var entities = new[]
        {
            new TextEntity { Name = "data.csv", Text = "001,Product A,100.50" },
            new TextEntity { Name = "data.csv", Text = "002,Product B,250.00" }
        };

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("001", table[0][0]);
        Assert.AreEqual("Product A", table[0][1]);
        Assert.AreEqual("100.50", table[0][2]);
        Assert.AreEqual("002", table[1][0]);
        Assert.AreEqual("Product B", table[1][1]);
        Assert.AreEqual("250.00", table[1][2]);
    }

    [TestMethod]
    public void Query_SelectParse_BracketedTimestamp_Between_ShouldExtract()
    {
        // Arrange: Log format with bracketed timestamp
        // Note: 'until' consumes the delimiter, 'between' consumes both brackets
        var query = @"
            text LogEntry {
                Timestamp: between '[' ']',
                _: literal ' ',
                Level: until ' ',
                Message: rest
            };
            select
                e.Timestamp,
                e.Level,
                e.Message
            from #test.lines() l
            cross apply Parse(l.Text, 'LogEntry') e
            order by e.Timestamp";

        var entities = new[]
        {
            new TextEntity { Name = "app.log", Text = "[2024-01-01 10:00:00] INFO Application started" },
            new TextEntity { Name = "app.log", Text = "[2024-01-01 10:00:05] ERROR Connection failed" }
        };

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("2024-01-01 10:00:00", table[0][0]);
        Assert.AreEqual("INFO", table[0][1]);
        Assert.AreEqual("Application started", table[0][2]);
        Assert.AreEqual("2024-01-01 10:00:05", table[1][0]);
        Assert.AreEqual("ERROR", table[1][1]);
        Assert.AreEqual("Connection failed", table[1][2]);
    }

    [TestMethod]
    public void Query_SelectParse_FixedWidthRecord_WithChars_ShouldParse()
    {
        // Arrange: Fixed-width record (COBOL-style)
        var query = @"
            text FixedRecord {
                Id: chars[5],
                Name: chars[20] trim,
                Amount: chars[10] trim
            };
            select
                r.Id,
                r.Name,
                r.Amount
            from #test.lines() l
            cross apply Parse(l.Text, 'FixedRecord') r
            order by r.Id";

        var entities = new[]
        {
            new TextEntity { Name = "data.dat", Text = "00001John Smith          0000100.50" },
            new TextEntity { Name = "data.dat", Text = "00002Jane Doe            0000250.00" }
        };

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("00001", table[0][0]);
        Assert.AreEqual("John Smith", table[0][1]);
        Assert.AreEqual("0000100.50", table[0][2]);
        Assert.AreEqual("00002", table[1][0]);
        Assert.AreEqual("Jane Doe", table[1][1]);
        Assert.AreEqual("0000250.00", table[1][2]);
    }

    [TestMethod]
    public void Query_SelectParse_PatternMatching_ShouldExtractTokens()
    {
        // Arrange: Extract specific patterns from text
        // Note: 'pattern' does NOT consume any delimiter, so we use 'until' to get rest
        var query = @"
            text StatusLine {
                Code: until ' ',
                Status: rest
            };
            select
                s.Code,
                s.Status
            from #test.lines() l
            cross apply Parse(l.Text, 'StatusLine') s
            order by s.Code";

        var entities = new[]
        {
            new TextEntity { Name = "responses.txt", Text = "200 OK" },
            new TextEntity { Name = "responses.txt", Text = "404 Not Found" },
            new TextEntity { Name = "responses.txt", Text = "500 Internal Server Error" }
        };

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("200", table[0][0]);
        Assert.AreEqual("OK", table[0][1]);
        Assert.AreEqual("404", table[1][0]);
        Assert.AreEqual("Not Found", table[1][1]);
        Assert.AreEqual("500", table[2][0]);
        Assert.AreEqual("Internal Server Error", table[2][1]);
    }

    #endregion

    #region Complex Query Pattern Tests

    [TestMethod]
    public void Query_SelectInterpret_CteWithInterpret_ShouldWorkWithCommonTableExpression()
    {
        // Arrange: Use CTE with interpretation results
        var query = @"
            binary Header {
                Version: short le,
                Count: int le
            };
            with ParsedHeaders as (
                select
                    f.Name as FileName,
                    h.Version as FileVersion,
                    h.Count as FileCount
                from #test.files() f
                cross apply Interpret(f.Content, 'Header') h
            )
            select
                FileName,
                FileVersion,
                FileCount
            from ParsedHeaders
            where FileCount > 50
            order by FileCount desc";

        var entities = new[]
        {
            new BinaryEntity { Name = "file1.bin", Content = CreateHeader(1, 100) },
            new BinaryEntity { Name = "file2.bin", Content = CreateHeader(2, 25) },
            new BinaryEntity { Name = "file3.bin", Content = CreateHeader(1, 75) }
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

        // Assert - only files with Count > 50
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("file1.bin", table[0][0]);
        Assert.AreEqual(100, table[0][2]);
        Assert.AreEqual("file3.bin", table[1][0]);
        Assert.AreEqual(75, table[1][2]);
    }

    private static byte[] CreateHeader(short version, int count)
    {
        var data = new byte[6];
        BitConverter.GetBytes(version).CopyTo(data, 0);
        BitConverter.GetBytes(count).CopyTo(data, 2);
        return data;
    }

    [TestMethod]
    public void Query_SelectInterpret_WithDistinct_ShouldReturnUniqueRecords()
    {
        // Arrange: Use DISTINCT with interpretation results
        var query = @"
            binary Record {
                Category: int le,
                Value: int le
            };
            select distinct
                r.Category
            from #test.files() f
            cross apply Interpret(f.Content, 'Record') r";

        // Create records with duplicate categories
        var entities = new[]
        {
            new BinaryEntity { Name = "rec1.bin", Content = CreateRecord(1, 100) },
            new BinaryEntity { Name = "rec2.bin", Content = CreateRecord(1, 200) },
            new BinaryEntity { Name = "rec3.bin", Content = CreateRecord(2, 300) },
            new BinaryEntity { Name = "rec4.bin", Content = CreateRecord(2, 400) },
            new BinaryEntity { Name = "rec5.bin", Content = CreateRecord(3, 500) }
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

        // Assert - should have 3 distinct categories
        Assert.AreEqual(3, table.Count);
        var categories = table.Select(r => (int)r[0]).OrderBy(c => c).ToList();
        Assert.AreEqual(1, categories[0]);
        Assert.AreEqual(2, categories[1]);
        Assert.AreEqual(3, categories[2]);
    }

    private static byte[] CreateRecord(int id, int value)
    {
        var data = new byte[8];
        BitConverter.GetBytes(id).CopyTo(data, 0);
        BitConverter.GetBytes(value).CopyTo(data, 4);
        return data;
    }

    [TestMethod]
    public void Query_SelectInterpret_SelfJoin_ShouldCorrelate()
    {
        // Arrange: Use a CTE with self-join on materialized records
        // Single interpretation, then self-join the CTE
        var query = @"
            binary Record {
                Id: int le,
                ParentId: int le,
                Value: int le
            };
            with AllRecords as (
                select
                    r.Id as Id,
                    r.ParentId as ParentId,
                    r.Value as Value
                from #test.files() f
                cross apply Interpret(f.Content, 'Record') r
            )
            select
                ch.Id as ChildId,
                ch.Value as ChildValue,
                pa.Id as ParentId,
                pa.Value as ParentValue
            from AllRecords ch inner join AllRecords pa on ch.ParentId = pa.Id
            order by ch.Id";

        // Create a hierarchy:
        // Record 1: Parent=0 (root), Value=100
        // Record 2: Parent=1, Value=200
        // Record 3: Parent=1, Value=300
        var entities = new[]
        {
            new BinaryEntity { Name = "rec1.bin", Content = CreateRecordWithParent(1, 0, 100) }, // root
            new BinaryEntity { Name = "rec2.bin", Content = CreateRecordWithParent(2, 1, 200) }, // child of 1
            new BinaryEntity { Name = "rec3.bin", Content = CreateRecordWithParent(3, 1, 300) } // child of 1
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

        // Assert - only records 2 and 3 have a valid parent (record 1)
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(2, table[0][0]); // ChildId
        Assert.AreEqual(200, table[0][1]); // ChildValue
        Assert.AreEqual(1, table[0][2]); // ParentId
        Assert.AreEqual(100, table[0][3]); // ParentValue
        Assert.AreEqual(3, table[1][0]); // ChildId
        Assert.AreEqual(300, table[1][1]); // ChildValue
    }

    private static byte[] CreateRecordWithParent(int id, int parentId, int value)
    {
        var data = new byte[12];
        BitConverter.GetBytes(id).CopyTo(data, 0);
        BitConverter.GetBytes(parentId).CopyTo(data, 4);
        BitConverter.GetBytes(value).CopyTo(data, 8);
        return data;
    }

    #endregion

    #region Real-World Binary Format Tests

    /// <summary>
    ///     Tests parsing of PNG file format header.
    ///     PNG files start with an 8-byte signature followed by chunks.
    ///     Each chunk has: 4-byte length, 4-byte type, data, 4-byte CRC.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_PngHeader_ShouldParseSignatureAndIHDR()
    {
        var query = @"
            binary PngSignature {
                Signature: byte[8],
                FirstChunkLength: int be,
                FirstChunkType: string[4] ascii,
                Width: int be,
                Height: int be,
                BitDepth: byte,
                ColorType: byte,
                CompressionMethod: byte,
                FilterMethod: byte,
                InterlaceMethod: byte
            };
            select
                p.Width,
                p.Height,
                p.BitDepth,
                p.ColorType,
                p.FirstChunkType
            from #test.files() f
            cross apply Interpret(f.Content, 'PngSignature') p";


        var pngData = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x03, 0x20,
            0x00, 0x00, 0x02, 0x58,
            0x08,
            0x06,
            0x00,
            0x00,
            0x00
        };

        var entities = new[] { new BinaryEntity { Name = "image.png", Content = pngData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(800, table[0][0]);
        Assert.AreEqual(600, table[0][1]);
        Assert.AreEqual((byte)8, table[0][2]);
        Assert.AreEqual((byte)6, table[0][3]);
        Assert.AreEqual("IHDR", table[0][4]);
    }

    /// <summary>
    ///     Tests parsing of BMP file format header.
    ///     BMP has a 14-byte file header followed by DIB header (typically 40 bytes for BITMAPINFOHEADER).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_BmpHeader_ShouldParseFileAndDibHeader()
    {
        var query = @"
            binary BmpHeader {
                Magic: string[2] ascii,
                FileSize: int le,
                Reserved1: short le,
                Reserved2: short le,
                PixelDataOffset: int le,
                DibHeaderSize: int le,
                Width: int le,
                Height: int le,
                ColorPlanes: short le,
                BitsPerPixel: short le,
                Compression: int le,
                ImageSize: int le,
                HorizontalRes: int le,
                VerticalRes: int le,
                ColorsInPalette: int le,
                ImportantColors: int le
            };
            select
                b.Magic,
                b.Width,
                b.Height,
                b.BitsPerPixel,
                b.Compression
            from #test.files() f
            cross apply Interpret(f.Content, 'BmpHeader') b";


        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);


        bw.Write((byte)'B');
        bw.Write((byte)'M');
        bw.Write(1024 * 768 * 3 + 54);
        bw.Write((short)0);
        bw.Write((short)0);
        bw.Write(54);


        bw.Write(40);
        bw.Write(1024);
        bw.Write(768);
        bw.Write((short)1);
        bw.Write((short)24);
        bw.Write(0);
        bw.Write(1024 * 768 * 3);
        bw.Write(2835);
        bw.Write(2835);
        bw.Write(0);
        bw.Write(0);

        var entities = new[] { new BinaryEntity { Name = "image.bmp", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("BM", table[0][0]);
        Assert.AreEqual(1024, table[0][1]);
        Assert.AreEqual(768, table[0][2]);
        Assert.AreEqual((short)24, table[0][3]);
        Assert.AreEqual(0, table[0][4]);
    }

    /// <summary>
    ///     Tests parsing of WAV audio file format (RIFF container).
    ///     WAV files have: RIFF header, fmt chunk, and data chunk.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_WavHeader_ShouldParseRiffAndFmtChunk()
    {
        var query = @"
            binary WavHeader {
                RiffMagic: string[4] ascii,
                FileSize: int le,
                WaveMagic: string[4] ascii,
                FmtChunkId: string[4] ascii,
                FmtChunkSize: int le,
                AudioFormat: short le,
                NumChannels: short le,
                SampleRate: int le,
                ByteRate: int le,
                BlockAlign: short le,
                BitsPerSample: short le
            };
            select
                w.RiffMagic,
                w.WaveMagic,
                w.AudioFormat,
                w.NumChannels,
                w.SampleRate,
                w.BitsPerSample
            from #test.files() f
            cross apply Interpret(f.Content, 'WavHeader') w";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);


        bw.Write("RIFF"u8.ToArray());
        bw.Write(44100 * 2 * 2 + 36);
        bw.Write("WAVE"u8.ToArray());


        bw.Write("fmt "u8.ToArray());
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)2);
        bw.Write(44100);
        bw.Write(44100 * 2 * 2);
        bw.Write((short)4);
        bw.Write((short)16);

        var entities = new[] { new BinaryEntity { Name = "audio.wav", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("RIFF", table[0][0]);
        Assert.AreEqual("WAVE", table[0][1]);
        Assert.AreEqual((short)1, table[0][2]);
        Assert.AreEqual((short)2, table[0][3]);
        Assert.AreEqual(44100, table[0][4]);
        Assert.AreEqual((short)16, table[0][5]);
    }

    /// <summary>
    ///     Tests parsing of ZIP local file header.
    ///     ZIP files have local file headers with signature 0x04034b50.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_ZipLocalFileHeader_ShouldParseEntry()
    {
        var query = @"
            binary ZipLocalHeader {
                Signature: int le,
                VersionNeeded: short le,
                GeneralPurpose: short le,
                CompressionMethod: short le,
                LastModTime: short le,
                LastModDate: short le,
                Crc32: int le,
                CompressedSize: int le,
                UncompressedSize: int le,
                FileNameLength: short le,
                ExtraFieldLength: short le,
                FileName: string[FileNameLength] ascii
            };
            select
                z.Signature,
                z.CompressionMethod,
                z.CompressedSize,
                z.UncompressedSize,
                z.FileName
            from #test.files() f
            cross apply Interpret(f.Content, 'ZipLocalHeader') z";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        var fileName = "hello.txt"u8.ToArray();

        bw.Write(0x04034b50);
        bw.Write((short)20);
        bw.Write((short)0);
        bw.Write((short)8);
        bw.Write((short)0);
        bw.Write((short)0);
        bw.Write(0x12345678);
        bw.Write(100);
        bw.Write(200);
        bw.Write((short)fileName.Length);
        bw.Write((short)0);
        bw.Write(fileName);

        var entities = new[] { new BinaryEntity { Name = "archive.zip", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x04034b50, table[0][0]);
        Assert.AreEqual((short)8, table[0][1]);
        Assert.AreEqual(100, table[0][2]);
        Assert.AreEqual(200, table[0][3]);
        Assert.AreEqual("hello.txt", table[0][4]);
    }

    /// <summary>
    ///     Tests parsing of GIF header (GIF89a format).
    ///     GIF files have: signature, logical screen descriptor, and global color table.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_GifHeader_ShouldParseSignatureAndScreenDescriptor()
    {
        var query = @"
            binary GifHeader {
                Signature: string[3] ascii,
                Version: string[3] ascii,
                Width: short le,
                Height: short le,
                PackedByte: byte,
                BackgroundColorIndex: byte,
                PixelAspectRatio: byte
            };
            select
                g.Signature,
                g.Version,
                g.Width,
                g.Height,
                g.PackedByte,
                g.BackgroundColorIndex
            from #test.files() f
            cross apply Interpret(f.Content, 'GifHeader') g";

        var gifData = new byte[]
        {
            0x47, 0x49, 0x46,
            0x38, 0x39, 0x61,
            0x80, 0x02,
            0xE0, 0x01,
            0xF7,
            0x00,
            0x00
        };

        var entities = new[] { new BinaryEntity { Name = "image.gif", Content = gifData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("GIF", table[0][0]);
        Assert.AreEqual("89a", table[0][1]);
        Assert.AreEqual((short)640, table[0][2]);
        Assert.AreEqual((short)480, table[0][3]);
        Assert.AreEqual((byte)0xF7, table[0][4]);
        Assert.AreEqual((byte)0, table[0][5]);
    }

    /// <summary>
    ///     Tests parsing of ELF executable header (Linux binary format).
    ///     ELF files have a 52-byte (32-bit) or 64-byte (64-bit) header.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_Elf64Header_ShouldParseExecutable()
    {
        var query = @"
            binary Elf64Header {
                Magic: byte[4],
                Class: byte,
                Endianness: byte,
                Version: byte,
                OsAbi: byte,
                AbiVersion: byte,
                Padding: byte[7],
                Type: short le,
                Machine: short le,
                ElfVersion: int le,
                EntryPoint: long le,
                ProgramHeaderOffset: long le,
                SectionHeaderOffset: long le
            };
            select
                e.Class,
                e.Endianness,
                e.Type,
                e.Machine,
                e.EntryPoint
            from #test.files() f
            cross apply Interpret(f.Content, 'Elf64Header') e
            where e.Class = 2";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);


        bw.Write(new byte[] { 0x7F, 0x45, 0x4C, 0x46 });
        bw.Write((byte)2);
        bw.Write((byte)1);
        bw.Write((byte)1);
        bw.Write((byte)0);
        bw.Write((byte)0);
        bw.Write(new byte[7]);
        bw.Write((short)2);
        bw.Write((short)62);
        bw.Write(1);
        bw.Write((long)0x400000);
        bw.Write((long)64);
        bw.Write((long)0);

        var entities = new[] { new BinaryEntity { Name = "program", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
        Assert.AreEqual((short)2, table[0][2]);
        Assert.AreEqual((short)62, table[0][3]);
        Assert.AreEqual(0x400000L, table[0][4]);
    }

    /// <summary>
    ///     Tests parsing of TAR archive header (USTAR format).
    ///     TAR headers are 512 bytes with fixed-width ASCII fields.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_TarHeader_ShouldParseArchiveEntry()
    {
        var query = @"
            binary TarHeader {
                FileName: string[100] ascii,
                FileMode: string[8] ascii,
                OwnerId: string[8] ascii,
                GroupId: string[8] ascii,
                FileSize: string[12] ascii,
                ModTime: string[12] ascii,
                Checksum: string[8] ascii,
                TypeFlag: byte,
                LinkName: string[100] ascii,
                UstarMagic: string[6] ascii,
                UstarVersion: string[2] ascii,
                OwnerName: string[32] ascii,
                GroupName: string[32] ascii
            };
            select
                Trim(t.FileName) as FileName,
                Trim(t.FileMode) as FileMode,
                Trim(t.FileSize) as FileSize,
                t.TypeFlag,
                Trim(t.UstarMagic) as Magic,
                Trim(t.OwnerName) as Owner
            from #test.files() f
            cross apply Interpret(f.Content, 'TarHeader') t";


        var header = new byte[512];
        var fileName = "documents/report.txt"u8.ToArray();
        var fileMode = "0000644\0"u8.ToArray();
        var ownerId = "0001750\0"u8.ToArray();
        var groupId = "0001750\0"u8.ToArray();
        var fileSize = "00000001234\0"u8.ToArray();
        var modTime = "14342633427\0"u8.ToArray();
        var checksum = "        "u8.ToArray();
        var ustarMagic = "ustar\0"u8.ToArray();
        var ustarVersion = "00"u8.ToArray();
        var ownerName = "developer"u8.ToArray();

        Array.Copy(fileName, 0, header, 0, fileName.Length);
        Array.Copy(fileMode, 0, header, 100, fileMode.Length);
        Array.Copy(ownerId, 0, header, 108, ownerId.Length);
        Array.Copy(groupId, 0, header, 116, groupId.Length);
        Array.Copy(fileSize, 0, header, 124, fileSize.Length);
        Array.Copy(modTime, 0, header, 136, modTime.Length);
        Array.Copy(checksum, 0, header, 148, checksum.Length);
        header[156] = (byte)'0';
        Array.Copy(ustarMagic, 0, header, 257, ustarMagic.Length);
        Array.Copy(ustarVersion, 0, header, 263, ustarVersion.Length);
        Array.Copy(ownerName, 0, header, 265, ownerName.Length);

        var entities = new[] { new BinaryEntity { Name = "archive.tar", Content = header } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);


        Assert.StartsWith("documents/report.txt", (string)table[0][0]);
        Assert.StartsWith("0000644", (string)table[0][1]);
        Assert.StartsWith("00000001234", (string)table[0][2]);
        Assert.AreEqual((byte)'0', table[0][3]);
        Assert.StartsWith("ustar", (string)table[0][4]);
        Assert.StartsWith("developer", (string)table[0][5]);
    }

    #endregion

    #region Real-World Text Format Tests

    /// <summary>
    ///     Tests parsing of key=value configuration format (simpler than Apache logs).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SimpleConfig_ShouldParseKeyValuePairs()
    {
        var query = @"
            text Config {
                Key: until '=',
                Value: rest trim
            };
            select
                c.Key,
                c.Value
            from #test.files() f
            cross apply Parse(f.Text, 'Config') c";

        var configLines = new[]
        {
            "host=localhost",
            "port=5432",
            "database=myapp",
            "user=admin"
        };

        var entities = configLines.Select((line, i) => new TextEntity
        {
            Name = $"config_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(4, table.Count);
        var keys = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("host", keys);
        Assert.Contains("port", keys);
        Assert.Contains("database", keys);
        Assert.Contains("user", keys);
    }

    /// <summary>
    ///     Tests parsing of colon-separated format like /etc/passwd.
    ///     Format: username:password:uid:gid:gecos:home:shell
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_ColonSeparated_ShouldParseUserEntries()
    {
        var query = @"
            text PasswdEntry {
                Username: until ':',
                Password: until ':',
                Uid: until ':',
                Gid: until ':',
                Gecos: until ':',
                HomeDir: until ':',
                Shell: rest
            };
            select
                p.Username,
                p.Uid,
                p.Gid,
                p.HomeDir,
                p.Shell
            from #test.files() f
            cross apply Parse(f.Text, 'PasswdEntry') p
            where p.Uid <> '65534'";

        var passwdLines = new[]
        {
            "root:x:0:0:root:/root:/bin/bash",
            "daemon:x:1:1:daemon:/usr/sbin:/usr/sbin/nologin",
            "www-data:x:33:33:www-data:/var/www:/usr/sbin/nologin",
            "nobody:x:65534:65534:nobody:/nonexistent:/usr/sbin/nologin",
            "developer:x:1000:1000:Developer Account:/home/developer:/bin/zsh"
        };

        var entities = passwdLines.Select((line, i) => new TextEntity
        {
            Name = $"passwd_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(4, table.Count);
        var usernames = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("root", usernames);
        Assert.Contains("daemon", usernames);
        Assert.Contains("www-data", usernames);
        Assert.Contains("developer", usernames);
        Assert.DoesNotContain("nobody", usernames);
    }

    /// <summary>
    ///     Tests parsing of pipe-separated log format.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_PipeSeparated_ShouldParseLogEntries()
    {
        var query = @"
            text PipeLog {
                Timestamp: until '|',
                Level: until '|',
                Component: until '|',
                Message: rest trim
            };
            select
                l.Timestamp,
                l.Level,
                l.Component,
                l.Message
            from #test.files() f
            cross apply Parse(f.Text, 'PipeLog') l
            where l.Level = 'ERROR'";

        var logLines = new[]
        {
            "2024-01-05 10:30:00|INFO|WebServer|Request received from 10.0.0.1",
            "2024-01-05 10:30:01|ERROR|Database|Connection timeout after 30s",
            "2024-01-05 10:30:02|DEBUG|Cache|Cache miss for key user_123",
            "2024-01-05 10:30:03|ERROR|Auth|Invalid token for user admin"
        };

        var entities = logLines.Select((line, i) => new TextEntity
        {
            Name = $"log_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var components = table.Select(r => (string)r[2]).ToList();
        Assert.Contains("Database", components);
        Assert.Contains("Auth", components);

        Assert.IsTrue(table.All(r => (string)r[1] == "ERROR"));
    }

    /// <summary>
    ///     Tests parsing of HTTP headers (simple Name: Value format).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_HttpHeaders_ShouldParseRequestLine()
    {
        var query = @"
            text HttpHeader {
                Name: until ':',
                _: until ' ',
                Value: rest
            };
            select
                h.Name,
                h.Value
            from #test.files() f
            cross apply Parse(f.Text, 'HttpHeader') h
            where h.Name in ('Content-Type', 'Authorization', 'User-Agent')";

        var headers = new[]
        {
            "Host: api.example.com",
            "Content-Type: application/json",
            "Authorization: Bearer eyJhbGciOiJIUzI1NiIs",
            "User-Agent: MyApp/1.0.0",
            "Accept: */*",
            "Content-Length: 256"
        };

        var entities = headers.Select((h, i) => new TextEntity
        {
            Name = $"header_{i}",
            Text = h
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        var names = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("Content-Type", names);
        Assert.Contains("Authorization", names);
        Assert.Contains("User-Agent", names);
    }

    /// <summary>
    ///     Tests parsing of space-separated fixed-width fields.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SpaceSeparated_ShouldParseData()
    {
        var query = @"
            text DataEntry {
                Id: until ' ',
                Name: until ' ',
                Value: until ' ',
                Status: rest trim
            };
            select
                d.Id,
                d.Name,
                d.Value,
                d.Status
            from #test.files() f
            cross apply Parse(f.Text, 'DataEntry') d";

        var dataLines = new[]
        {
            "001 Alpha 100 Active",
            "002 Beta 200 Pending",
            "003 Gamma 300 Complete",
            "004 Delta 400 Failed"
        };

        var entities = dataLines.Select((line, i) => new TextEntity
        {
            Name = $"data_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(4, table.Count);
        var ids = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("001", ids);
        Assert.Contains("002", ids);
        Assert.Contains("003", ids);
        Assert.Contains("004", ids);
    }

    /// <summary>
    ///     Tests parsing of tab-separated values (TSV format).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_TabSeparated_ShouldParseTsvData()
    {
        var query = @"
            text TsvRow {
                Name: until '\t',
                Age: until '\t',
                City: rest trim
            };
            select
                t.Name,
                t.Age,
                t.City
            from #test.files() f
            cross apply Parse(f.Text, 'TsvRow') t";

        var tsvLines = new[]
        {
            "Alice\t30\tNew York",
            "Bob\t25\tLos Angeles",
            "Charlie\t35\tChicago"
        };

        var entities = tsvLines.Select((line, i) => new TextEntity
        {
            Name = $"tsv_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        var names = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("Alice", names);
        Assert.Contains("Bob", names);
        Assert.Contains("Charlie", names);
    }

    /// <summary>
    ///     Tests parsing of semicolon-separated format (like CSV with semicolons).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SemicolonSeparated_ShouldParseCsvData()
    {
        var query = @"
            text SemicolonCsv {
                ProductId: until ';',
                ProductName: until ';',
                Price: until ';',
                Quantity: rest trim
            };
            select
                c.ProductId,
                c.ProductName,
                c.Price,
                c.Quantity
            from #test.files() f
            cross apply Parse(f.Text, 'SemicolonCsv') c
            where c.ProductName in ('Laptop', 'Keyboard', 'Monitor')";

        var csvLines = new[]
        {
            "P001;Laptop;999.99;10",
            "P002;Mouse;29.99;150",
            "P003;Keyboard;79.99;75",
            "P004;USB Cable;9.99;500",
            "P005;Monitor;299.99;25"
        };

        var entities = csvLines.Select((line, i) => new TextEntity
        {
            Name = $"csv_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        var products = table.Select(r => (string)r[1]).ToList();
        Assert.Contains("Laptop", products);
        Assert.Contains("Keyboard", products);
        Assert.Contains("Monitor", products);
    }

    /// <summary>
    ///     Tests parsing of git log oneline format (hash + message).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_GitLogOneline_ShouldParseCommits()
    {
        var query = @"
            text GitCommit {
                Hash: until ' ',
                Message: rest trim
            };
            select
                g.Hash,
                g.Message
            from #test.files() f
            cross apply Parse(f.Text, 'GitCommit') g
            where g.Message like '%Fix%' or g.Message like '%Bug%'
            order by g.Hash desc";

        var gitLog = new[]
        {
            "a1b2c3d4 Add new feature for user authentication",
            "e5f6a7b8 Fix null pointer exception in parser",
            "c9d0e1f2 Update dependencies to latest versions",
            "a3b4c5d6 Bug fix handle empty input gracefully",
            "e7f8a9b0 Refactor database connection pooling"
        };

        var entities = gitLog.Select((line, i) => new TextEntity
        {
            Name = $"commit_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("e5f6a7b8", table[0][0]);
        Assert.AreEqual("a3b4c5d6", table[1][0]);
    }

    /// <summary>
    ///     Tests parsing of simple CSV format (comma-separated).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SimpleCsv_ShouldParseFields()
    {
        var query = @"
            text CsvRow {
                Name: until ',',
                Address: until ',',
                Age: until ',',
                Salary: rest trim
            };
            select
                c.Name,
                c.Address,
                c.Age,
                c.Salary
            from #test.files() f
            cross apply Parse(f.Text, 'CsvRow') c";

        var csvLines = new[]
        {
            "John Smith,123 Main St,35,75000",
            "Jane Doe,456 Oak Ave,28,82000",
            "Bob Wilson,789 Pine Rd,42,95000"
        };

        var entities = csvLines.Select((line, i) => new TextEntity
        {
            Name = $"row_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        var names = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("John Smith", names);
        Assert.Contains("Jane Doe", names);
        Assert.Contains("Bob Wilson", names);
    }

    /// <summary>
    ///     Tests parsing of email-style headers (Name: Value format).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_EmailHeaders_ShouldParseMailFields()
    {
        var query = @"
            text EmailHeader {
                Field: until ':',
                _: until ' ',
                Value: rest
            };
            select
                e.Field,
                e.Value
            from #test.files() f
            cross apply Parse(f.Text, 'EmailHeader') e
            where e.Field in ('From', 'To', 'Subject', 'Date')";

        var emailHeaders = new[]
        {
            "From: sender@example.com",
            "To: recipient@example.com",
            "Subject: Important Meeting Tomorrow",
            "Date: Mon 5 Jan 2026 10:30:00",
            "Message-ID: abc123@mail.example.com",
            "MIME-Version: 1.0",
            "Content-Type: text/plain"
        };

        var entities = emailHeaders.Select((h, i) => new TextEntity
        {
            Name = $"header_{i}",
            Text = h
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(4, table.Count);
        var fields = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("From", fields);
        Assert.Contains("To", fields);
        Assert.Contains("Subject", fields);
        Assert.Contains("Date", fields);
    }

    /// <summary>
    ///     Tests parsing of URL-like format (protocol://host/path).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_UrlFormat_ShouldParseUrlComponents()
    {
        var query = @"
            text UrlEntry {
                Protocol: until ':',
                _: until '/',
                _: until '/',
                Host: until '/',
                Path: rest trim
            };
            select
                u.Protocol,
                u.Host,
                u.Path
            from #test.files() f
            cross apply Parse(f.Text, 'UrlEntry') u
            where u.Protocol = 'https'";

        var urls = new[]
        {
            "https://api.example.com/v1/users",
            "http://localhost/health",
            "https://cdn.example.net/assets/image.png",
            "ftp://files.example.org/pub/file.zip"
        };

        var entities = urls.Select((url, i) => new TextEntity
        {
            Name = $"url_{i}",
            Text = url
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var hosts = table.Select(r => (string)r[1]).ToList();
        Assert.Contains("api.example.com", hosts);
        Assert.Contains("cdn.example.net", hosts);

        Assert.IsTrue(table.All(r => (string)r[0] == "https"));
    }

    #endregion

    #region Array Indexing in WHERE Clause Tests

    /// <summary>
    ///     Tests array indexing in WHERE clause for byte arrays.
    /// </summary>
    [TestMethod]
    public void Query_WhereClause_WithByteArrayIndexing_ShouldFilter()
    {
        var query = @"
            binary MagicHeader {
                Magic: byte[4],
                Version: int le
            };
            select
                f.Name,
                h.Version
            from #test.files() f
            cross apply Interpret(f.Content, 'MagicHeader') h
            where h.Magic[0] = 0x7F and h.Magic[1] = 0x45";

        using var ms1 = new MemoryStream();
        using var bw1 = new BinaryWriter(ms1);
        bw1.Write(new byte[] { 0x7F, 0x45, 0x4C, 0x46 });
        bw1.Write(1);

        using var ms2 = new MemoryStream();
        using var bw2 = new BinaryWriter(ms2);
        bw2.Write(new byte[] { 0x4D, 0x5A, 0x00, 0x00 });
        bw2.Write(2);

        using var ms3 = new MemoryStream();
        using var bw3 = new BinaryWriter(ms3);
        bw3.Write(new byte[] { 0x7F, 0x45, 0x00, 0x00 });
        bw3.Write(3);

        var entities = new[]
        {
            new BinaryEntity { Name = "elf.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "mz.bin", Content = ms2.ToArray() },
            new BinaryEntity { Name = "custom.bin", Content = ms3.ToArray() }
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);


        Assert.AreEqual(2, table.Count);
        var names = table.Select(r => (string)r[0]).OrderBy(n => n).ToList();
        Assert.AreEqual("custom.bin", names[0]);
        Assert.AreEqual("elf.bin", names[1]);
    }

    /// <summary>
    ///     Tests array indexing in SELECT clause for byte arrays.
    /// </summary>
    [TestMethod]
    public void Query_SelectClause_WithByteArrayIndexing_ShouldExtractElements()
    {
        var query = @"
            binary MagicHeader {
                Magic: byte[4],
                Version: int le
            };
            select
                h.Magic[0],
                h.Magic[1],
                h.Magic[2],
                h.Magic[3],
                h.Version
            from #test.files() f
            cross apply Interpret(f.Content, 'MagicHeader') h";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(new byte[] { 0x7F, 0x45, 0x4C, 0x46 });
        bw.Write(42);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x7F, table[0][0]);
        Assert.AreEqual((byte)0x45, table[0][1]);
        Assert.AreEqual((byte)0x4C, table[0][2]);
        Assert.AreEqual((byte)0x46, table[0][3]);
        Assert.AreEqual(42, table[0][4]);
    }

    #endregion

    #region Test Entities and Schema Infrastructure

    /// <summary>
    ///     Entity with binary content for testing Interpret().
    /// </summary>
    public class BinaryEntity
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(Name), 0 },
            { nameof(Content), 1 }
        };

        public static readonly IReadOnlyDictionary<int, Func<BinaryEntity, object>> IndexToObjectAccessMap =
            new Dictionary<int, Func<BinaryEntity, object>>
            {
                { 0, e => e.Name },
                { 1, e => e.Content }
            };

        public string Name { get; set; } = string.Empty;
        public byte[] Content { get; set; } = [];
    }

    /// <summary>
    ///     Entity with text content for testing Parse().
    /// </summary>
    public class TextEntity
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(Name), 0 },
            { nameof(Text), 1 },
            { nameof(Line), 1 } // Alias for Text
        };

        public static readonly IReadOnlyDictionary<int, Func<TextEntity, object>> IndexToObjectAccessMap =
            new Dictionary<int, Func<TextEntity, object>>
            {
                { 0, e => e.Name },
                { 1, e => e.Text }
            };

        public string Name { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Line => Text;
    }

    /// <summary>
    ///     Table for binary entities.
    /// </summary>
    public class BinaryEntityTable : ISchemaTable
    {
        public ISchemaColumn[] Columns =>
        [
            new SchemaColumn(nameof(BinaryEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(BinaryEntity.Content), 1, typeof(byte[]))
        ];

        public SchemaTableMetadata Metadata => new(typeof(BinaryEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Array.Find(Columns, c => c.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Array.FindAll(Columns, c => c.ColumnName == name);
        }
    }

    /// <summary>
    ///     Table for text entities.
    /// </summary>
    public class TextEntityTable : ISchemaTable
    {
        public ISchemaColumn[] Columns =>
        [
            new SchemaColumn(nameof(TextEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(TextEntity.Text), 1, typeof(string)),
            new SchemaColumn(nameof(TextEntity.Line), 1, typeof(string))
        ];

        public SchemaTableMetadata Metadata => new(typeof(TextEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Array.Find(Columns, c => c.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Array.FindAll(Columns, c => c.ColumnName == name);
        }
    }

    /// <summary>
    ///     Schema for binary entities with byte[] content.
    /// </summary>
    public class BinarySchema : SchemaBase
    {
        private readonly IEnumerable<BinaryEntity> _entities;

        public BinarySchema(IEnumerable<BinaryEntity> entities)
            : base("test", CreateLibrary())
        {
            _entities = entities;
        }

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext,
            params object[] parameters)
        {
            return new BinaryEntityTable();
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new EntitySource<BinaryEntity>(
                _entities,
                BinaryEntity.NameToIndexMap,
                BinaryEntity.IndexToObjectAccessMap);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            return new MethodsAggregator(methodManager);
        }
    }

    /// <summary>
    ///     Schema for text entities with string content.
    /// </summary>
    public class TextSchema : SchemaBase
    {
        private readonly IEnumerable<TextEntity> _entities;

        public TextSchema(IEnumerable<TextEntity> entities)
            : base("test", CreateLibrary())
        {
            _entities = entities;
        }

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext,
            params object[] parameters)
        {
            return new TextEntityTable();
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new EntitySource<TextEntity>(
                _entities,
                TextEntity.NameToIndexMap,
                TextEntity.IndexToObjectAccessMap);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            return new MethodsAggregator(methodManager);
        }
    }

    /// <summary>
    ///     Schema provider for binary entities.
    /// </summary>
    public class BinarySchemaProvider : ISchemaProvider
    {
        private readonly IDictionary<string, IEnumerable<BinaryEntity>> _values;

        public BinarySchemaProvider(IDictionary<string, IEnumerable<BinaryEntity>> values)
        {
            _values = values;
        }

        public ISchema GetSchema(string schema)
        {
            if (_values.TryGetValue(schema, out var entities)) return new BinarySchema(entities);
            throw new InvalidOperationException($"Schema '{schema}' not found");
        }
    }

    /// <summary>
    ///     Schema provider for text entities.
    /// </summary>
    public class TextSchemaProvider : ISchemaProvider
    {
        private readonly IDictionary<string, IEnumerable<TextEntity>> _values;

        public TextSchemaProvider(IDictionary<string, IEnumerable<TextEntity>> values)
        {
            _values = values;
        }

        public ISchema GetSchema(string schema)
        {
            if (_values.TryGetValue(schema, out var entities)) return new TextSchema(entities);
            throw new InvalidOperationException($"Schema '{schema}' not found");
        }
    }

    /// <summary>
    ///     Simple schema column implementation.
    /// </summary>
    private class SchemaColumn : ISchemaColumn
    {
        public SchemaColumn(string columnName, int columnIndex, Type columnType)
        {
            ColumnName = columnName;
            ColumnIndex = columnIndex;
            ColumnType = columnType;
        }

        public string ColumnName { get; }
        public int ColumnIndex { get; }
        public Type ColumnType { get; }
    }

    #endregion

    #region Bitwise Expression Alias Tests

    /// <summary>
    ///     Tests bitwise AND operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithBitwiseAndAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                PackedByte: byte
            };
            select
                h.PackedByte & 0x80 as HighBit
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[] { 0xF7 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x80L, table[0][0]);
    }

    /// <summary>
    ///     Tests bitwise OR operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithBitwiseOrAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                LowByte: byte,
                HighByte: byte
            };
            select
                h.LowByte | h.HighByte as Combined
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[] { 0x0F, 0xF0 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xFF, Convert.ToInt32(table[0][0]));
    }

    /// <summary>
    ///     Tests bitwise XOR operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithBitwiseXorAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                A: byte,
                B: byte
            };
            select
                h.A ^ h.B as Xored
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[] { 0xAA, 0x55 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xFF, Convert.ToInt32(table[0][0]));
    }

    /// <summary>
    ///     Tests left shift operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithLeftShiftAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                Value: byte
            };
            select
                h.Value << 4 as Shifted
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[] { 0x0F };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xF0, Convert.ToInt32(table[0][0]));
    }

    /// <summary>
    ///     Tests right shift operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithRightShiftAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                Value: byte
            };
            select
                h.Value >> 4 as Shifted
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[] { 0xF0 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x0F, Convert.ToInt32(table[0][0]));
    }

    /// <summary>
    ///     Tests complex bitwise expression with alias in SELECT.
    ///     Note: This tests parentheses with bitwise and shift operators.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithComplexBitwiseExpressionAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                PackedByte: byte
            };
            select
                h.PackedByte >> 4 as HighNibble,
                h.PackedByte & 0x0F as LowNibble
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[] { 0xAB };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(0x0A, Convert.ToInt32(table[0][0]));
        Assert.AreEqual(0x0BL, table[0][1]);
    }

    /// <summary>
    ///     Tests multiple bitwise aliases in a single SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithMultipleBitwiseAliases_ShouldWork()
    {
        var query = @"
            binary GifFlags {
                PackedByte: byte
            };
            select
                h.PackedByte & 0x80 as HasGlobalColorTable,
                (h.PackedByte & 0x70) >> 4 as ColorResolution,
                h.PackedByte & 0x08 as SortFlag,
                h.PackedByte & 0x07 as SizeOfGlobalColorTable
            from #test.files() f
            cross apply Interpret(f.Content, 'GifFlags') h";


        var testData = new byte[] { 0xF7 };
        var entities = new[] { new BinaryEntity { Name = "test.gif", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x80L, table[0][0]);
        Assert.AreEqual(0x07L, table[0][1]);
        Assert.AreEqual(0x00L, table[0][2]);
        Assert.AreEqual(0x07L, table[0][3]);
    }

    #endregion
}
