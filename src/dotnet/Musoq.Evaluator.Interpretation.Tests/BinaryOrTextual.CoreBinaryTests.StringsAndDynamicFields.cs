using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
    [TestMethod]
    public void Query_SelectInterpret_WithString_ShouldParseUtf8String()
    {
        // Arrange
        var query = (
            "\n"
            + "            binary NameRecord {\n"
            + "                Name: string[5] utf8\n"
            + "            };\n"
            + "            select\n"
            + "                h.Name\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<NameRecord>(f.Content) h"
        );

        var testData = "Hello"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "name.bin", Content = testData },
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
    public void Query_SelectInterpret_WithStringTrim_ShouldTrimWhitespace()
    {
        // Arrange
        var query = (
            "\n"
            + "            binary PaddedRecord {\n"
            + "                Value: string[10] utf8 trim\n"
            + "            };\n"
            + "            select\n"
            + "                h.Value\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<PaddedRecord>(f.Content) h"
        );

        // "Hello" followed by spaces to make 10 bytes
        var testData = "Hello     "u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "padded.bin", Content = testData },
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
    public void Query_SelectInterpret_WithDynamicByteArray_ShouldUsePreviousField()
    {
        // Arrange
        var query = (
            "\n"
            + "            binary LengthPrefixedData {\n"
            + "                Length: short le,\n"
            + "                Data: byte[Length]\n"
            + "            };\n"
            + "            select\n"
            + "                h.Length,\n"
            + "                h.Data\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<LengthPrefixedData>(f.Content) h"
        );

        // Length=4 (little-endian), followed by 4 bytes of data
        var testData = new byte[] { 0x04, 0x00, 0xAA, 0xBB, 0xCC, 0xDD };
        var entities = new[]
        {
            new BinaryEntity { Name = "prefixed.bin", Content = testData },
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
        Assert.AreEqual((short)4, table[0].ReadValue(0));
        var data = (byte[])table[0].ReadValue(1);
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
        var query = (
            "\n"
            + "            binary StringRecord {\n"
            + "                NameLen: byte,\n"
            + "                Name: string[NameLen] utf8\n"
            + "            };\n"
            + "            select\n"
            + "                h.NameLen,\n"
            + "                h.Name\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<StringRecord>(f.Content) h"
        );

        // NameLen=5, followed by "Hello"
        var testData = new byte[] { 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var entities = new[]
        {
            new BinaryEntity { Name = "name.bin", Content = testData },
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
        Assert.AreEqual((byte)5, table[0].ReadValue(0));
        Assert.AreEqual("Hello", table[0].ReadValue(1));
    }

    [TestMethod]
    public void Query_SelectInterpret_WithNullTermString_ShouldStopAtNull()
    {
        // Arrange
        var query = (
            "\n"
            + "            binary CStringRecord {\n"
            + "                Name: string[10] utf8 nullterm\n"
            + "            };\n"
            + "            select\n"
            + "                h.Name\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<CStringRecord>(f.Content) h"
        );

        // "Hello" followed by null and garbage bytes
        var testData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF };
        var entities = new[]
        {
            new BinaryEntity { Name = "cstring.bin", Content = testData },
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
    public void Query_SelectInterpret_WithAsciiString_ShouldParseCorrectly()
    {
        // Arrange
        var query = (
            "\n"
            + "            binary AsciiRecord {\n"
            + "                Label: string[4] ascii\n"
            + "            };\n"
            + "            select\n"
            + "                h.Label\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<AsciiRecord>(f.Content) h"
        );

        var testData = "TEST"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "ascii.bin", Content = testData },
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
        Assert.AreEqual("TEST", table[0].ReadValue(0));
    }
}
