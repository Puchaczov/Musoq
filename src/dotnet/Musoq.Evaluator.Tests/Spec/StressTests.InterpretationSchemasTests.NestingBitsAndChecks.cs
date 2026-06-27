using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class StressTestsInterpretationSchemasTests
{
    #region Step 5: Deep Nesting with Mixed Conditionals

    /// <summary>
    ///     Tests 3-level deep nested binary schemas with conditionals.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_DeepNestingWithConditionals_ShouldResolveAll()
    {
        var query = @"
            binary Inner {
                Value: int le
            };
            binary Middle {
                HasInner: byte,
                InnerData: Inner when HasInner <> 0
            };
            binary Outer {
                HasMiddle: byte,
                MiddleData: Middle when HasMiddle <> 0
            };
            select s.HasMiddle, s.MiddleData.HasInner, s.MiddleData.InnerData.Value
            from #test.files() f
            cross apply Interpret<Outer>(f.Content) s";

        // HasMiddle=1, HasInner=1, Value=42
        using var ms = new MemoryStream();
        ms.WriteByte(1); // HasMiddle = 1
        ms.WriteByte(1); // HasInner = 1
        ms.Write(BitConverter.GetBytes(42));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
        Assert.AreEqual(42, table[0][2]);
    }

    /// <summary>
    ///     Tests deep nesting where conditional is false at top level.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_DeepNestingConditionalFalse_ShouldReturnNulls()
    {
        var query = @"
            binary Inner {
                Value: int le
            };
            binary Middle {
                HasInner: byte,
                InnerData: Inner when HasInner <> 0,
                Trailer: byte
            };
            binary Outer {
                HasMiddle: byte,
                MiddleData: Middle when HasMiddle <> 0,
                Footer: int le
            };
            select s.HasMiddle, s.MiddleData, s.Footer
            from #test.files() f
            cross apply Interpret<Outer>(f.Content) s";

        // HasMiddle=0, Footer=99
        using var ms = new MemoryStream();
        ms.WriteByte(0); // HasMiddle = 0
        ms.Write(BitConverter.GetBytes(99));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual(99, table[0][2]);
    }

    /// <summary>
    ///     Tests nested schema arrays with conditional inner fields.
    ///     When an array element has a conditional field that evaluates to null,
    ///     the generated code should gracefully handle it by producing null values.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_NestedArrayWithConditionalInnerFields_ShouldParseAll()
    {
        var query = @"
            binary Item {
                Tag: byte,
                Payload: int le when Tag <> 0
            };
            binary Container {
                Count: byte,
                Items: Item[Count]
            };
            select i.Tag, i.Payload
            from #test.files() f
            cross apply Interpret<Container>(f.Content) c
            cross apply c.Items i
            order by i.Tag asc";

        using var ms = new MemoryStream();
        ms.WriteByte(3); // Count = 3

        // Item 1: Tag=1, Payload=100
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes(100));

        // Item 2: Tag=0, Payload=null (not written)
        ms.WriteByte(0);

        // Item 3: Tag=2, Payload=200
        ms.WriteByte(2);
        ms.Write(BitConverter.GetBytes(200));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual((byte)1, table[1][0]);
        Assert.AreEqual(100, table[1][1]);
        Assert.AreEqual((byte)2, table[2][0]);
        Assert.AreEqual(200, table[2][1]);
    }

    /// <summary>
    ///     Tests multiple levels of computed fields referencing nested fields.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_ComputedFromNested_ShouldEvaluate()
    {
        var query = @"
            binary Point {
                X: int le,
                Y: int le
            };
            binary Line {
                Start: Point,
                Finish: Point,
                DeltaX: Finish.X - Start.X,
                DeltaY: Finish.Y - Start.Y
            };
            select s.Start.X, s.Start.Y, s.Finish.X, s.Finish.Y, s.DeltaX, s.DeltaY
            from #test.files() f
            cross apply Interpret<Line>(f.Content) s";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(10); // Start.X
        bw.Write(20); // Start.Y
        bw.Write(50); // End.X
        bw.Write(80); // End.Y

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table[0][0]);
        Assert.AreEqual(20, table[0][1]);
        Assert.AreEqual(50, table[0][2]);
        Assert.AreEqual(80, table[0][3]);
        Assert.AreEqual(40, table[0][4]); // 50 - 10
        Assert.AreEqual(60, table[0][5]); // 80 - 20
    }

    /// <summary>
    ///     Tests inheritance with conditional in child schema.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_InheritanceWithConditionalChild_ShouldParseCorrectly()
    {
        var query = @"
            binary Base {
                MsgType: byte,
                Length: short le
            };
            binary ExtMessage extends Base {
                HasPayload: byte,
                Payload: byte[Length] when HasPayload <> 0
            };
            select s.MsgType, s.Length, s.HasPayload, s.Payload
            from #test.files() f
            cross apply Interpret<ExtMessage>(f.Content) s";

        using var ms = new MemoryStream();
        ms.WriteByte(0x01); // MsgType
        ms.Write(BitConverter.GetBytes((short)3)); // Length = 3
        ms.WriteByte(1); // HasPayload = 1
        ms.Write([0xAA, 0xBB, 0xCC]); // Payload (3 bytes)

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((short)3, table[0][1]);
        Assert.AreEqual((byte)1, table[0][2]);
        var payload = (byte[])table[0][3];
        Assert.HasCount(3, payload);
        Assert.AreEqual((byte)0xAA, payload[0]);
    }

    #endregion

    #region Step 6: Binary Bit Fields Crossing Byte Boundaries

    /// <summary>
    ///     Tests bit fields that don't fit neatly in one byte (5+5+6 = 16 bits = 2 bytes).
    /// </summary>
    [TestMethod]
    public void Stress_Binary_BitFieldsCrossBytesBoundary_ShouldParse()
    {
        var query = @"
            binary Data {
                A: bits[5],
                B: bits[5],
                C: bits[6]
            };
            select s.A, s.B, s.C from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        // 16 bits total:
        // A = 5 bits from bit 0-4 of byte 0
        // B = 5 bits from bit 5-7 of byte 0 + bit 0-1 of byte 1
        // C = 6 bits from bit 2-7 of byte 1
        // Let's set: A=31 (11111), B=21 (10101), C=42 (101010)
        // Byte 0: bits 0-4 = A=31=11111, bits 5-7 = B low 3 bits: 101 -> byte0 = 10111111 = 0xBF
        // Byte 1: bits 0-1 = B high 2 bits: 10, bits 2-7 = C=42=101010 -> byte1 = 10101010 = 0xAA
        var testData = new byte[] { 0xBF, 0xAA };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)31, table[0][0]);
        Assert.AreEqual((byte)21, table[0][1]);
        Assert.AreEqual((byte)42, table[0][2]);
    }

    /// <summary>
    ///     Tests a 12-bit field that spans two bytes.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_BitField12Bits_ShouldParse()
    {
        var query = @"
            binary Data {
                Value: bits[12],
                Remainder: bits[4]
            };
            select s.Value, s.Remainder from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        // 12+4 = 16 bits = 2 bytes
        // Value = 12 bits from bit 0-11 = 0xABC = 2748
        // Remainder = 4 bits from bit 12-15 = 0xD = 13
        // byte 0: bits 0-7 = low 8 bits of Value = 0xBC
        // byte 1: bits 0-3 = high 4 bits of Value = 0xA, bits 4-7 = Remainder = 0xD
        // byte 1 = 0xDA
        var testData = new byte[] { 0xBC, 0xDA };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((ushort)0xABC, table[0][0]);
        Assert.AreEqual((byte)0xD, table[0][1]);
    }

    /// <summary>
    ///     Tests alignment after odd bit count followed by byte field.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_BitFieldAlignmentBeforeByte_ShouldAlign()
    {
        var query = @"
            binary Data {
                Flags: bits[3],
                _: align[8],
                NextByte: byte
            };
            select s.Flags, s.NextByte from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        // Flags = 3 bits from bit 0-2 of byte 0 = 5 (101)
        // align[8] skips remaining 5 bits of byte 0
        // NextByte = byte 1 = 0x42
        var testData = new byte[] { 0x05, 0x42 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)5, table[0][0]);
        Assert.AreEqual((byte)0x42, table[0][1]);
    }

    /// <summary>
    ///     Tests mixed bit fields and regular byte fields in sequence.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_BitFieldsThenBytesThenBitFields_ShouldParse()
    {
        var query = @"
            binary Data {
                A: bits[4],
                B: bits[4],
                C: byte,
                D: bits[2],
                E: bits[6]
            };
            select s.A, s.B, s.C, s.D, s.E from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        // A=0xF(15), B=0x0(0) -> byte0 = 0x0F
        // C=0x42 -> byte1
        // D=3(11), E=0(000000) -> byte2 = 0x03
        var testData = new byte[] { 0x0F, 0x42, 0x03 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xF, table[0][0]); // low nibble
        Assert.AreEqual((byte)0x0, table[0][1]); // high nibble
        Assert.AreEqual((byte)0x42, table[0][2]);
        Assert.AreEqual((byte)3, table[0][3]); // low 2 bits
        Assert.AreEqual((byte)0, table[0][4]); // high 6 bits
    }

    #endregion

    #region Step 7: Binary Check Constraint Failures

    /// <summary>
    ///     Tests that Interpret (not TryInterpret) throws on check constraint failure.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_CheckConstraintFails_InterpretShouldThrow()
    {
        var query = @"
            binary Data {
                Magic: int le check Magic = 0xDEADBEEF
            };
            select s.Magic from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        // Wrong magic value
        var testData = BitConverter.GetBytes(0x12345678);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);

        Assert.Throws<Exception>(() => _ = vm.Run(CancellationToken.None).Count);
    }

    /// <summary>
    ///     Tests check constraint with range validation.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_CheckConstraintRange_ValidValue_ShouldPass()
    {
        var query = @"
            binary Data {
                Version: short le check Version >= 1 and Version <= 10
            };
            select s.Version from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        var testData = BitConverter.GetBytes((short)5);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)5, table[0][0]);
    }

    /// <summary>
    ///     Tests check constraint referencing an earlier field.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_CheckConstraintReferencingEarlierField_ShouldValidate()
    {
        var query = @"
            binary Data {
                MaxLen: int le,
                ActualLen: int le check ActualLen <= MaxLen
            };
            select s.MaxLen, s.ActualLen from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(100); // MaxLen
        bw.Write(50); // ActualLen <= MaxLen

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100, table[0][0]);
        Assert.AreEqual(50, table[0][1]);
    }

    /// <summary>
    ///     Tests TryInterpret returns null on check failure and filters with OUTER APPLY.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_TryInterpretWithCheckFailure_ShouldReturnNull()
    {
        var query = @"
            binary Data {
                Magic: int le check Magic = 0xDEADBEEF
            };
            select f.Name, s.Magic from #test.files() f
            outer apply TryInterpret<Data>(f.Content) s";

        var entities = new[]
        {
            new BinaryEntity { Name = "good.bin", Content = BitConverter.GetBytes(unchecked((int)0xDEADBEEF)) },
            new BinaryEntity { Name = "bad.bin", Content = BitConverter.GetBytes(0x12345678) }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
    }

    /// <summary>
    ///     Tests multiple check constraints on different fields.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_MultipleCheckConstraints_AllPass_ShouldSucceed()
    {
        var query = @"
            binary Data {
                Magic: int le check Magic = 42,
                Version: byte check Version >= 1,
                Flags: byte check Flags <> 0
            };
            select s.Magic, s.Version, s.Flags from #test.files() f
            cross apply Interpret<Data>(f.Content) s";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(42); // Magic
        bw.Write((byte)3); // Version
        bw.Write((byte)1); // Flags

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
        Assert.AreEqual((byte)3, table[0][1]);
        Assert.AreEqual((byte)1, table[0][2]);
    }

    #endregion
}
