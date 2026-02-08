using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Binary and Text Schema Discard Fields (Section 4.11)
///     and Text Literal Fields (Section 5.5 of specification).
///     Tests unnamed discard fields that consume bytes/text without exposing them.
/// </summary>
[TestClass]
public class BinaryDiscardFieldsFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 4.11: Discard Byte Array

    /// <summary>
    ///     Tests skipping bytes with a discard field using byte array (named '_').
    /// </summary>
    [TestMethod]
    public void Binary_Discard_ByteArray_ShouldConsumeButNotExpose()
    {
        var query = @"
            binary Data { 
                Magic: int le,
                _: byte[4],
                Value: int le
            };
            select d.Magic, d.Value from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[]
        {
            0x01, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF,
            0x02, 0x00, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual(2, table[0][1]);
    }

    #endregion

    #region Section 4.11: Discard Single Byte

    /// <summary>
    ///     Tests skipping a single byte discard field.
    /// </summary>
    [TestMethod]
    public void Binary_Discard_SingleByte_ShouldSkipOneByte()
    {
        var query = @"
            binary Data { 
                First: byte,
                _: byte,
                Second: byte
            };
            select d.First, d.Second from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[] { 0x0A, 0xFF, 0x0B };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x0A, table[0][0]);
        Assert.AreEqual((byte)0x0B, table[0][1]);
    }

    #endregion

    #region Section 4.11: Discard Int

    /// <summary>
    ///     Tests skipping a 4-byte integer discard field.
    /// </summary>
    [TestMethod]
    public void Binary_Discard_IntLe_ShouldSkipFourBytes()
    {
        var query = @"
            binary Data { 
                _: int le,
                Value: short le
            };
            select d.Value from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[]
        {
            0xFF, 0xFF, 0xFF, 0xFF,
            0x34, 0x12
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)0x1234, table[0][0]);
    }

    #endregion

    #region Section 4.11: Discard at Start

    /// <summary>
    ///     Tests discard as the first field in a schema.
    /// </summary>
    [TestMethod]
    public void Binary_Discard_AtStart_ShouldSkipLeadingBytes()
    {
        var query = @"
            binary Data { 
                _: byte[8],
                Value: int le
            };
            select d.Value from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x2A, 0x00, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
    }

    #endregion

    #region Section 4.11: Discard at End

    /// <summary>
    ///     Tests discard as the last field in a schema.
    /// </summary>
    [TestMethod]
    public void Binary_Discard_AtEnd_ShouldSkipTrailingBytes()
    {
        var query = @"
            binary Data { 
                Value: int le,
                _: byte[4]
            };
            select d.Value from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[]
        {
            0x05, 0x00, 0x00, 0x00,
            0xDE, 0xAD, 0xBE, 0xEF
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5, table[0][0]);
    }

    #endregion

    #region Section 5.5: Text Literal Combined with Between

    /// <summary>
    ///     Tests text schema with literal followed by between.
    /// </summary>
    [TestMethod]
    public void Text_Literal_CombinedWithBetween_ShouldWork()
    {
        var query = @"
            text Data { 
                _: literal 'start:',
                Value: between '[' ']',
                _: literal ':end'
            };
            select d.Value from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "start:[hello]:end" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0][0]);
    }

    #endregion

    #region Section 5.5: Text Literal at Start

    /// <summary>
    ///     Tests text schema with literal at the beginning of the line.
    /// </summary>
    [TestMethod]
    public void Text_Literal_AtStart_ShouldConsumePrefix()
    {
        var query = @"
            text Data { 
                _: literal 'PREFIX:',
                Value: rest
            };
            select d.Value from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "PREFIX:the remaining text" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("the remaining text", table[0][0]);
    }

    #endregion
}
