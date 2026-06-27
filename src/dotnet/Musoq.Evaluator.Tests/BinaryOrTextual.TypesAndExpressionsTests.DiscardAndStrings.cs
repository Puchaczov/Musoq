using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualTypesAndExpressionsTests
{
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
            cross apply Interpret<SkippedData>(f.Content) h";

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
            cross apply Interpret<SkipData>(f.Content) h";

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
            cross apply Interpret<Utf8Data>(f.Content) h";

        var testData = "Hello"u8.ToArray();
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
            cross apply Interpret<TrimmedData>(f.Content) h";

        // "Hello" padded to 10 bytes with spaces
        var testData = "Hello     "u8.ToArray();
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
            cross apply Interpret<NullTermData>(f.Content) h";

        // "Hi" followed by null and garbage
        var testData = "Hi\u0000XXXXXXX"u8.ToArray();
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
            cross apply Interpret<AsciiData>(f.Content) h";

        var testData = "World"u8.ToArray();
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
}
