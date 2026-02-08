using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: SQL keywords used as field names in binary/text schemas.
///     The lexer matches SQL keywords BEFORE schema context, producing
///     TokenType.Case/Select/As/In/etc. ComposeIdentifierOrWord() only
///     handles Identifier, Word, and certain schema token types — NOT SQL
///     keyword token types. Result: SyntaxException on parse.
///
///     Root cause: KeywordLookup.TryGetKeyword wins over schema context
///     in Lexer.ScanIdentifierOrKeyword() (line ~324).
/// </summary>
[TestClass]
public class BugProbe_SqlKeywordFieldNameTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: 'Case' is TokenType.Case — not handled by ComposeIdentifierOrWord.
    ///     Expected: Field named 'Case' should be usable in a binary schema.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameCase_ShouldParse()
    {
        var query = @"
            binary Rec {
                Case: byte,
                Value: int le
            };
            select r.Case, r.Value from #test.files() b
            cross apply Interpret(b.Content, 'Rec') r";

        var testData = new byte[] { 0x01, 0x0A, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual(10, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'Desc' is TokenType.Desc — not handled by ComposeIdentifierOrWord.
    ///     Expected: Field named 'Desc' should be usable in a binary schema.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameDesc_ShouldParse()
    {
        var query = @"
            binary Rec {
                Desc: byte,
                Value: int le
            };
            select r.Desc, r.Value from #test.files() b
            cross apply Interpret(b.Content, 'Rec') r";

        var testData = new byte[] { 0x02, 0x14, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x02, table[0][0]);
        Assert.AreEqual(20, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'As' is TokenType.As — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameAs_ShouldParse()
    {
        var query = @"
            binary Rec {
                As: byte,
                Value: byte
            };
            select r.As, r.Value from #test.files() b
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
    ///     BUG: 'In' is TokenType.In — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameIn_ShouldParse()
    {
        var query = @"
            binary Pkt {
                In: short le,
                Out: short le
            };
            select p.In, p.Out from #test.files() b
            cross apply Interpret(b.Content, 'Pkt') p";

        var testData = new byte[] { 0x01, 0x00, 0x02, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)1, table[0][0]);
        Assert.AreEqual((short)2, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'On' is TokenType.On — not handled by ComposeIdentifierOrWord.
    ///     Common name for boolean flags.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameOn_ShouldParse()
    {
        var query = @"
            binary Flags {
                On: byte,
                Off: byte
            };
            select f.On, f.Off from #test.files() b
            cross apply Interpret(b.Content, 'Flags') f";

        var testData = new byte[] { 0x01, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((byte)0x00, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'Not' is TokenType.Not — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameNot_ShouldParse()
    {
        var query = @"
            binary Flags {
                Not: byte,
                Result: int le
            };
            select f.Not, f.Result from #test.files() b
            cross apply Interpret(b.Content, 'Flags') f";

        var testData = new byte[] { 0xFF, 0x05, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xFF, table[0][0]);
        Assert.AreEqual(5, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'Null' is TokenType.Null — not handled by ComposeIdentifierOrWord.
    ///     Common in format specs (e.g., NullPadding, NullTerminator).
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameNull_ShouldParse()
    {
        var query = @"
            binary Rec {
                Null: byte,
                Payload: int le
            };
            select r.Null, r.Payload from #test.files() b
            cross apply Interpret(b.Content, 'Rec') r";

        var testData = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x00, table[0][0]);
        Assert.AreEqual(1, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'True' is TokenType.True — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameTrue_ShouldParse()
    {
        var query = @"
            binary Bits {
                True: byte,
                False: byte
            };
            select b.True, b.False from #test.files() f
            cross apply Interpret(f.Content, 'Bits') b";

        var testData = new byte[] { 0x01, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((byte)0x00, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'Select' is TokenType.Select — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameSelect_ShouldParse()
    {
        var query = @"
            binary Rec {
                Select: byte,
                Data: byte
            };
            select r.Select, r.Data from #test.files() b
            cross apply Interpret(b.Content, 'Rec') r";

        var testData = new byte[] { 0x0A, 0x0B };
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

    /// <summary>
    ///     BUG: 'Where' is TokenType.Where — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameWhere_ShouldParse()
    {
        var query = @"
            binary Rec {
                Where: int le,
                What: int le
            };
            select r.Where, r.What from #test.files() b
            cross apply Interpret(b.Content, 'Rec') r";

        var testData = new byte[] { 0x0A, 0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table[0][0]);
        Assert.AreEqual(20, table[0][1]);
    }

    /// <summary>
    ///     BUG: 'And'/'Or' are SQL keywords — not handled in schema field names.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameAndOr_ShouldParse()
    {
        var query = @"
            binary Logic {
                And: byte,
                Or: byte
            };
            select l.And, l.Or from #test.files() b
            cross apply Interpret(b.Content, 'Logic') l";

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
    ///     BUG: 'Distinct' is TokenType.Distinct — not handled by ComposeIdentifierOrWord.
    /// </summary>
    [TestMethod]
    public void Binary_FieldNameDistinct_ShouldParse()
    {
        var query = @"
            binary Rec {
                Distinct: int le
            };
            select r.Distinct from #test.files() b
            cross apply Interpret(b.Content, 'Rec') r";

        var testData = new byte[] { 0x07, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(7, table[0][0]);
    }
}
