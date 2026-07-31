using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
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
            cross apply Interpret<NameRecord>(f.Content) h";

        var testData = "Hello"u8.ToArray();
        var entities = new[] { new BinaryEntity { Name = "name.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
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
            cross apply Interpret<PaddedRecord>(f.Content) h";

        // "Hello" followed by spaces to make 10 bytes
        var testData = "Hello     "u8.ToArray();
        var entities = new[] { new BinaryEntity { Name = "padded.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
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
            cross apply Interpret<LengthPrefixedData>(f.Content) h";

        // Length=4 (little-endian), followed by 4 bytes of data
        var testData = new byte[] { 0x04, 0x00, 0xAA, 0xBB, 0xCC, 0xDD };
        var entities = new[] { new BinaryEntity { Name = "prefixed.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
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
            cross apply Interpret<StringRecord>(f.Content) h";

        // NameLen=5, followed by "Hello"
        var testData = new byte[] { 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var entities = new[] { new BinaryEntity { Name = "name.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
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
            cross apply Interpret<CStringRecord>(f.Content) h";

        // "Hello" followed by null and garbage bytes
        var testData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF };
        var entities = new[] { new BinaryEntity { Name = "cstring.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
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
            cross apply Interpret<AsciiRecord>(f.Content) h";

        var testData = "TEST"u8.ToArray();
        var entities = new[] { new BinaryEntity { Name = "ascii.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("TEST", table[0][0]);
    }
}
