using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class StressTestsInterpretationSchemasTests
{
    #region Step 8: Binary String Encodings Stress

    /// <summary>
    ///     Tests UTF-8 string with multibyte characters (emoji = 4 bytes).
    /// </summary>
    [TestMethod]
    public void Stress_Binary_Utf8FourByteChars_ShouldDecodeCorrectly()
    {
        var query = @"
            binary Data {
                Text: string[8] utf8
            };
            select s.Text from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        // "Hi" + smiley emoji (U+1F600 = 4 bytes) + "!" = 2+4+1 = 7 bytes, pad to 8
        var text = "Hi\U0001F600!";
        var bytes = Encoding.UTF8.GetBytes(text);
        var testData = new byte[8];
        Array.Copy(bytes, testData, Math.Min(bytes.Length, 8));

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        var result = (string)table[0][0];
        Assert.StartsWith("Hi", result);
    }

    /// <summary>
    ///     Tests ASCII string with all printable characters.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_AsciiFullPrintable_ShouldDecodeCorrectly()
    {
        var query = @"
            binary Data {
                Text: string[26] ascii
            };
            select s.Text from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        var testText = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var testData = Encoding.ASCII.GetBytes(testText);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(testText, table[0][0]);
    }

    /// <summary>
    ///     Tests nullterm with null in the middle of the buffer.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_NulltermInMiddle_ShouldTruncateAtNull()
    {
        var query = @"
            binary Data {
                Text: string[10] ascii nullterm
            };
            select s.Text from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        // "Hello\0XXXX" - null at position 5, rest is junk
        var testData = new byte[10];
        "Hello"u8.ToArray().CopyTo(testData, 0);
        testData[5] = 0;
        testData[6] = (byte)'X';
        testData[7] = (byte)'X';

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
    }

    /// <summary>
    ///     Tests nullterm with no null byte present - should return entire string.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_NulltermNoNullInBuffer_ShouldReturnFullString()
    {
        var query = @"
            binary Data {
                Text: string[5] ascii nullterm
            };
            select s.Text from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        var testData = "Hello"u8.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
    }

    /// <summary>
    ///     Tests empty string (zero-length).
    /// </summary>
    [TestMethod]
    public void Stress_Binary_EmptyString_ShouldReturnEmpty()
    {
        var query = @"
            binary Data {
                Prefix: byte,
                Text: string[0] utf8,
                Suffix: byte
            };
            select s.Prefix, s.Text, s.Suffix from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        var testData = new byte[] { 0x01, 0x02 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual("", table[0][1]);
        Assert.AreEqual((byte)2, table[0][2]);
    }

    /// <summary>
    ///     Tests multiple strings with different encodings in same schema.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_MultipleEncodings_ShouldDecodeAllCorrectly()
    {
        var query = @"
            binary Data {
                AsciiText: string[5] ascii,
                Utf8Text: string[5] utf8,
                Latin1Text: string[5] latin1
            };
            select s.AsciiText, s.Utf8Text, s.Latin1Text from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        using var ms = new MemoryStream();
        ms.Write("Hello"u8.ToArray());
        ms.Write("World"u8.ToArray());
        ms.Write(Encoding.Latin1.GetBytes("Test!"));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
        Assert.AreEqual("World", table[0][1]);
        Assert.AreEqual("Test!", table[0][2]);
    }

    /// <summary>
    ///     Tests string with trim modifier removes trailing padding.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_StringWithTrimAndPadding_ShouldTrimCorrectly()
    {
        var query = @"
            binary Data {
                Name: string[20] ascii trim,
                Code: string[10] ascii rtrim,
                Label: string[10] ascii ltrim
            };
            select s.Name, s.Code, s.Label from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        using var ms = new MemoryStream();
        // Name: "  Hello  " padded to 20 with spaces
        ms.Write("  Hello             "u8.ToArray());
        // Code: "ABC   " padded to 10
        ms.Write("ABC       "u8.ToArray());
        // Label: "   XYZ" padded to 10
        ms.Write("   XYZ    "u8.ToArray());

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]); // trim both sides
        Assert.AreEqual("ABC", table[0][1]); // rtrim only
        Assert.StartsWith("XYZ", (string)table[0][2]); // ltrim removes leading, trailing spaces may remain
    }

    /// <summary>
    ///     Tests UTF-16LE string decoding.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_Utf16LE_ShouldDecodeCorrectly()
    {
        var query = @"
            binary Data {
                Text: string[10] utf16le
            };
            select s.Text from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        // "Hello" in UTF-16LE = 10 bytes (2 per char)
        var testData = Encoding.Unicode.GetBytes("Hello");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
    }

    /// <summary>
    ///     Tests UTF-16BE string decoding.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_Utf16BE_ShouldDecodeCorrectly()
    {
        var query = @"
            binary Data {
                Text: string[10] utf16be
            };
            select s.Text from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        // "Hello" in UTF-16BE = 10 bytes
        var testData = Encoding.BigEndianUnicode.GetBytes("Hello");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
    }

    #endregion

    #region Step 9: Binary-Text Composition Edge Cases

    /// <summary>
    ///     Tests 'as' clause to parse binary string as text schema with multiple fields.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_AsTextWithMultipleFields_ShouldChainParse()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest
            };
            binary Packet {
                ConfigLen: byte,
                Config: string[ConfigLen] utf8 as KeyValue
            };
            select p.Config.Key, p.Config.Value from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var configStr = "host=localhost";
        using var ms = new MemoryStream();
        ms.WriteByte((byte)configStr.Length);
        ms.Write(Encoding.UTF8.GetBytes(configStr));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("host", table[0][0]);
        Assert.AreEqual("localhost", table[0][1]);
    }

    /// <summary>
    ///     Tests 'as' clause with empty string payload.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_AsTextWithEmptyString_ShouldHandleGracefully()
    {
        var query = @"
            text Data {
                Content: rest
            };
            binary Packet {
                Len: byte,
                Text: string[Len] utf8 as Data,
                Trailer: byte
            };
            select p.Len, p.Text.Content, p.Trailer from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        // Len=0 empty string, trailer=0xFF
        var testData = new byte[] { 0x00, 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.AreEqual("", table[0][1]);
        Assert.AreEqual((byte)0xFF, table[0][2]);
    }

    /// <summary>
    ///     Tests binary with multiple 'as' text fields in same schema.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_MultipleAsTextFields_ShouldParseAll()
    {
        var query = @"
            text NameValue {
                Name: until '=',
                Value: rest
            };
            binary Config {
                Entry1: string[10] utf8 as NameValue,
                Entry2: string[12] utf8 as NameValue
            };
            select c.Entry1.Name, c.Entry1.Value, c.Entry2.Name, c.Entry2.Value
            from #test.files() f
            cross apply Interpret<Config>(f.Content) c";

        using var ms = new MemoryStream();
        // Entry1 = "key1=val1 " (10 bytes)
        ms.Write("key1=val1 "u8.ToArray());
        // Entry2 = "key2=value2 " (12 bytes)
        ms.Write("key2=value2 "u8.ToArray());

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("key1", table[0][0]);
        Assert.AreEqual("val1 ", table[0][1]);
        Assert.AreEqual("key2", table[0][2]);
        Assert.AreEqual("value2 ", table[0][3]);
    }

    /// <summary>
    ///     Tests chaining: binary -> text with 'as' on a string field that uses pattern.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_AsTextWithPatternExtraction_ShouldChainParse()
    {
        var query = @"
            text VersionInfo {
                Major: pattern '\d+',
                _: pattern '\.',
                Minor: pattern '\d+',
                _: pattern '\.',
                Patch: rest
            };
            binary Header {
                Magic: int le,
                VersionStr: string[10] utf8 as VersionInfo
            };
            select h.Magic, h.VersionStr.Major, h.VersionStr.Minor, h.VersionStr.Patch
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(0xCAFE);
        ms.Write("12.34.5678"u8.ToArray());

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xCAFE, table[0][0]);
        Assert.AreEqual("12", table[0][1]);
        Assert.AreEqual("34", table[0][2]);
        Assert.AreEqual("5678", table[0][3]);
    }

    #endregion
}
