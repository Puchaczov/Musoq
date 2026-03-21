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
public class BinaryOrTextual_AdvancedFormatsTests : BinaryOrTextualEvaluatorTestBase
{
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
}
