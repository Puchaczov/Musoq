using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: String encodings latin1, utf16le, utf16be.
///     Per spec section 4.2.4, these encodings are supported.
///     No E2E coverage exists for latin1, utf16le, or utf16be.
/// </summary>
[TestClass]
public class BugProbe_StringEncodingTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     PROBE: latin1 (ISO-8859-1) string encoding.
    ///     Spec 4.2.4: "latin1 - ISO-8859-1, 1 byte per char"
    ///     Expected: Bytes decoded as latin1 characters.
    /// </summary>
    [TestMethod]
    public void Binary_Latin1Encoding_ShouldDecodeProperly()
    {
        var query = @"
            binary Data { 
                Name: string[5] latin1
            };
            select d.Name from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        // "Hello" in ISO-8859-1
        var testData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
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
    ///     PROBE: latin1 with extended characters (>127).
    ///     Spec 4.2.4: latin1 supports values 128-255 (accented chars, etc.)
    /// </summary>
    [TestMethod]
    public void Binary_Latin1ExtendedChars_ShouldDecodeCorrectly()
    {
        var query = @"
            binary Data { 
                Name: string[4] latin1
            };
            select d.Name from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        // "café" in ISO-8859-1: c=0x63, a=0x61, f=0x66, é=0xE9
        var testData = new byte[] { 0x63, 0x61, 0x66, 0xE9 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("café", table[0][0]);
    }

    /// <summary>
    ///     PROBE: utf16le string encoding.
    ///     Spec 4.2.4: "utf16le - UTF-16 Little Endian, 2 bytes per char"
    ///     Note: size in string[N] is in BYTES.
    /// </summary>
    [TestMethod]
    public void Binary_Utf16LeEncoding_ShouldDecodeProperly()
    {
        var query = @"
            binary Data { 
                Name: string[8] utf16le
            };
            select d.Name from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        // "Test" in UTF-16LE: T=0x54,0x00 e=0x65,0x00 s=0x73,0x00 t=0x74,0x00
        var testData = Encoding.Unicode.GetBytes("Test");
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
    ///     PROBE: utf16be string encoding.
    ///     Spec 4.2.4: "utf16be - UTF-16 Big Endian, 2 bytes per char"
    /// </summary>
    [TestMethod]
    public void Binary_Utf16BeEncoding_ShouldDecodeProperly()
    {
        var query = @"
            binary Data { 
                Name: string[6] utf16be
            };
            select d.Name from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        // "Hi!" in UTF-16BE: H=0x00,0x48 i=0x00,0x69 !=0x00,0x21
        var testData = Encoding.BigEndianUnicode.GetBytes("Hi!");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hi!", table[0][0]);
    }
}
