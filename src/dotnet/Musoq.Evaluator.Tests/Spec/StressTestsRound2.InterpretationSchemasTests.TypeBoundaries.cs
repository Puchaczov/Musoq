using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class StressTestsRound2InterpretationSchemasTests
{
    #region Category 2: Type Boundary & Precision

    /// <summary>
    ///     Standalone big-endian double precision test.
    /// </summary>
    [TestMethod]
    public void R2_Binary_DoubleBigEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary Rec { Val: double be };
            select r.Val
            from #test.files() f
            cross apply Interpret<Rec>(f.Content) r";

        var value = 3.141592653589793;
        var bytes = BitConverter.GetBytes(value);
        Array.Reverse(bytes); // convert to big-endian

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = bytes } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(value, (double)table[0][0], 1e-15);
    }

    /// <summary>
    ///     sbyte boundary values: -128 and 127.
    /// </summary>
    [TestMethod]
    public void R2_Binary_SByteBoundaries_ShouldParseCorrectly()
    {
        var query = @"
            binary Rec { Lo: sbyte, Hi: sbyte };
            select r.Lo, r.Hi
            from #test.files() f
            cross apply Interpret<Rec>(f.Content) r";

        var data = new byte[] { 0x80, 0x7F }; // -128, 127
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((sbyte)-128, table[0][0]);
        Assert.AreEqual((sbyte)127, table[0][1]);
    }

    /// <summary>
    ///     uint max value: 4294967295.
    /// </summary>
    [TestMethod]
    public void R2_Binary_UIntMaxValue_ShouldParseCorrectly()
    {
        var query = @"
            binary Rec { Val: uint le };
            select r.Val
            from #test.files() f
            cross apply Interpret<Rec>(f.Content) r";

        var data = BitConverter.GetBytes(uint.MaxValue);
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(uint.MaxValue, table[0][0]);
    }

    /// <summary>
    ///     Float positive infinity.
    /// </summary>
    [TestMethod]
    public void R2_Binary_FloatPositiveInfinity_ShouldParseCorrectly()
    {
        var query = @"
            binary Rec { Val: float le };
            select r.Val
            from #test.files() f
            cross apply Interpret<Rec>(f.Content) r";

        var data = BitConverter.GetBytes(float.PositiveInfinity);
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(float.PositiveInfinity, table[0][0]);
    }

    /// <summary>
    ///     Computed field from mixed types: byte + short should widen correctly.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ComputedMixedTypes_ShouldWiden()
    {
        var query = @"
            binary Rec {
                A: byte,
                B: short le,
                Total: A + B
            };
            select r.A, r.B, r.Total
            from #test.files() f
            cross apply Interpret<Rec>(f.Content) r";

        using var ms = new MemoryStream();
        ms.WriteByte(200);
        ms.Write(BitConverter.GetBytes((short)300));
        var data = ms.ToArray();

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)200, table[0][0]);
        Assert.AreEqual((short)300, table[0][1]);
        // 200 + 300 = 500
        Assert.AreEqual(500, Convert.ToInt32(table[0][2]));
    }

    /// <summary>
    ///     Single bit fields used as boolean-like flags: all zeros, all ones.
    /// </summary>
    [TestMethod]
    public void R2_Binary_SingleBitBooleanFlags_ShouldParse()
    {
        var query = @"
            binary Flags {
                A: bits[1],
                B: bits[1],
                C: bits[1],
                D: bits[1],
                E: bits[1],
                F: bits[1],
                G: bits[1],
                H: bits[1]
            };
            select f.A, f.B, f.C, f.D, f.E, f.F, f.G, f.H
            from #test.files() fil
            cross apply Interpret<Flags>(fil.Content) f";

        // Byte 0xA5 = 10100101 → bits LSB first: A=1, B=0, C=1, D=0, E=0, F=1, G=0, H=1
        var data = new byte[] { 0xA5 };
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]); // A
        Assert.AreEqual((byte)0, table[0][1]); // B
        Assert.AreEqual((byte)1, table[0][2]); // C
        Assert.AreEqual((byte)0, table[0][3]); // D
        Assert.AreEqual((byte)0, table[0][4]); // E
        Assert.AreEqual((byte)1, table[0][5]); // F
        Assert.AreEqual((byte)0, table[0][6]); // G
        Assert.AreEqual((byte)1, table[0][7]); // H
    }

    #endregion
}
