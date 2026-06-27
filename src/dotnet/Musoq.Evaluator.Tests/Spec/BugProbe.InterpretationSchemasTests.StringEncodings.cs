using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class BugProbeInterpretationSchemasTests
{
    [TestMethod]
    public void Binary_AsciiEncoding_ShouldWork()
    {
        var asciiBytes = "Abc"u8.ToArray();

        using var ms = new MemoryStream();
        ms.Write(asciiBytes, 0, asciiBytes.Length);

        var query = @"
            binary AsciiStr {
                A: string[3] ascii
            };
            select s.A
            from #test.files() b
            cross apply Interpret<AsciiStr>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Abc", table[0][0]);
    }

    /// <summary>
    ///     String with UTF-8 encoding
    /// </summary>

    [TestMethod]
    public void Binary_Utf8Encoding_ShouldWork()
    {
        var utf8Bytes = "Hello"u8.ToArray();

        using var ms = new MemoryStream();
        ms.Write(utf8Bytes, 0, utf8Bytes.Length);

        var query = @"
            binary Utf8Str {
                A: string[5] utf8
            };
            select s.A
            from #test.files() b
            cross apply Interpret<Utf8Str>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
    }

    /// <summary>
    ///     String with UTF-16 Little Endian encoding
    /// </summary>

    [TestMethod]
    public void Binary_Utf16LeEncoding_ShouldWork()
    {
        var utf16LeBytes = Encoding.Unicode.GetBytes("Test");

        using var ms = new MemoryStream();
        ms.Write(utf16LeBytes, 0, utf16LeBytes.Length);

        var query = @"
            binary Utf16LeStr {
                A: string[8] utf16le
            };
            select s.A
            from #test.files() b
            cross apply Interpret<Utf16LeStr>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test", table[0][0]);
    }

    /// <summary>
    ///     String with UTF-16 Big Endian encoding
    /// </summary>

    [TestMethod]
    public void Binary_Utf16BeEncoding_ShouldWork()
    {
        var utf16BeBytes = Encoding.BigEndianUnicode.GetBytes("Data");

        using var ms = new MemoryStream();
        ms.Write(utf16BeBytes, 0, utf16BeBytes.Length);

        var query = @"
            binary Utf16BeStr {
                A: string[8] utf16be
            };
            select s.A
            from #test.files() b
            cross apply Interpret<Utf16BeStr>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Data", table[0][0]);
    }

    /// <summary>
    ///     String with Latin1 (ISO-8859-1) encoding
    /// </summary>

    [TestMethod]
    public void Binary_Latin1Encoding_ShouldWork()
    {
        var latin1Bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes("Café");

        using var ms = new MemoryStream();
        ms.Write(latin1Bytes, 0, latin1Bytes.Length);

        var query = @"
            binary Latin1Str {
                A: string[4] latin1
            };
            select s.A
            from #test.files() b
            cross apply Interpret<Latin1Str>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Café", table[0][0]);
    }

    /// <summary>
    ///     String with trim modifier
    /// </summary>

    [TestMethod]
    public void Binary_StringTrimModifier_ShouldWork()
    {
        var paddedBytes = "  Test  "u8.ToArray();

        using var ms = new MemoryStream();
        ms.Write(paddedBytes, 0, paddedBytes.Length);

        var query = @"
            binary TrimStr {
                A: string[8] ascii trim
            };
            select s.A
            from #test.files() b
            cross apply Interpret<TrimStr>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test", table[0][0]);
    }

    /// <summary>
    ///     String with rtrim modifier
    /// </summary>

    [TestMethod]
    public void Binary_StringRtrimModifier_ShouldWork()
    {
        var paddedBytes = "Data   "u8.ToArray();

        using var ms = new MemoryStream();
        ms.Write(paddedBytes, 0, paddedBytes.Length);

        var query = @"
            binary RtrimStr {
                A: string[7] ascii rtrim
            };
            select s.A
            from #test.files() b
            cross apply Interpret<RtrimStr>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Data", table[0][0]);
    }

    /// <summary>
    ///     String with ltrim modifier
    /// </summary>

    [TestMethod]
    public void Binary_StringLtrimModifier_ShouldWork()
    {
        var paddedBytes = "   Code"u8.ToArray();

        using var ms = new MemoryStream();
        ms.Write(paddedBytes, 0, paddedBytes.Length);

        var query = @"
            binary LtrimStr {
                A: string[7] ascii ltrim
            };
            select s.A
            from #test.files() b
            cross apply Interpret<LtrimStr>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Code", table[0][0]);
    }

    /// <summary>
    ///     String with nullterm modifier
    /// </summary>

    [TestMethod]
    public void Binary_StringNulltermModifier_ShouldWork()
    {
        var nullTermBytes = new byte[10];
        var text = "Hi"u8.ToArray();
        Array.Copy(text, nullTermBytes, text.Length);
        // Rest is zeros (null terminators)

        using var ms = new MemoryStream();
        ms.Write(nullTermBytes, 0, nullTermBytes.Length);

        var query = @"
            binary NulltermStr {
                A: string[10] ascii nullterm
            };
            select s.A
            from #test.files() b
            cross apply Interpret<NulltermStr>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hi", table[0][0]);
    }
}
