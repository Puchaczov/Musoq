using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Schema-specific keywords used as field names in binary/text schemas.
///     When in schema context, words like "Check", "At", "Trim", "Le", "Be", "Int" etc.
///     get schema-specific token types. ComposeIdentifierOrWord() only handles SOME
///     schema tokens but NOT: Le, Be, ByteType, ShortType, IntType, UIntType, etc.,
///     Check, At, Trim, RTrim, LTrim, NullTerm, Utf8, Ascii, Latin1, Ebcdic,
///     Nested, Escaped, Greedy, Lazy, Lower, Upper, Capture, Extends.
///
///     Root cause: ComposeIdentifierOrWord switch only handles ~13 schema token types,
///     but SchemaKeywordTypes defines ~40 schema keywords.
/// </summary>
[TestClass]
public class BugProbe_SchemaKeywordFieldNameTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: 'Check' is a schema keyword — not handled by ComposeIdentifierOrWord.
    ///     Common name for checksum/validation fields in binary formats.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameCheck_ShouldParse()
    {
        var query = @"
            binary Pkt {
                Header: int le,
                Check: int le
            };
            select p.Header, p.Check from #test.files() b
            cross apply Interpret(b.Content, 'Pkt') p";

        var testData = new byte[] { 0x01, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual(255, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'At' is a schema keyword — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameAt_ShouldParse()
    {
        var query = @"
            binary Rec {
                At: int le,
                Length: int le
            };
            select r.At, r.Length from #test.files() b
            cross apply Interpret(b.Content, 'Rec') r";

        var testData = new byte[] { 0x10, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(16, table[0][0]);
        Assert.AreEqual(32, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'Trim' is a schema keyword — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameTrim_ShouldParse()
    {
        var query = @"
            binary Rec {
                Trim: byte,
                Payload: byte
            };
            select r.Trim, r.Payload from #test.files() b
            cross apply Interpret(b.Content, 'Rec') r";

        var testData = new byte[] { 0x01, 0x02 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((byte)0x02, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'Nested' is a schema keyword — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameNested_ShouldParse()
    {
        var query = @"
            binary Rec {
                Nested: int le,
                Depth: int le
            };
            select r.Nested, r.Depth from #test.files() b
            cross apply Interpret(b.Content, 'Rec') r";

        var testData = new byte[] { 0x03, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, table[0][0]);
        Assert.AreEqual(5, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'Extends' is a schema keyword — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameExtends_ShouldParse()
    {
        var query = @"
            binary Rec {
                Extends: byte,
                Type: byte
            };
            select r.Extends, r.Type from #test.files() b
            cross apply Interpret(b.Content, 'Rec') r";

        var testData = new byte[] { 0xAA, 0xBB };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xAA, table[0][0]);
        Assert.AreEqual((byte)0xBB, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'Lower' is a schema keyword — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameLower_ShouldParse()
    {
        var query = @"
            binary Bounds {
                Lower: int le,
                Upper: int le
            };
            select b.Lower, b.Upper from #test.files() f
            cross apply Interpret(f.Content, 'Bounds') b";

        var testData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x7F };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0, table[0][0]);
        Assert.AreEqual(int.MaxValue, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'Capture' is a schema keyword — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameCapture_ShouldParse()
    {
        var query = @"
            binary Frame {
                Capture: int le,
                Length: int le
            };
            select f.Capture, f.Length from #test.files() b
            cross apply Interpret(b.Content, 'Frame') f";

        var testData = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual(64, table[0][1]);
    }
}
