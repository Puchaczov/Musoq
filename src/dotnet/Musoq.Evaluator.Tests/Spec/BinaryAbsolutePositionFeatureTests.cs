using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Binary Schema Absolute Positioning (Section 4.8 of specification).
///     Tests the 'at' clause for reading fields at specific offsets.
/// </summary>
[TestClass]
public class BinaryAbsolutePositionFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 4.8: Hex Offset

    /// <summary>
    ///     Tests reading a field at a hexadecimal offset.
    /// </summary>
    [TestMethod]
    public void Binary_AtClause_HexOffset_ShouldReadAtHexPosition()
    {
        var query = @"
            binary Data { 
                Value: byte at 0x04
            };
            select d.Value from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xAB };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xAB, table[0][0]);
    }

    #endregion

    #region Section 4.8: Field-Referenced Offset

    /// <summary>
    ///     Tests reading a field at an offset stored in another field.
    /// </summary>
    [TestMethod]
    public void Binary_AtClause_FieldReference_ShouldUseDynamicOffset()
    {
        var query = @"
            binary FileHeader { 
                DataOffset: int le,
                Data: int le at DataOffset
            };
            select h.DataOffset, h.Data from #test.files() b
            cross apply Interpret(b.Content, 'FileHeader') h";

        var testData = new byte[]
        {
            0x08, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF,
            0x2A, 0x00, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(8, table[0][0]);
        Assert.AreEqual(42, table[0][1]);
    }

    #endregion

    #region Section 4.8: Expression-Based Offset

    /// <summary>
    ///     Tests reading a field at a computed offset expression.
    /// </summary>
    [TestMethod]
    public void Binary_AtClause_ExpressionOffset_ShouldComputePosition()
    {
        var query = @"
            binary Data { 
                BaseOffset: int le,
                Value: short le at BaseOffset + 2
            };
            select d.BaseOffset, d.Value from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[]
        {
            0x04, 0x00, 0x00, 0x00,
            0xFF, 0xFF,
            0x2A, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(4, table[0][0]);
        Assert.AreEqual((short)42, table[0][1]);
    }

    #endregion

    #region Section 4.8: Backward Jump

    /// <summary>
    ///     Tests reading a field that jumps backward to re-read earlier data.
    /// </summary>
    [TestMethod]
    public void Binary_AtClause_BackwardJump_ShouldRereadData()
    {
        var query = @"
            binary Data { 
                First: int le,
                Second: int le,
                FirstAgain: int le at 0
            };
            select d.First, d.Second, d.FirstAgain from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[]
        {
            0x01, 0x00, 0x00, 0x00,
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
        Assert.AreEqual(1, table[0][2]);
    }

    #endregion

    #region Section 4.8: Multiple At Clauses

    /// <summary>
    ///     Tests multiple fields each reading at specific offsets.
    /// </summary>
    [TestMethod]
    public void Binary_AtClause_MultipleFields_ShouldReadEachAtOffset()
    {
        var query = @"
            binary PeHeader { 
                DosMagic: short le at 0,
                PeOffset: int le at 4,
                Signature: int le at 8
            };
            select h.DosMagic, h.PeOffset, h.Signature from #test.files() b
            cross apply Interpret(b.Content, 'PeHeader') h";

        var testData = new byte[]
        {
            0x4D, 0x5A,
            0x00, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x50, 0x45, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)0x5A4D, table[0][0]);
        Assert.AreEqual(8, table[0][1]);
        Assert.AreEqual(0x00004550, table[0][2]);
    }

    #endregion

    #region Section 4.8: Literal Offset

    /// <summary>
    ///     Tests reading a field at a fixed integer offset.
    /// </summary>
    [TestMethod]
    public void Binary_AtClause_LiteralOffset_ShouldReadAtPosition()
    {
        var query = @"
            binary Data { 
                Value: int le at 4
            };
            select d.Value from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[]
        {
            0xFF, 0xFF, 0xFF, 0xFF,
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

    /// <summary>
    ///     Tests reading a field at offset 0 explicitly.
    /// </summary>
    [TestMethod]
    public void Binary_AtClause_ZeroOffset_ShouldReadFromStart()
    {
        var query = @"
            binary Data { 
                Value: short le at 0
            };
            select d.Value from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[] { 0x0A, 0x00, 0xFF, 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)10, table[0][0]);
    }

    #endregion

    #region Section 4.8: At Clause Combined with When

    /// <summary>
    ///     Tests at clause combined with conditional when clause.
    /// </summary>
    [TestMethod]
    public void Binary_AtClause_CombinedWithWhen_ShouldApplyBoth()
    {
        var query = @"
            binary Data { 
                HasExtra: byte,
                Value: int le,
                ExtraValue: int le at 8 when HasExtra <> 0
            };
            select d.HasExtra, d.Value, d.ExtraValue from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[]
        {
            0x01,
            0x0A, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF,
            0x14, 0x00, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual(20, table[0][2]);
    }

    /// <summary>
    ///     Tests at clause with when=false should yield null and not advance cursor.
    /// </summary>
    [TestMethod]
    public void Binary_AtClause_CombinedWithWhenFalse_ShouldBeNull()
    {
        var query = @"
            binary Data { 
                HasExtra: byte,
                Value: int le,
                ExtraValue: int le at 8 when HasExtra <> 0
            };
            select d.HasExtra, d.Value, d.ExtraValue from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[]
        {
            0x00,
            0x0A, 0x00, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.IsNull(table[0][2]);
    }

    #endregion
}
