using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: nullterm modifier with UTF-16 encodings.
///     BytesInterpreterBase.ReadNullTerminatedString searches for a single 0x00 byte
///     as the null terminator. For UTF-16, the null terminator is 0x00 0x00 (two bytes).
///     A single 0x00 byte is the HIGH byte of many valid UTF-16 characters
///     (e.g., 'A' in UTF-16LE is 0x41 0x00), causing premature truncation.
///     Root cause: BytesInterpreterBase.cs line ~383:
///     var nullIndex = bytes.IndexOf((byte)0);  // Wrong for UTF-16
/// </summary>
[TestClass]
public class BugProbe_NulltermUtf16Tests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: UTF-16LE nullterm string truncated at first 0x00 byte.
    ///     "AB" in UTF-16LE = 0x41 0x00 0x42 0x00 — the 0x00 after 'A'
    ///     is mistakenly treated as null terminator, returning just "A".
    ///     Expected: Read until 0x00 0x00 (double-byte null terminator).
    /// </summary>
    [TestMethod]
    public void Binary_NulltermUtf16Le_ShouldNotTruncateAtHighByte()
    {
        // "Hello" in UTF-16LE + null terminator (0x00 0x00) + padding
        var text = Encoding.Unicode.GetBytes("Hello");
        var testData = new byte[64];
        Array.Copy(text, testData, text.Length);
        // Remaining bytes are 0x00 — the double 0x00 after "Hello" is the real terminator

        var query = @"
            binary Msg {
                Label: string[64] utf16le nullterm
            };
            select m.Label from #test.files() b
            cross apply Interpret(b.Content, 'Msg') m";

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0],
            "nullterm with UTF-16LE should find double-byte null terminator, not single 0x00");
    }

    /// <summary>
    ///     BUG: UTF-16BE nullterm string truncated at first 0x00 byte.
    ///     "A" in UTF-16BE = 0x00 0x41 — the very first byte is 0x00,
    ///     so the string would be truncated to empty string.
    ///     Expected: Read until 0x00 0x00 aligned on 2-byte boundary.
    /// </summary>
    [TestMethod]
    public void Binary_NulltermUtf16Be_ShouldNotTruncateAtHighByte()
    {
        // "Hi" in UTF-16BE = 0x00 0x48 0x00 0x69 + null terminator 0x00 0x00
        var text = Encoding.BigEndianUnicode.GetBytes("Hi");
        var testData = new byte[32];
        Array.Copy(text, testData, text.Length);
        // Rest is 0x00 padding

        var query = @"
            binary Msg {
                Label: string[32] utf16be nullterm
            };
            select m.Label from #test.files() b
            cross apply Interpret(b.Content, 'Msg') m";

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hi", table[0][0],
            "nullterm with UTF-16BE should find double-byte null terminator, not single 0x00");
    }

    /// <summary>
    ///     Verify that nullterm works correctly with ASCII (1-byte encoding).
    ///     This should work correctly since single 0x00 is the right terminator.
    /// </summary>
    [TestMethod]
    public void Binary_NulltermAscii_ShouldWorkCorrectly()
    {
        // "Test" + 0x00 + garbage
        var testData = new byte[] { 0x54, 0x65, 0x73, 0x74, 0x00, 0xFF, 0xFF, 0xFF };

        var query = @"
            binary Msg {
                Label: string[8] ascii nullterm
            };
            select m.Label from #test.files() b
            cross apply Interpret(b.Content, 'Msg') m";

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test", table[0][0]);
    }
}
