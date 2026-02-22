using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: uint/ulong field values used as size references for string/byte[].
///     GenerateSizeExpression always casts to (int), which truncates uint values
///     above int.MaxValue (2,147,483,647) to negative numbers.
///     ReadString/ReadBytes will then throw with "Negative string size".
///     Root cause: InterpreterCodeGenerator.cs GenerateSizeExpression() line ~1279:
///     return $"(int){localVar}";  // narrowing cast for uint/ulong
///     Also tests: expressions in size references (e.g. string[Len * 2]).
/// </summary>
[TestClass]
public class BugProbe_SizeExpressionEdgeCaseTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: uint field used as string size, value fits in int (should work but tests the path).
    /// </summary>
    [TestMethod]
    public void Binary_UintSizeRefSmallValue_ShouldWorkForStringField()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((uint)3);
        bw.Write(Encoding.ASCII.GetBytes("ABC"));
        bw.Flush();

        var query = @"
            binary Pkt {
                Size: uint le,
                Data: string[Size] ascii
            };
            select p.Size, p.Data from #test.files() b
            cross apply Interpret(b.Content, 'Pkt') p";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((uint)3, table[0][0]);
        Assert.AreEqual("ABC", table[0][1]);
    }

    /// <summary>
    ///     BUG: ushort field used as byte array size.
    ///     Tests that ushort is properly cast to int for ReadBytes.
    /// </summary>
    [TestMethod]
    public void Binary_UshortSizeRefForByteArray_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((ushort)4);
        ms.Write([0xDE, 0xAD, 0xBE, 0xEF], 0, 4);

        var query = @"
            binary Chunk {
                Len: ushort le,
                Blob: byte[Len]
            };
            select c.Len from #test.files() b
            cross apply Interpret(b.Content, 'Chunk') c";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((ushort)4, table[0][0]);
    }

    /// <summary>
    ///     BUG: short field used as string size — negative short value causes
    ///     ReadString to receive negative length → exception thrown.
    /// </summary>
    [TestMethod]
    public void Binary_ShortSizeRefNegativeValue_ShouldThrowParseException()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((short)-1);
        bw.Write(new byte[16]); // padding
        bw.Flush();

        var query = @"
            binary Broken {
                Len: short le,
                Data: string[Len] ascii
            };
            select b.Len, b.Data from #test.files() f
            cross apply Interpret(f.Content, 'Broken') b";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Negative size should throw ParseException with clear error message
        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var ex = Assert.Throws<ParseException>(() =>
            vm.Run(CancellationToken.None));
        Assert.Contains("Negative string size", ex.Message,
            $"Expected 'Negative string size' in message but got: {ex.Message}");
    }

    /// <summary>
    ///     Test: int field used as string size — most common pattern, should work.
    /// </summary>
    [TestMethod]
    public void Binary_IntSizeRefForString_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(6);
        bw.Write(Encoding.UTF8.GetBytes("Musoq!"));
        bw.Flush();

        var query = @"
            binary Chunk {
                Len: int le,
                Name: string[Len] utf8
            };
            select c.Len, c.Name from #test.files() b
            cross apply Interpret(b.Content, 'Chunk') c";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(6, table[0][0]);
        Assert.AreEqual("Musoq!", table[0][1]);
    }

    /// <summary>
    ///     Test: expression-based size: string[Len - 1]
    ///     Uses arithmetic in size reference.
    /// </summary>
    [TestMethod]
    public void Binary_ExpressionSizeMinusOne_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        // Total length including a null terminator = 6, so string data = 5 chars
        bw.Write((byte)6);
        bw.Write(Encoding.ASCII.GetBytes("Hello"));
        bw.Write((byte)0x00); // null terminator (part of the 6 bytes total)
        bw.Flush();

        var query = @"
            binary Msg {
                TotalLen: byte,
                Text: string[TotalLen - 1] ascii
            };
            select m.TotalLen, m.Text from #test.files() b
            cross apply Interpret(b.Content, 'Msg') m";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)6, table[0][0]);
        Assert.AreEqual("Hello", table[0][1]);
    }

    /// <summary>
    ///     Test: expression-based size: string[Len * 2]
    ///     Length field contains half the actual string length.
    /// </summary>
    [TestMethod]
    public void Binary_ExpressionSizeMultiply_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((byte)3); // half-length
        bw.Write(Encoding.ASCII.GetBytes("ABCDEF")); // 3*2 = 6 chars
        bw.Flush();

        var query = @"
            binary Msg {
                HalfLen: byte,
                Text: string[HalfLen * 2] ascii
            };
            select m.HalfLen, m.Text from #test.files() b
            cross apply Interpret(b.Content, 'Msg') m";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        Assert.AreEqual("ABCDEF", table[0][1]);
    }

    /// <summary>
    ///     Test: long field used as byte array size (narrowing cast from long → int).
    ///     Value fits in int range so should work, but exercises the code path.
    /// </summary>
    [TestMethod]
    public void Binary_LongSizeRefSmallValue_ShouldWorkForByteArray()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(2L);
        ms.Write([0xAB, 0xCD], 0, 2);

        var query = @"
            binary Chunk {
                Size: long le,
                Data: byte[Size]
            };
            select c.Size from #test.files() b
            cross apply Interpret(b.Content, 'Chunk') c";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2L, table[0][0]);
    }
}
