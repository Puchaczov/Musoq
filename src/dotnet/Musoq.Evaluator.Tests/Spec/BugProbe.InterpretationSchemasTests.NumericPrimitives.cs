using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class BugProbeInterpretationSchemasTests
{
    [TestMethod]
    public void Binary_ExactUserSchema_ShouldWork()
    {
        // Build binary: D=42, C=256, A=5, B="Hello"
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(42); // D (int LE)
        bw.Write((ushort)256); // C (ushort LE)
        bw.Write((byte)5); // A
        bw.Write("Hello"u8.ToArray()); // B (5 bytes)
        bw.Flush();

        var query = @"
            binary Structure {
                D: int le,
                C: ushort le,
                A: byte,
                B: string[A] ascii
            };
            select s.D, s.C, s.A, s.B
            from #test.files() b
            cross apply Interpret<Structure>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
        Assert.AreEqual((ushort)256, table[0][1]);
        Assert.AreEqual((byte)5, table[0][2]);
        Assert.AreEqual("Hello", table[0][3]);
    }

    /// <summary>
    ///     Minimal: just byte + string[Length] ascii
    /// </summary>

    [TestMethod]
    public void Binary_ByteThenStringVarLength_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((byte)3);
        bw.Write("ABC"u8.ToArray());
        bw.Flush();

        var query = @"
            binary Msg {
                Len: byte,
                Text: string[Len] ascii
            };
            select s.Len, s.Text
            from #test.files() b
            cross apply Interpret<Msg>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        Assert.AreEqual("ABC", table[0][1]);
    }

    /// <summary>
    ///     Minimal: byte + ushort le
    /// </summary>

    [TestMethod]
    public void Binary_ByteThenUshort_ShouldWork()
    {
        var query = @"
            binary Hdr {
                Tag: byte,
                Val: ushort le
            };
            select s.Tag, s.Val
            from #test.files() b
            cross apply Interpret<Hdr>(b.Content) s";

        var testData = new byte[] { 0xFF, 0x34, 0x12 }; // byte + ushort LE 0x1234
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xFF, table[0][0]);
        Assert.AreEqual((ushort)0x1234, table[0][1]);
    }

    /// <summary>
    ///     Minimal: string[VarRef] ascii + ushort le
    /// </summary>

    [TestMethod]
    public void Binary_StringVarRefThenUshort_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((byte)2);
        bw.Write("Hi"u8.ToArray());
        bw.Write((ushort)999);
        bw.Flush();

        var query = @"
            binary Pkt {
                Len: byte,
                Name: string[Len] ascii,
                Code: ushort le
            };
            select s.Len, s.Name, s.Code
            from #test.files() b
            cross apply Interpret<Pkt>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual("Hi", table[0][1]);
        Assert.AreEqual((ushort)999, table[0][2]);
    }

    /// <summary>
    ///     Minimal: string[VarRef] ascii + int le
    /// </summary>

    [TestMethod]
    public void Binary_StringVarRefThenInt_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((byte)4);
        bw.Write("Test"u8.ToArray());
        bw.Write(12345);
        bw.Flush();

        var query = @"
            binary Blk {
                Len: byte,
                Data: string[Len] ascii,
                Num: int le
            };
            select s.Len, s.Data, s.Num
            from #test.files() b
            cross apply Interpret<Blk>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)4, table[0][0]);
        Assert.AreEqual("Test", table[0][1]);
        Assert.AreEqual(12345, table[0][2]);
    }

    /// <summary>
    ///     All unsigned types: ushort, uint, ulong
    /// </summary>

    [TestMethod]
    public void Binary_AllUnsignedTypes_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((ushort)1000);
        bw.Write((uint)100000);
        bw.Write((ulong)10000000000);
        bw.Flush();

        var query = @"
            binary UnsignedPack {
                A: ushort le,
                B: uint le,
                C: ulong le
            };
            select s.A, s.B, s.C
            from #test.files() b
            cross apply Interpret<UnsignedPack>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((ushort)1000, table[0][0]);
        Assert.AreEqual((uint)100000, table[0][1]);
        Assert.AreEqual((ulong)10000000000, table[0][2]);
    }

    /// <summary>
    ///     sbyte type
    /// </summary>

    [TestMethod]
    public void Binary_SbyteType_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((sbyte)-42);
        bw.Write((byte)100);
        bw.Flush();

        var query = @"
            binary SignedByte {
                Neg: sbyte,
                Pos: byte
            };
            select s.Neg, s.Pos
            from #test.files() b
            cross apply Interpret<SignedByte>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((sbyte)-42, table[0][0]);
        Assert.AreEqual((byte)100, table[0][1]);
    }

    /// <summary>
    ///     Float and double types
    /// </summary>

    [TestMethod]
    public void Binary_FloatDouble_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(3.14f);
        bw.Write(2.71828);
        bw.Flush();

        var query = @"
            binary FloatPack {
                F: float le,
                D: double le
            };
            select s.F, s.D
            from #test.files() b
            cross apply Interpret<FloatPack>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3.14f, (float)table[0][0], 0.001f);
        Assert.AreEqual(2.71828, (double)table[0][1], 0.0001);
    }

    /// <summary>
    ///     Big endian variants
    /// </summary>

    [TestMethod]
    public void Binary_BigEndianTypes_ShouldWork()
    {
        var testData = new byte[]
        {
            0x00, 0x0A, // short be = 10
            0x00, 0x14, // ushort be = 20
            0x00, 0x00, 0x00, 0x1E, // int be = 30
            0x00, 0x00, 0x00, 0x28 // uint be = 40
        };

        var query = @"
            binary BigEndianPack {
                A: short be,
                B: ushort be,
                C: int be,
                D: uint be
            };
            select s.A, s.B, s.C, s.D
            from #test.files() b
            cross apply Interpret<BigEndianPack>(b.Content) s";

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)10, table[0][0]);
        Assert.AreEqual((ushort)20, table[0][1]);
        Assert.AreEqual(30, table[0][2]);
        Assert.AreEqual((uint)40, table[0][3]);
    }

    /// <summary>
    ///     String with ascii encoding (ebcdic removed - needs CodePages provider)
    /// </summary>

    [TestMethod]
    public void Binary_MultipleIntsInSequence_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(1);
        bw.Write(2);
        bw.Write(3);
        bw.Write(4);
        bw.Write(5);
        bw.Flush();

        var query = @"
            binary FiveInts {
                A: int le,
                B: int le,
                C: int le,
                D: int le,
                E: int le
            };
            select s.A, s.B, s.C, s.D, s.E
            from #test.files() b
            cross apply Interpret<FiveInts>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual(2, table[0][1]);
        Assert.AreEqual(3, table[0][2]);
        Assert.AreEqual(4, table[0][3]);
        Assert.AreEqual(5, table[0][4]);
    }

    /// <summary>
    ///     Mix of ALL 10 primitive types in one schema
    /// </summary>

    [TestMethod]
    public void Binary_AllPrimitiveTypes_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((byte)0x01);
        bw.Write((sbyte)-1);
        bw.Write((short)1000);
        bw.Write((ushort)2000);
        bw.Write(30000);
        bw.Write(40000u);
        bw.Write(50000L);
        bw.Write(60000UL);
        bw.Write(1.5f);
        bw.Write(2.5);
        bw.Flush();

        var query = @"
            binary AllTypes {
                A: byte,
                B: sbyte,
                C: short le,
                D: ushort le,
                E: int le,
                F: uint le,
                G: long le,
                H: ulong le,
                I: float le,
                J: double le
            };
            select s.A, s.B, s.C, s.D, s.E, s.F, s.G, s.H, s.I, s.J
            from #test.files() b
            cross apply Interpret<AllTypes>(b.Content) s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((sbyte)-1, table[0][1]);
        Assert.AreEqual((short)1000, table[0][2]);
        Assert.AreEqual((ushort)2000, table[0][3]);
        Assert.AreEqual(30000, table[0][4]);
        Assert.AreEqual(40000u, table[0][5]);
        Assert.AreEqual(50000L, table[0][6]);
        Assert.AreEqual(60000UL, table[0][7]);
        Assert.AreEqual(1.5f, (float)table[0][8], 0.001f);
        Assert.AreEqual(2.5, (double)table[0][9], 0.001);
    }
}
