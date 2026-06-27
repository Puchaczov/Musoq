using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualTypesAndExpressionsTests
{
    #region Generic Schema Query Tests

    // Note: Generic schema instantiation in SQL queries (e.g., Interpret<Wrapper<Data>>(f.Content))
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
            cross apply Interpret<BigEndianData>(f.Content) h";

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
            cross apply Interpret<MixedEndian>(f.Content) h";

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
            cross apply Interpret<BigEndianLongData>(f.Content) h";

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
            cross apply Interpret<UnsignedData>(f.Content) h";

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
            cross apply Interpret<UnsignedIntData>(f.Content) h";

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
            cross apply Interpret<UnsignedLongData>(f.Content) h";

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
            cross apply Interpret<SignedByteData>(f.Content) h";

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
            cross apply Interpret<FloatData>(f.Content) h";

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
            cross apply Interpret<DoubleData>(f.Content) h";

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
            cross apply Interpret<FloatBigEndian>(f.Content) h";

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
}
