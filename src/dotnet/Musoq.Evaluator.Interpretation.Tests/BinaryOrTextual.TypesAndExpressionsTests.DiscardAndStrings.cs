using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualTypesAndExpressionsTests
{
    #region Discard Field Tests

    [TestMethod]
    public void Query_SelectInterpret_WithDiscardField_ShouldSkipBytes()
    {
        // Arrange: Discard fields to skip bytes
        var query = (
            "\n"
            + "            binary SkippedData {\n"
            + "                Magic: int le,\n"
            + "                _: byte[4],\n"
            + "                Value: int le\n"
            + "            };\n"
            + "            select\n"
            + "                h.Magic,\n"
            + "                h.Value\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<SkippedData>(f.Content) h"
        );

        // Magic=0x12345678, 4 skipped bytes, Value=0xDEADBEEF
        var testData = new byte[]
        {
            0x78,
            0x56,
            0x34,
            0x12, // Magic
            0x00,
            0x00,
            0x00,
            0x00, // Discarded
            0xEF,
            0xBE,
            0xAD,
            0xDE, // Value
        };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x12345678, table[0].ReadValue(0));
        Assert.AreEqual(unchecked((int)0xDEADBEEF), table[0].ReadValue(1));
    }

    [TestMethod]
    public void Query_SelectInterpret_WithSingleDiscardField_ShouldSkipBytes()
    {
        // Arrange: Single discard field skips bytes
        var query = (
            "\n"
            + "            binary SkipData {\n"
            + "                A: byte,\n"
            + "                _: byte[4],\n"
            + "                B: byte\n"
            + "            };\n"
            + "            select\n"
            + "                h.A,\n"
            + "                h.B\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<SkipData>(f.Content) h"
        );

        // A=0x01, skip 4 bytes, B=0x06
        var testData = new byte[] { 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0x06 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0].ReadValue(0));
        Assert.AreEqual((byte)0x06, table[0].ReadValue(1));
    }

    #endregion

    #region String Encoding Tests

    [TestMethod]
    public void Query_SelectInterpret_WithUtf8String_ShouldParseCorrectly()
    {
        // Arrange: UTF-8 string parsing
        var query = (
            "\n"
            + "            binary Utf8Data {\n"
            + "                Message: string[5] utf8\n"
            + "            };\n"
            + "            select h.Message\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Utf8Data>(f.Content) h"
        );

        var testData = "Hello"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0].ReadValue(0));
    }

    [TestMethod]
    public void Query_SelectInterpret_WithTrimmedString_ShouldRemoveTrailingSpaces()
    {
        // Arrange: String with trim modifier
        var query = (
            "\n"
            + "            binary TrimmedData {\n"
            + "                Value: string[10] utf8 trim\n"
            + "            };\n"
            + "            select h.Value\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<TrimmedData>(f.Content) h"
        );

        // "Hello" padded to 10 bytes with spaces
        var testData = "Hello     "u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0].ReadValue(0));
    }

    [TestMethod]
    public void Query_SelectInterpret_WithNullTerminatedString_ShouldStopAtNull()
    {
        // Arrange: Null-terminated string
        var query = (
            "\n"
            + "            binary NullTermData {\n"
            + "                Value: string[10] utf8 nullterm\n"
            + "            };\n"
            + "            select h.Value\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<NullTermData>(f.Content) h"
        );

        // "Hi" followed by null and garbage
        var testData = "Hi\u0000XXXXXXX"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hi", table[0].ReadValue(0));
    }

    [TestMethod]
    public void Query_SelectInterpret_WithAsciiEncoding_ShouldParseCorrectly()
    {
        // Arrange: ASCII string parsing
        var query = (
            "\n"
            + "            binary AsciiData {\n"
            + "                Value: string[5] ascii\n"
            + "            };\n"
            + "            select h.Value\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<AsciiData>(f.Content) h"
        );

        var testData = "World"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("World", table[0].ReadValue(0));
    }

    #endregion
}
