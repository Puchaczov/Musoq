using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Binary Schema Bit Fields (Section 4.7 of specification).
///     Tests bits[N] for sub-byte field access and align[N] for byte alignment.
/// </summary>
[TestClass]
public class BinaryBitFieldsFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 4.7: Bit Fields with Alignment

    /// <summary>
    ///     Tests align[8] directive to force byte alignment after bit fields.
    /// </summary>
    [TestMethod]
    public void Binary_BitField_WithAlignment_ShouldSkipToNextByte()
    {
        var query = @"
            binary Record { 
                Flags: bits[3],
                _: align[8],
                NextByte: byte
            };
            select r.Flags, r.NextByte from #test.files() b
            cross apply Interpret(b.Content, 'Record') r";

        var testData = new byte[] { 0x05, 0xAB };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)5, table[0][0]);
        Assert.AreEqual((byte)0xAB, table[0][1]);
    }

    #endregion

    #region Section 4.7: Bit Fields with Regular Fields

    /// <summary>
    ///     Tests bit fields mixed with regular byte fields.
    /// </summary>
    [TestMethod]
    public void Binary_BitField_MixedWithRegularFields_ShouldParseAll()
    {
        var query = @"
            binary Packet { 
                Version: bits[4],
                HeaderLen: bits[4],
                Protocol: byte,
                Length: short le
            };
            select p.Version, p.HeaderLen, p.Protocol, p.Length 
            from #test.files() b
            cross apply Interpret(b.Content, 'Packet') p";

        var testData = new byte[] { 0x54, 0x06, 0x40, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)4, table[0][0]);
        Assert.AreEqual((byte)5, table[0][1]);
        Assert.AreEqual((byte)0x06, table[0][2]);
        Assert.AreEqual((short)0x0040, table[0][3]);
    }

    #endregion

    #region Section 4.7: Bit Fields in WHERE Clause

    /// <summary>
    ///     Tests filtering on bit field values.
    /// </summary>
    [TestMethod]
    public void Binary_BitField_InWhereClause_ShouldFilter()
    {
        var query = @"
            binary Flags { 
                Readable: bits[1],
                Writable: bits[1],
                Executable: bits[1]
            };
            select f.Readable, f.Writable, f.Executable
            from #test.files() b
            cross apply Interpret(b.Content, 'Flags') f
            where f.Writable = 1";

        var entities = new[]
        {
            new BinaryEntity { Name = "rw.bin", Content = [0b011] },
            new BinaryEntity { Name = "ro.bin", Content = [0b001] },
            new BinaryEntity { Name = "rwx.bin", Content = [0b111] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Section 4.7: Basic Single Bit

    /// <summary>
    ///     Tests reading a single bit from a byte.
    /// </summary>
    [TestMethod]
    public void Binary_BitField_SingleBit_ShouldReadLeastSignificant()
    {
        var query = @"
            binary Flags { Flag0: bits[1] };
            select f.Flag0 from #test.files() b
            cross apply Interpret(b.Content, 'Flags') f";

        var testData = new byte[] { 0x01 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
    }

    /// <summary>
    ///     Tests reading a single bit that is zero.
    /// </summary>
    [TestMethod]
    public void Binary_BitField_SingleBitZero_ShouldReturnZero()
    {
        var query = @"
            binary Flags { Flag0: bits[1] };
            select f.Flag0 from #test.files() b
            cross apply Interpret(b.Content, 'Flags') f";

        var testData = new byte[] { 0xFE };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
    }

    #endregion

    #region Section 4.7: Multiple Bit Fields in a Byte

    /// <summary>
    ///     Tests reading multiple bit fields within a single byte.
    /// </summary>
    [TestMethod]
    public void Binary_BitField_MultipleBits_ShouldReadSequentially()
    {
        var query = @"
            binary Flags { 
                Low: bits[4],
                High: bits[4] 
            };
            select f.Low, f.High from #test.files() b
            cross apply Interpret(b.Content, 'Flags') f";

        var testData = new byte[] { 0xAB };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x0B, table[0][0]);
        Assert.AreEqual((byte)0x0A, table[0][1]);
    }

    /// <summary>
    ///     Tests reading three bit fields from a single byte.
    /// </summary>
    [TestMethod]
    public void Binary_BitField_ThreeBitFields_ShouldPackCorrectly()
    {
        var query = @"
            binary Data { 
                A: bits[2],
                B: bits[3],
                C: bits[3]
            };
            select d.A, d.B, d.C from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        // 0b11_101_011 = 0xEB
        // bits[2] A reads bits 0-1: 11 = 3
        // bits[3] B reads bits 2-4: 010 = 2  (from 0b11_101_[01]1 -> reading sequentially)
        // bits[3] C reads bits 5-7: 111 = 7
        // Let's use a clearer byte: 0b_CCC_BBB_AA
        // 0xEB = 0b11101011
        // A = bits[0:1] = 11 = 3
        // B = bits[2:4] = 010 = 2
        // C = bits[5:7] = 111 = 7
        var testData = new byte[] { 0xEB };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        Assert.AreEqual((byte)2, table[0][1]);
        Assert.AreEqual((byte)7, table[0][2]);
    }

    #endregion
}
