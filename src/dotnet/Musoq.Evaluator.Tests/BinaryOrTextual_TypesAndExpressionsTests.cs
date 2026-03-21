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
public class BinaryOrTextual_TypesAndExpressionsTests : BinaryOrTextualEvaluatorTestBase
{
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
}
