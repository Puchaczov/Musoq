using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

[TestClass]
public class BugProbe_InterpretationSchemasTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    /// Exact reproduction of the user-reported failing schema:
    /// binary Structure {
    ///     D: int le,
    ///     C: ushort le,
    ///     A: byte,
    ///     B: string[A] ascii
    /// }
    /// </summary>
    [TestMethod]
    public void Binary_ExactUserSchema_ShouldWork()
    {
        // Build binary: D=42, C=256, A=5, B="Hello"
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
        bw.Write((int)42);                                           // D (int LE)
        bw.Write((ushort)256);                                       // C (ushort LE)
        bw.Write((byte)5);                                           // A
        bw.Write(System.Text.Encoding.ASCII.GetBytes("Hello"));     // B (5 bytes)
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
            cross apply Interpret(b.Content, 'Structure') s";

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
    /// Minimal: just byte + string[Length] ascii
    /// </summary>
    [TestMethod]
    public void Binary_ByteThenStringVarLength_ShouldWork()
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
        bw.Write((byte)3);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("ABC"));
        bw.Flush();

        var query = @"
            binary Msg {
                Len: byte,
                Text: string[Len] ascii
            };
            select s.Len, s.Text
            from #test.files() b
            cross apply Interpret(b.Content, 'Msg') s";

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
    /// Minimal: byte + ushort le
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
            cross apply Interpret(b.Content, 'Hdr') s";

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
    /// Minimal: string[VarRef] ascii + ushort le
    /// </summary>
    [TestMethod]
    public void Binary_StringVarRefThenUshort_ShouldWork()
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
        bw.Write((byte)2);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("Hi"));
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
            cross apply Interpret(b.Content, 'Pkt') s";

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
    /// Minimal: string[VarRef] ascii + int le
    /// </summary>
    [TestMethod]
    public void Binary_StringVarRefThenInt_ShouldWork()
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
        bw.Write((byte)4);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("Test"));
        bw.Write((int)12345);
        bw.Flush();

        var query = @"
            binary Blk {
                Len: byte,
                Data: string[Len] ascii,
                Num: int le
            };
            select s.Len, s.Data, s.Num
            from #test.files() b
            cross apply Interpret(b.Content, 'Blk') s";

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
    /// All unsigned types: ushort, uint, ulong
    /// </summary>
    [TestMethod]
    public void Binary_AllUnsignedTypes_ShouldWork()
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
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
            cross apply Interpret(b.Content, 'UnsignedPack') s";

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
    /// sbyte type
    /// </summary>
    [TestMethod]
    public void Binary_SbyteType_ShouldWork()
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
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
            cross apply Interpret(b.Content, 'SignedByte') s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((sbyte)(-42), table[0][0]);
        Assert.AreEqual((byte)100, table[0][1]);
    }

    /// <summary>
    /// Float and double types
    /// </summary>
    [TestMethod]
    public void Binary_FloatDouble_ShouldWork()
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
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
            cross apply Interpret(b.Content, 'FloatPack') s";

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
    /// Big endian variants
    /// </summary>
    [TestMethod]
    public void Binary_BigEndianTypes_ShouldWork()
    {
        var testData = new byte[]
        {
            0x00, 0x0A,             // short be = 10
            0x00, 0x14,             // ushort be = 20
            0x00, 0x00, 0x00, 0x1E, // int be = 30
            0x00, 0x00, 0x00, 0x28  // uint be = 40
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
            cross apply Interpret(b.Content, 'BigEndianPack') s";

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
    /// String with ascii encoding (ebcdic removed - needs CodePages provider)
    /// </summary>
    [TestMethod]
    public void Binary_AsciiEncoding_ShouldWork()
    {
        var asciiBytes = System.Text.Encoding.ASCII.GetBytes("Abc");

        using var ms = new System.IO.MemoryStream();
        ms.Write(asciiBytes, 0, asciiBytes.Length);

        var query = @"
            binary AsciiStr {
                A: string[3] ascii
            };
            select s.A
            from #test.files() b
            cross apply Interpret(b.Content, 'AsciiStr') s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Abc", table[0][0]);
    }

    /// <summary>
    /// String with UTF-8 encoding
    /// </summary>
    [TestMethod]
    public void Binary_Utf8Encoding_ShouldWork()
    {
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes("Hello");

        using var ms = new System.IO.MemoryStream();
        ms.Write(utf8Bytes, 0, utf8Bytes.Length);

        var query = @"
            binary Utf8Str {
                A: string[5] utf8
            };
            select s.A
            from #test.files() b
            cross apply Interpret(b.Content, 'Utf8Str') s";

        var testData = ms.ToArray();
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
    /// String with UTF-16 Little Endian encoding
    /// </summary>
    [TestMethod]
    public void Binary_Utf16LeEncoding_ShouldWork()
    {
        var utf16LeBytes = System.Text.Encoding.Unicode.GetBytes("Test");

        using var ms = new System.IO.MemoryStream();
        ms.Write(utf16LeBytes, 0, utf16LeBytes.Length);

        var query = @"
            binary Utf16LeStr {
                A: string[8] utf16le
            };
            select s.A
            from #test.files() b
            cross apply Interpret(b.Content, 'Utf16LeStr') s";

        var testData = ms.ToArray();
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
    /// String with UTF-16 Big Endian encoding
    /// </summary>
    [TestMethod]
    public void Binary_Utf16BeEncoding_ShouldWork()
    {
        var utf16BeBytes = System.Text.Encoding.BigEndianUnicode.GetBytes("Data");

        using var ms = new System.IO.MemoryStream();
        ms.Write(utf16BeBytes, 0, utf16BeBytes.Length);

        var query = @"
            binary Utf16BeStr {
                A: string[8] utf16be
            };
            select s.A
            from #test.files() b
            cross apply Interpret(b.Content, 'Utf16BeStr') s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Data", table[0][0]);
    }

    /// <summary>
    /// String with Latin1 (ISO-8859-1) encoding
    /// </summary>
    [TestMethod]
    public void Binary_Latin1Encoding_ShouldWork()
    {
        var latin1Bytes = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes("Café");

        using var ms = new System.IO.MemoryStream();
        ms.Write(latin1Bytes, 0, latin1Bytes.Length);

        var query = @"
            binary Latin1Str {
                A: string[4] latin1
            };
            select s.A
            from #test.files() b
            cross apply Interpret(b.Content, 'Latin1Str') s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Café", table[0][0]);
    }

    /// <summary>
    /// String with trim modifier
    /// </summary>
    [TestMethod]
    public void Binary_StringTrimModifier_ShouldWork()
    {
        var paddedBytes = System.Text.Encoding.ASCII.GetBytes("  Test  ");

        using var ms = new System.IO.MemoryStream();
        ms.Write(paddedBytes, 0, paddedBytes.Length);

        var query = @"
            binary TrimStr {
                A: string[8] ascii trim
            };
            select s.A
            from #test.files() b
            cross apply Interpret(b.Content, 'TrimStr') s";

        var testData = ms.ToArray();
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
    /// String with rtrim modifier
    /// </summary>
    [TestMethod]
    public void Binary_StringRtrimModifier_ShouldWork()
    {
        var paddedBytes = System.Text.Encoding.ASCII.GetBytes("Data   ");

        using var ms = new System.IO.MemoryStream();
        ms.Write(paddedBytes, 0, paddedBytes.Length);

        var query = @"
            binary RtrimStr {
                A: string[7] ascii rtrim
            };
            select s.A
            from #test.files() b
            cross apply Interpret(b.Content, 'RtrimStr') s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Data", table[0][0]);
    }

    /// <summary>
    /// String with ltrim modifier
    /// </summary>
    [TestMethod]
    public void Binary_StringLtrimModifier_ShouldWork()
    {
        var paddedBytes = System.Text.Encoding.ASCII.GetBytes("   Code");

        using var ms = new System.IO.MemoryStream();
        ms.Write(paddedBytes, 0, paddedBytes.Length);

        var query = @"
            binary LtrimStr {
                A: string[7] ascii ltrim
            };
            select s.A
            from #test.files() b
            cross apply Interpret(b.Content, 'LtrimStr') s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Code", table[0][0]);
    }

    /// <summary>
    /// String with nullterm modifier
    /// </summary>
    [TestMethod]
    public void Binary_StringNulltermModifier_ShouldWork()
    {
        var nullTermBytes = new byte[10];
        var text = System.Text.Encoding.ASCII.GetBytes("Hi");
        System.Array.Copy(text, nullTermBytes, text.Length);
        // Rest is zeros (null terminators)

        using var ms = new System.IO.MemoryStream();
        ms.Write(nullTermBytes, 0, nullTermBytes.Length);

        var query = @"
            binary NulltermStr {
                A: string[10] ascii nullterm
            };
            select s.A
            from #test.files() b
            cross apply Interpret(b.Content, 'NulltermStr') s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hi", table[0][0]);
    }

    /// <summary>
    /// Multiple fields of same type in sequence (5 ints)
    /// </summary>
    [TestMethod]
    public void Binary_MultipleIntsInSequence_ShouldWork()
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
        bw.Write((int)1);
        bw.Write((int)2);
        bw.Write((int)3);
        bw.Write((int)4);
        bw.Write((int)5);
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
            cross apply Interpret(b.Content, 'FiveInts') s";

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
    /// Mix of ALL 10 primitive types in one schema
    /// </summary>
    [TestMethod]
    public void Binary_AllPrimitiveTypes_ShouldWork()
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
        bw.Write((byte)0x01);
        bw.Write((sbyte)-1);
        bw.Write((short)1000);
        bw.Write((ushort)2000);
        bw.Write((int)30000);
        bw.Write((uint)40000u);
        bw.Write((long)50000L);
        bw.Write((ulong)60000UL);
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
            cross apply Interpret(b.Content, 'AllTypes') s";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((sbyte)(-1), table[0][1]);
        Assert.AreEqual((short)1000, table[0][2]);
        Assert.AreEqual((ushort)2000, table[0][3]);
        Assert.AreEqual(30000, table[0][4]);
        Assert.AreEqual(40000u, table[0][5]);
        Assert.AreEqual(50000L, table[0][6]);
        Assert.AreEqual(60000UL, table[0][7]);
        Assert.AreEqual(1.5f, (float)table[0][8], 0.001f);
        Assert.AreEqual(2.5, (double)table[0][9], 0.001);
    }

    /// <summary>
    /// CTE Test: Interpret in FIRST CTE
    /// </summary>
    [TestMethod]
    public void Binary_InterpretInFirstCTE_ShouldWork()
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
        bw.Write((ushort)256);
        bw.Write((byte)5);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("Hello"));
        bw.Flush();

        var query = @"
            binary Structure {
                C: ushort le,
                A: byte,
                B: string[A] ascii
            };
            with ParsedData as (
                select s.C as UshortVal, s.A as ByteVal, s.B as StrData
                from #test.files() b
                cross apply Interpret(b.Content, 'Structure') s
            )
            select UshortVal, ByteVal, StrData
            from ParsedData";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((ushort)256, table[0][0]);
        Assert.AreEqual((byte)5, table[0][1]);
        Assert.AreEqual("Hello", table[0][2]);
    }

    /// <summary>
    /// CTE Test: Interpret in SECOND CTE (first CTE fetches files, second CTE interprets)
    /// </summary>
    [TestMethod]
    public void Binary_InterpretInSecondCTE_ShouldWork()
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
        bw.Write((byte)3);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("XYZ"));
        bw.Write((int)42);
        bw.Flush();

        var query = @"
            binary Packet {
                Len: byte,
                Text: string[Len] ascii,
                Count: int le
            };
            with FileData as (
                select b.Name as FileName, b.Content as FileContent
                from #test.files() b
            ),
            ParsedPackets as (
                select s.Len as PacketLen, s.Text as PacketText, s.Count as PacketCount
                from FileData f
                cross apply Interpret(f.FileContent, 'Packet') s
            )
            select PacketLen, PacketText, PacketCount
            from ParsedPackets";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        Assert.AreEqual("XYZ", table[0][1]);
        Assert.AreEqual(42, table[0][2]);
    }

    /// <summary>
    /// CTE Test: Interpret in FIRST CTE with complex user schema (all field types)
    /// </summary>
    [TestMethod]
    public void Binary_InterpretInFirstCTE_ComplexSchema_ShouldWork()
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
        bw.Write((int)42);
        bw.Write((ushort)256);
        bw.Write((byte)5);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("Hello"));
        bw.Flush();

        var query = @"
            binary Structure {
                D: int le,
                C: ushort le,
                A: byte,
                B: string[A] ascii
            };
            with BinaryRecords as (
                select s.D as IntField, s.C as UshortField, s.A as LenField, s.B as NameField
                from #test.files() b
                cross apply Interpret(b.Content, 'Structure') s
            )
            select IntField, UshortField, LenField, NameField
            from BinaryRecords
            where IntField > 40";

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
}
