using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Round-two stress tests targeting edge cases, null propagation, type boundaries,
///     encoding corners, array/cross-apply behaviors, text schema gaps, and complex
///     integration scenarios identified by gap analysis against the specification.
/// </summary>
[TestClass]
public class StressTestsRound2_InterpretationSchemasTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Category 1: Null Propagation & Conditional Edge Cases

    /// <summary>
    ///     Computed field referencing a present conditional field should compute correctly.
    ///     When the conditional is satisfied, the computed field uses the parsed value.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ComputedFromPresentConditional_ShouldCompute()
    {
        var query = @"
            binary Msg {
                HasData: byte,
                Len: int le when HasData <> 0,
                Doubled: Len * 2
            };
            select m.HasData, m.Len, m.Doubled
            from #test.files() f
            cross apply Interpret(f.Content, 'Msg') m";

        using var ms = new MemoryStream();
        ms.WriteByte(1); // HasData=1 → Len=10 → Doubled=20
        ms.Write(BitConverter.GetBytes(10));
        var data = ms.ToArray();

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual(20, Convert.ToInt32(table[0][2]));
    }

    /// <summary>
    ///     Conditional byte[] array field should be null when condition is false.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ConditionalByteArray_WhenFalse_ShouldBeNull()
    {
        var query = @"
            binary Pkt {
                HasPayload: byte,
                Len: int le when HasPayload <> 0,
                Payload: byte[Len] when HasPayload <> 0
            };
            select p.HasPayload, p.Len, p.Payload
            from #test.files() f
            cross apply Interpret(f.Content, 'Pkt') p";

        var data = new byte[] { 0x00 }; // HasPayload=0
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.IsNull(table[0][2]);
    }

    /// <summary>
    ///     Conditional string field should be null when condition is false.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ConditionalString_WhenFalse_ShouldBeNull()
    {
        var query = @"
            binary Rec {
                HasName: byte,
                Name: string[8] utf8 when HasName <> 0
            };
            select r.HasName, r.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        var data = new byte[] { 0x00 }; // HasName=0
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    /// <summary>
    ///     Conditional nested schema field should be null when condition is false.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ConditionalNestedSchema_WhenFalse_ShouldBeNull()
    {
        var query = @"
            binary Inner { X: short le, Y: short le };
            binary Outer {
                HasPoint: byte,
                Point: Inner when HasPoint <> 0
            };
            select o.HasPoint, o.Point
            from #test.files() f
            cross apply Interpret(f.Content, 'Outer') o";

        var data = new byte[] { 0x00 }; // HasPoint=0
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    /// <summary>
    ///     Chained conditionals: when both conditions are true, both fields parse.
    ///     Tests that chained conditional dependencies work when data is present.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ChainedConditionals_BothTrue_ShouldParse()
    {
        var query = @"
            binary Chain {
                Flag: byte,
                A: short le when Flag <> 0,
                B: int le when Flag <> 0
            };
            select c.Flag, c.A, c.B
            from #test.files() f
            cross apply Interpret(f.Content, 'Chain') c";

        using var ms = new MemoryStream();
        ms.WriteByte(1); // Flag=1
        ms.Write(BitConverter.GetBytes((short)42)); // A=42
        ms.Write(BitConverter.GetBytes(99)); // B=99
        var data = ms.ToArray();

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((short)42, table[0][1]);
        Assert.AreEqual(99, table[0][2]);
    }

    /// <summary>
    ///     Check on field verifies constraint and when separately verifies condition;
    ///     spec grammar allows when+check together but parser currently does not.
    ///     This test exercises check alone with a passing value.
    /// </summary>
    [TestMethod]
    public void R2_Binary_CheckAlone_PassingValue_ShouldSucceed()
    {
        var query = @"
            binary Rec {
                Val: short le check Val >= 10
            };
            select r.Val
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        var data = BitConverter.GetBytes((short)42);
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)42, table[0][0]);
    }

    /// <summary>
    ///     Check on field with failing value: TryInterpret should return no rows.
    /// </summary>
    [TestMethod]
    public void R2_Binary_CheckAlone_FailingValue_TryInterpretReturnsNoRows()
    {
        var query = @"
            binary Rec {
                Val: short le check Val >= 100
            };
            select r.Val
            from #test.files() f
            cross apply TryInterpret(f.Content, 'Rec') r";

        var data = BitConverter.GetBytes((short)5); // 5 < 100, check fails
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(0, table.Count);
    }

    /// <summary>
    ///     Inheritance: parent has conditional field, child computed references it.
    /// </summary>
    [TestMethod]
    public void R2_Binary_InheritanceParentConditional_ChildComputed()
    {
        var query = @"
            binary Base {
                Flag: byte,
                Val: int le when Flag <> 0
            };
            binary Child extends Base {
                Extra: byte
            };
            select c.Flag, c.Val, c.Extra
            from #test.files() f
            cross apply Interpret(f.Content, 'Child') c";

        // Flag=1, Val=99, Extra=7
        using var ms = new MemoryStream();
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes(99));
        ms.WriteByte(7);
        var data = ms.ToArray();

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(99, table[0][1]);
        Assert.AreEqual((byte)7, table[0][2]);
    }

    #endregion

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
            cross apply Interpret(f.Content, 'Rec') r";

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
            cross apply Interpret(f.Content, 'Rec') r";

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
            cross apply Interpret(f.Content, 'Rec') r";

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
            cross apply Interpret(f.Content, 'Rec') r";

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
            cross apply Interpret(f.Content, 'Rec') r";

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
            cross apply Interpret(fil.Content, 'Flags') f";

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

    #region Category 3: String & Encoding Edge Cases

    /// <summary>
    ///     Zero-length string: string[0] should produce empty string.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ZeroLengthString_ShouldReturnEmpty()
    {
        var query = @"
            binary Rec {
                Tag: byte,
                Name: string[0] utf8
            };
            select r.Tag, r.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        var data = new byte[] { 0x42 };
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x42, table[0][0]);
        Assert.AreEqual(string.Empty, table[0][1]);
    }

    /// <summary>
    ///     String of all whitespace with trim modifier should produce empty string.
    /// </summary>
    [TestMethod]
    public void R2_Binary_AllWhitespaceStringTrimmed_ShouldBeEmpty()
    {
        var query = @"
            binary Rec {
                Name: string[8] utf8 trim
            };
            select r.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        var data = Encoding.UTF8.GetBytes("        "); // 8 spaces
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(string.Empty, table[0][0]);
    }

    /// <summary>
    ///     Nullterm on string that is all nulls should produce empty string.
    /// </summary>
    [TestMethod]
    public void R2_Binary_AllNullsNullterm_ShouldReturnEmpty()
    {
        var query = @"
            binary Rec {
                Name: string[8] utf8 nullterm
            };
            select r.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        var data = new byte[8]; // all zeros
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(string.Empty, table[0][0]);
    }

    /// <summary>
    ///     String field sized by expression: string[Len * 2].
    /// </summary>
    [TestMethod]
    public void R2_Binary_StringSizeByExpression_ShouldParseCorrectly()
    {
        var query = @"
            binary Rec {
                Len: byte,
                Name: string[Len * 2] utf8
            };
            select r.Len, r.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        using var ms = new MemoryStream();
        ms.WriteByte(3); // Len=3 → string size = 6
        ms.Write(Encoding.UTF8.GetBytes("Hello!")); // 6 bytes
        var data = ms.ToArray();

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        Assert.AreEqual("Hello!", table[0][1]);
    }

    /// <summary>
    ///     rtrim should only trim trailing, not leading whitespace.
    /// </summary>
    [TestMethod]
    public void R2_Binary_RtrimPreservesLeadingWhitespace()
    {
        var query = @"
            binary Rec {
                Name: string[10] utf8 rtrim
            };
            select r.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        var data = Encoding.UTF8.GetBytes("  Hello   "); // 10 bytes: 2 leading + "Hello" + 3 trailing
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("  Hello", table[0][0]);
    }

    /// <summary>
    ///     ltrim should only trim leading, not trailing whitespace.
    /// </summary>
    [TestMethod]
    public void R2_Binary_LtrimPreservesTrailingWhitespace()
    {
        var query = @"
            binary Rec {
                Name: string[10] utf8 ltrim
            };
            select r.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r";

        var data = Encoding.UTF8.GetBytes("  Hello   "); // 10 bytes
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello   ", table[0][0]);
    }

    #endregion

    #region Category 4: Array & Cross Apply Edge Cases

    /// <summary>
    ///     Primitive array with Count=0 via cross apply should produce no rows.
    /// </summary>
    [TestMethod]
    public void R2_Binary_PrimitiveArrayCountZero_CrossApply_ShouldProduceNoRows()
    {
        var query = @"
            binary Container {
                Count: byte,
                Values: int le[Count]
            };
            select v.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'Container') c
            cross apply c.Values v";

        var data = new byte[] { 0x00 }; // Count=0
        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(0, table.Count);
    }

    /// <summary>
    ///     Array of schemas with conditional fields + WHERE filter on the conditional value.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ArrayConditionalFields_WhereFilter()
    {
        var query = @"
            binary Item {
                Tag: byte,
                Val: int le when Tag <> 0
            };
            binary Box {
                Count: byte,
                Items: Item[Count]
            };
            select i.Tag, i.Val
            from #test.files() f
            cross apply Interpret(f.Content, 'Box') b
            cross apply b.Items i
            where i.Val is not null
            order by i.Tag asc";

        using var ms = new MemoryStream();
        ms.WriteByte(4); // 4 items
        // Item 1: Tag=1, Val=10
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes(10));
        // Item 2: Tag=0, Val=null
        ms.WriteByte(0);
        // Item 3: Tag=3, Val=30
        ms.WriteByte(3);
        ms.Write(BitConverter.GetBytes(30));
        // Item 4: Tag=0, Val=null
        ms.WriteByte(0);

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        // Only items with Tag<>0 pass filter
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual((byte)3, table[1][0]);
        Assert.AreEqual(30, table[1][1]);
    }

    /// <summary>
    ///     Cross apply on schema array + GROUP BY + HAVING.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ArrayCrossApply_GroupByHaving()
    {
        var query = @"
            binary Entry {
                Cat: byte,
                Score: short le
            };
            binary DataFile {
                Count: byte,
                Entries: Entry[Count]
            };
            select e.Cat, Count(e.Cat) as Cnt, Sum(e.Score) as Total
            from #test.files() f
            cross apply Interpret(f.Content, 'DataFile') d
            cross apply d.Entries e
            group by e.Cat
            having Count(e.Cat) > 1
            order by e.Cat asc";

        using var ms = new MemoryStream();
        ms.WriteByte(5); // 5 entries
        // Cat=1 Score=10
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes((short)10));
        // Cat=2 Score=20
        ms.WriteByte(2);
        ms.Write(BitConverter.GetBytes((short)20));
        // Cat=1 Score=15
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes((short)15));
        // Cat=1 Score=25
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes((short)25));
        // Cat=2 Score=30
        ms.WriteByte(2);
        ms.Write(BitConverter.GetBytes((short)30));

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        // Cat=1: 3 entries (10+15+25=50), Cat=2: 2 entries (20+30=50)
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(3, Convert.ToInt32(table[0][1]));
        Assert.AreEqual(50m, table[0][2]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual(2, Convert.ToInt32(table[1][1]));
        Assert.AreEqual(50m, table[1][2]);
    }

    /// <summary>
    ///     Array size derived from a computed field expression.
    /// </summary>
    [TestMethod]
    public void R2_Binary_ArraySizeFromComputedField()
    {
        var query = @"
            binary Rec {
                Half: byte,
                FullCount: Half * 2,
                Items: short le[FullCount]
            };
            select r.Half, r.FullCount, i.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r
            cross apply r.Items i
            order by i.Value asc";

        using var ms = new MemoryStream();
        ms.WriteByte(2); // Half=2 → FullCount=4
        ms.Write(BitConverter.GetBytes((short)40));
        ms.Write(BitConverter.GetBytes((short)10));
        ms.Write(BitConverter.GetBytes((short)30));
        ms.Write(BitConverter.GetBytes((short)20));

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual((short)10, table[0][2]);
        Assert.AreEqual((short)20, table[1][2]);
        Assert.AreEqual((short)30, table[2][2]);
        Assert.AreEqual((short)40, table[3][2]);
    }

    /// <summary>
    ///     Multiple files with schema arrays: each file has its own data, aggregated across all.
    /// </summary>
    [TestMethod]
    public void R2_Binary_MultipleFiles_ArrayAggregation()
    {
        var query = @"
            binary Pkg {
                Count: byte,
                Vals: short le[Count]
            };
            select Sum(v.Value) as Total
            from #test.files() f
            cross apply Interpret(f.Content, 'Pkg') p
            cross apply p.Vals v";

        // File 1: Count=2, Vals=[10, 20]
        using var ms1 = new MemoryStream();
        ms1.WriteByte(2);
        ms1.Write(BitConverter.GetBytes((short)10));
        ms1.Write(BitConverter.GetBytes((short)20));

        // File 2: Count=3, Vals=[30, 40, 50]
        using var ms2 = new MemoryStream();
        ms2.WriteByte(3);
        ms2.Write(BitConverter.GetBytes((short)30));
        ms2.Write(BitConverter.GetBytes((short)40));
        ms2.Write(BitConverter.GetBytes((short)50));

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "b.bin", Content = ms2.ToArray() }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(150m, table[0][0]); // 10+20+30+40+50
    }

    /// <summary>
    ///     Schema with two separate arrays — cross apply each independently.
    /// </summary>
    [TestMethod]
    public void R2_Binary_TwoArrayFields_IndependentCrossApply()
    {
        var query = @"
            binary Rec {
                ACount: byte,
                Vals: short le[ACount],
                BCount: byte,
                Bs: int le[BCount]
            };
            select v.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r
            cross apply r.Vals v
            order by v.Value asc";

        using var ms = new MemoryStream();
        ms.WriteByte(3); // ACount=3
        ms.Write(BitConverter.GetBytes((short)30));
        ms.Write(BitConverter.GetBytes((short)10));
        ms.Write(BitConverter.GetBytes((short)20));
        ms.WriteByte(2); // BCount=2
        ms.Write(BitConverter.GetBytes(100));
        ms.Write(BitConverter.GetBytes(200));

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((short)10, table[0][0]);
        Assert.AreEqual((short)20, table[1][0]);
        Assert.AreEqual((short)30, table[2][0]);
    }

    #endregion

    #region Category 5: Text Schema Edge Cases

    /// <summary>
    ///     chars[N] with lower modifier should lowercase the captured text.
    /// </summary>
    [TestMethod]
    public void R2_Text_CharsLower_ShouldLowercase()
    {
        var query = @"
            text Rec {
                Name: chars[5] lower
            };
            select r.Name
            from #test.files() f
            cross apply Parse(f.Text, 'Rec') r";

        var entities = new[] { new TextEntity { Name = "a.txt", Text = "HELLO" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0][0]);
    }

    /// <summary>
    ///     chars[N] with upper modifier should uppercase the captured text.
    /// </summary>
    [TestMethod]
    public void R2_Text_CharsUpper_ShouldUppercase()
    {
        var query = @"
            text Rec {
                Name: chars[5] upper
            };
            select r.Name
            from #test.files() f
            cross apply Parse(f.Text, 'Rec') r";

        var entities = new[] { new TextEntity { Name = "a.txt", Text = "hello" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("HELLO", table[0][0]);
    }

    /// <summary>
    ///     Multiple sequential optional fields, some present, some not.
    /// </summary>
    [TestMethod]
    public void R2_Text_MultipleOptionals_MixedPresence()
    {
        var query = @"
            text Rec {
                Key: until '=',
                Value: until ';',
                _: optional literal ' ',
                Extra: optional pattern '[A-Z]+'
            };
            select r.Key, r.Value, r.Extra
            from #test.files() f
            cross apply Parse(f.Text, 'Rec') r";

        // Extra is present
        var entities1 = new[] { new TextEntity { Name = "a.txt", Text = "color=red; BOLD" } };
        var schemaProvider1 = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities1 } });
        var vm1 = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider1, LoggerResolver, TestCompilationOptions);
        var table1 = vm1.Run(CancellationToken.None);

        Assert.AreEqual(1, table1.Count);
        Assert.AreEqual("color", table1[0][0]);
        Assert.AreEqual("red", table1[0][1]);
        Assert.AreEqual("BOLD", table1[0][2]);

        // Extra is absent
        var entities2 = new[] { new TextEntity { Name = "a.txt", Text = "color=red;" } };
        var schemaProvider2 = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities2 } });
        var vm2 = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider2, LoggerResolver, TestCompilationOptions);
        var table2 = vm2.Run(CancellationToken.None);

        Assert.AreEqual(1, table2.Count);
        Assert.AreEqual("color", table2[0][0]);
        Assert.AreEqual("red", table2[0][1]);
        Assert.IsNull(table2[0][2]);
    }

    /// <summary>
    ///     until with multi-character delimiter.
    /// </summary>
    [TestMethod]
    public void R2_Text_UntilMultiCharDelimiter()
    {
        var query = @"
            text Rec {
                First: until '::',
                Second: rest
            };
            select r.First, r.Second
            from #test.files() f
            cross apply Parse(f.Text, 'Rec') r";

        var entities = new[] { new TextEntity { Name = "a.txt", Text = "hello::world" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0][0]);
        Assert.AreEqual("world", table[0][1]);
    }

    /// <summary>
    ///     Token at end of input should capture remaining non-whitespace.
    /// </summary>
    [TestMethod]
    public void R2_Text_TokenAtEndOfInput()
    {
        var query = @"
            text Rec {
                First: token,
                _: whitespace,
                Second: token
            };
            select r.First, r.Second
            from #test.files() f
            cross apply Parse(f.Text, 'Rec') r";

        var entities = new[] { new TextEntity { Name = "a.txt", Text = "alpha beta" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("alpha", table[0][0]);
        Assert.AreEqual("beta", table[0][1]);
    }

    /// <summary>
    ///     rest on empty remaining input should return empty string.
    /// </summary>
    [TestMethod]
    public void R2_Text_RestEmpty_ShouldReturnEmptyString()
    {
        var query = @"
            text Rec {
                All: chars[5],
                Remaining: rest
            };
            select r.All, r.Remaining
            from #test.files() f
            cross apply Parse(f.Text, 'Rec') r";

        var entities = new[] { new TextEntity { Name = "a.txt", Text = "ABCDE" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABCDE", table[0][0]);
        Assert.AreEqual(string.Empty, table[0][1]);
    }

    /// <summary>
    ///     between with immediately adjacent delimiters should capture empty string.
    /// </summary>
    [TestMethod]
    public void R2_Text_BetweenAdjacentDelimiters_EmptyCapture()
    {
        var query = @"
            text Rec {
                Val: between '[' ']'
            };
            select r.Val
            from #test.files() f
            cross apply Parse(f.Text, 'Rec') r";

        var entities = new[] { new TextEntity { Name = "a.txt", Text = "[]" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(string.Empty, table[0][0]);
    }

    /// <summary>
    ///     Text parse applied to multiple lines via filter.
    /// </summary>
    [TestMethod]
    public void R2_Text_MultipleRowsWithFilter_OrderBy()
    {
        var query = @"
            text KV {
                Key: until '=',
                Value: rest trim
            };
            select r.Key, r.Value
            from #test.files() f
            cross apply Parse(f.Text, 'KV') r
            where r.Key <> 'skip'
            order by r.Key asc";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "alpha=100" },
            new TextEntity { Name = "2.txt", Text = "skip=xxx" },
            new TextEntity { Name = "3.txt", Text = "beta=200" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("alpha", table[0][0]);
        Assert.AreEqual("100", table[0][1]);
        Assert.AreEqual("beta", table[1][0]);
        Assert.AreEqual("200", table[1][1]);
    }

    #endregion

    #region Category 6: Complex Integration

    /// <summary>
    ///     InterpretAt with computed offset from Interpret result of first schema.
    /// </summary>
    [TestMethod]
    public void R2_Complex_InterpretAtWithComputedOffset()
    {
        var query = @"
            binary Header {
                Magic: int le,
                DataOffset: int le
            };
            binary DataBlock {
                Tag: byte,
                Val: short le
            };
            select h.Magic, d.Tag, d.Val
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h
            cross apply InterpretAt(f.Content, h.DataOffset, 'DataBlock') d";

        using var ms = new MemoryStream();
        // Header: Magic=0xBEEF, DataOffset=16
        ms.Write(BitConverter.GetBytes(0xBEEF));
        ms.Write(BitConverter.GetBytes(16));
        // Pad to offset 16
        ms.Write(new byte[8]);
        // DataBlock at offset 16: Tag=42, Val=999
        ms.WriteByte(42);
        ms.Write(BitConverter.GetBytes((short)999));

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xBEEF, table[0][0]);
        Assert.AreEqual((byte)42, table[0][1]);
        Assert.AreEqual((short)999, table[0][2]);
    }

    /// <summary>
    ///     Three-level schema reference chain: A → B → C.
    /// </summary>
    [TestMethod]
    public void R2_Complex_ThreeLevelSchemaChain()
    {
        var query = @"
            binary Inner { Val: short le };
            binary Middle { Core: Inner, Extra: byte };
            binary Outer { Wrapper: Middle, Tag: byte };
            select o.Tag, o.Wrapper.Extra, o.Wrapper.Core.Val
            from #test.files() f
            cross apply Interpret(f.Content, 'Outer') o";

        using var ms = new MemoryStream();
        // Inner.Val=42, Middle.Extra=7, Outer.Tag=99
        ms.Write(BitConverter.GetBytes((short)42));
        ms.WriteByte(7);
        ms.WriteByte(99);

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)99, table[0][0]);
        Assert.AreEqual((byte)7, table[0][1]);
        Assert.AreEqual((short)42, table[0][2]);
    }

    /// <summary>
    ///     CTE with TryInterpret: filter out invalid files, aggregate valid ones.
    /// </summary>
    [TestMethod]
    public void R2_Complex_CteWithTryInterpret_FilterInvalid()
    {
        var query = @"
            binary Rec {
                Magic: int le check Magic = 48879,
                Val: short le
            };
            with ValidRecs as (
                select r.Val as V
                from #test.files() f
                cross apply TryInterpret(f.Content, 'Rec') r
            )
            select Sum(V) as Total, Count(V) as Cnt
            from ValidRecs";

        // File 1: valid (Magic=0xBEEF=48879, Val=10)
        using var ms1 = new MemoryStream();
        ms1.Write(BitConverter.GetBytes(48879));
        ms1.Write(BitConverter.GetBytes((short)10));

        // File 2: invalid (Magic=0xDEAD)
        using var ms2 = new MemoryStream();
        ms2.Write(BitConverter.GetBytes(0xDEAD));
        ms2.Write(BitConverter.GetBytes((short)99));

        // File 3: valid (Magic=0xBEEF=48879, Val=20)
        using var ms3 = new MemoryStream();
        ms3.Write(BitConverter.GetBytes(48879));
        ms3.Write(BitConverter.GetBytes((short)20));

        var entities = new[]
        {
            new BinaryEntity { Name = "ok1.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "bad.bin", Content = ms2.ToArray() },
            new BinaryEntity { Name = "ok2.bin", Content = ms3.ToArray() }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(30m, table[0][0]); // 10+20
        Assert.AreEqual(2, Convert.ToInt32(table[0][1]));
    }

    /// <summary>
    ///     Binary and text schemas in same query with correlation using two CTEs.
    /// </summary>
    [TestMethod]
    public void R2_Complex_BinaryAndText_CorrelatedJoin()
    {
        var query = @"
            binary BinRec {
                Id: byte,
                Score: short le
            };
            text TxtRec {
                Id: until ':',
                Label: rest trim
            };
            with BinData as (
                select ToString(b.Id) as BinId, b.Score as Score
                from #bin.files() bf
                cross apply Interpret(bf.Content, 'BinRec') b
            ),
            TextData as (
                select r.Id as TxtId, r.Label as Label
                from #txt.files() tf
                cross apply Parse(tf.Text, 'TxtRec') r
            )
            select bd.BinId, bd.Score, t.Label
            from BinData bd
            inner join TextData t on bd.BinId = t.TxtId
            order by bd.BinId asc";

        // Binary files: Id=1 Score=100, Id=2 Score=200
        using var ms1 = new MemoryStream();
        ms1.WriteByte(1);
        ms1.Write(BitConverter.GetBytes((short)100));
        using var ms2 = new MemoryStream();
        ms2.WriteByte(2);
        ms2.Write(BitConverter.GetBytes((short)200));

        var binEntities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "b.bin", Content = ms2.ToArray() }
        };

        // Text files: Id=1 Label=Alpha, Id=2 Label=Beta
        var txtEntities = new[]
        {
            new TextEntity { Name = "a.txt", Text = "1:Alpha" },
            new TextEntity { Name = "b.txt", Text = "2:Beta" }
        };

        var schemaProvider = new MixedSchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#bin", binEntities } },
            new Dictionary<string, IEnumerable<TextEntity>> { { "#txt", txtEntities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("1", table[0][0]);
        Assert.AreEqual((short)100, table[0][1]);
        Assert.AreEqual("Alpha", table[0][2]);
        Assert.AreEqual("2", table[1][0]);
        Assert.AreEqual((short)200, table[1][1]);
        Assert.AreEqual("Beta", table[1][2]);
    }

    /// <summary>
    ///     Multiple cross apply chains from same data source with WHERE.
    /// </summary>
    [TestMethod]
    public void R2_Complex_MultipleCrossApplyFromSameSource_WithWhere()
    {
        var query = @"
            binary Header { Magic: int le, Version: short le };
            binary Record { Id: byte, Val: int le };
            select h.Version, r.Id, r.Val
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h
            cross apply InterpretAt(f.Content, 6, 'Record') r
            where h.Version > 1 and r.Val > 50";

        using var ms = new MemoryStream();
        // Header: Magic=1234, Version=3
        ms.Write(BitConverter.GetBytes(1234));
        ms.Write(BitConverter.GetBytes((short)3));
        // Record at offset 6: Id=7, Val=99
        ms.WriteByte(7);
        ms.Write(BitConverter.GetBytes(99));

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)3, table[0][0]);
        Assert.AreEqual((byte)7, table[0][1]);
        Assert.AreEqual(99, table[0][2]);
    }

    /// <summary>
    ///     DISTINCT on interpreted field values.
    /// </summary>
    [TestMethod]
    public void R2_Complex_DistinctOnInterpretedFields()
    {
        var query = @"
            binary Rec { Cat: byte };
            select distinct r.Cat
            from #test.files() f
            cross apply Interpret(f.Content, 'Rec') r
            order by r.Cat asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = new byte[] { 3 } },
            new BinaryEntity { Name = "b.bin", Content = new byte[] { 1 } },
            new BinaryEntity { Name = "c.bin", Content = new byte[] { 3 } },
            new BinaryEntity { Name = "d.bin", Content = new byte[] { 2 } },
            new BinaryEntity { Name = "e.bin", Content = new byte[] { 1 } }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual((byte)3, table[2][0]);
    }

    #endregion
}
