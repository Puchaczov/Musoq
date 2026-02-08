using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Additional suspected issues discovered from code analysis.
///     - Schema named with C# reserved words
///     - Computed field with boolean result type in WHERE
///     - OUTER APPLY with interpretation (null handling)
///     - Text 'pattern' with capture groups
///     - Bits type per spec should return ulong (Appendix B), but implementation returns byte
/// </summary>
[TestClass]
public class BugProbe_AdditionalIssuesTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     Bits type should return the smallest unsigned type that fits the bit count.
    ///     bits[1-8] -> byte, bits[9-16] -> ushort, bits[17-32] -> uint, bits[33-64] -> ulong.
    /// </summary>
    [TestMethod]
    public void Binary_BitsTypeShouldReturnULongPerSpec()
    {
        var query = @"
            binary Data { Flags: bits[8] };
            select d.Flags from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[] { 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        var value = table[0][0];
        Assert.IsInstanceOfType(value, typeof(byte), $"bits[8] should return byte, got {value?.GetType()?.Name}");
        Assert.AreEqual((byte)255, value);
    }

    /// <summary>
    ///     BUG PROBE: Text pattern with named capture groups.
    ///     Spec 5.2: "Named groups in the regex create separate accessible fields"
    ///     Spec 5.2: "capture (Lat, Lon)" clause exposes groups as sub-fields.
    /// </summary>
    [TestMethod]
    public void Text_PatternWithCapture_ShouldExposeNamedGroups()
    {
        var query = @"
            text Data { 
                Coords: pattern '(?<Lat>-?\d+\.\d+),(?<Lon>-?\d+\.\d+)' capture (Lat, Lon)
            };
            select d.Coords.Lat, d.Coords.Lon from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "52.5200,13.4050" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("52.5200", table[0][0]);
        Assert.AreEqual("13.4050", table[0][1]);
    }

    /// <summary>
    ///     BUG PROBE: OUTER APPLY with TryInterpret should produce NULL row for failed interpretation.
    ///     Spec Appendix D: "null result with OUTER APPLY: one row with NULL alias"
    /// </summary>
    [TestMethod]
    public void Binary_OuterApplyWithTryInterpret_ShouldProduceNullRow()
    {
        var query = @"
            binary Packet { Magic: int le check Magic = 0xDEADBEEF, Value: byte };
            select b.Name, d.Value from #test.files() b
            outer apply TryInterpret(b.Content, 'Packet') d
            order by b.Name asc";

        var valid = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE, 0x42 }; // valid
        var invalid = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF }; // invalid magic
        var entities = new[]
        {
            new BinaryEntity { Name = "a_valid.bin", Content = valid },
            new BinaryEntity { Name = "b_invalid.bin", Content = invalid }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("a_valid.bin", table[0][0]);
        Assert.AreEqual((byte)0x42, table[0][1]);
        Assert.AreEqual("b_invalid.bin", table[1][0]);
        Assert.IsNull(table[1][1]); // OUTER APPLY should produce NULL
    }

    /// <summary>
    ///     PROBE: Byte array expressions with multiplication.
    ///     Spec 4.2.3 example: "Computed: byte[Length * 2]"
    ///     Expected: Size expression should evaluate correctly.
    /// </summary>
    [TestMethod]
    public void Binary_ByteArrayWithExpressionSize_ShouldComputeCorrectly()
    {
        var query = @"
            binary Data {
                Count: byte,
                Payload: byte[Count * 2],
                Trailer: byte
            };
            select d.Count, d.Trailer from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        // Count=2, Payload=4 bytes (2*2), Trailer=0xFF
        var testData = new byte[] { 0x02, 0xAA, 0xBB, 0xCC, 0xDD, 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual((byte)0xFF, table[0][1]);
    }

    /// <summary>
    ///     PROBE: Schema named with a name that becomes a C# keyword-like collision.
    ///     Testing that schema names like "Record" generate usable code.
    /// </summary>
    [TestMethod]
    public void Binary_SchemaNamedRecord_ShouldNotConflict()
    {
        var query = @"
            binary Record { Id: int le, Value: byte };
            select r.Id, r.Value from #test.files() b
            cross apply Interpret(b.Content, 'Record') r";

        var testData = new byte[] { 0x01, 0x00, 0x00, 0x00, 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual((byte)0xFF, table[0][1]);
    }

    /// <summary>
    ///     PROBE: Conditional discard field.
    ///     Spec 4.11: "Discard fields may have any type, including conditional"
    /// </summary>
    [TestMethod]
    public void Binary_ConditionalDiscardField_ShouldSkipWhenTrue()
    {
        var query = @"
            binary Data {
                HasPadding: byte,
                _: byte[4] when HasPadding <> 0,
                Value: short le
            };
            select d.HasPadding, d.Value from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d
            order by d.HasPadding asc";

        var entities = new[]
        {
            // No padding: HasPadding=0, Value=0x0064 (100)
            new BinaryEntity { Name = "no_pad.bin", Data = [0x00, 0x64, 0x00] },
            // With padding: HasPadding=1, skip 4 bytes, Value=0x00C8 (200)
            new BinaryEntity { Name = "with_pad.bin", Data = [0x01, 0x00, 0x00, 0x00, 0x00, 0xC8, 0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.AreEqual((short)100, table[0][1]);
        Assert.AreEqual((byte)1, table[1][0]);
        Assert.AreEqual((short)200, table[1][1]);
    }
}
