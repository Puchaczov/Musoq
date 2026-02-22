using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Computed fields with bitwise operations in binary schemas.
///     Per spec section 4.6: Computed fields can use &amp;, |, ^, &gt;&gt;, &lt;&lt; operators.
///     No E2E coverage for bitwise computed fields in interpretation schemas.
/// </summary>
[TestClass]
public class BugProbe_ComputedBitwiseTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     PROBE: Computed field with bitwise AND (&amp;) operator.
    ///     Spec 4.6 example: "IsCompressed: = (RawFlags &amp; 0x01) &lt;&gt; 0"
    ///     Expected: Bitwise AND should extract flag bit.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedBitwiseAnd_ShouldExtractFlagBit()
    {
        var query = @"
            binary Data { 
                Flags: byte,
                HasBit0: = (Flags & 1) <> 0,
                HasBit1: = (Flags & 2) <> 0,
                HasBit7: = (Flags & 128) <> 0
            };
            select d.Flags, d.HasBit0, d.HasBit1, d.HasBit7 from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d
            order by d.Flags asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Data = [0x83] }, // bits 0,1,7
            new BinaryEntity { Name = "b.bin", Data = [0x02] } // bit 1 only
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        // 0x02 = bit 1 only
        Assert.AreEqual((byte)0x02, table[0][0]);
        Assert.IsFalse((bool?)table[0][1]); // HasBit0 = false
        Assert.IsTrue((bool?)table[0][2]); // HasBit1 = true
        Assert.IsFalse((bool?)table[0][3]); // HasBit7 = false
        // 0x83 = bits 0,1,7
        Assert.AreEqual((byte)0x83, table[1][0]);
        Assert.IsTrue((bool?)table[1][1]); // HasBit0 = true
        Assert.IsTrue((bool?)table[1][2]); // HasBit1 = true
        Assert.IsTrue((bool?)table[1][3]); // HasBit7 = true
    }

    /// <summary>
    ///     PROBE: Computed field with bitwise right shift (&gt;&gt;).
    ///     Spec 4.6 example: "Priority: = (RawFlags &gt;&gt; 4) &amp; 0x0F"
    ///     Expected: Shift and mask should extract nibble.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedBitwiseShift_ShouldExtractNibble()
    {
        var query = @"
            binary Data { 
                Flags: byte,
                HighNibble: = (Flags >> 4) & 0x0F,
                LowNibble: = Flags & 0x0F
            };
            select d.Flags, d.HighNibble, d.LowNibble from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[] { 0xA3 }; // high=0xA (10), low=0x3
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xA3, table[0][0]);
        Assert.AreEqual(10, table[0][1]); // high nibble
        Assert.AreEqual(3, table[0][2]); // low nibble
    }

    /// <summary>
    ///     PROBE: Computed field with bitwise OR (|).
    ///     Spec 6.3 operators: bitwise | is a valid operator.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedBitwiseOr_ShouldCombineBits()
    {
        var query = @"
            binary Data { 
                Low: byte,
                High: byte,
                Combined: = (High << 8) | Low
            };
            select d.Low, d.High, d.Combined from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[] { 0x34, 0x12 }; // Low=0x34, High=0x12 -> Combined=0x1234
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x34, table[0][0]);
        Assert.AreEqual((byte)0x12, table[0][1]);
        Assert.AreEqual(0x1234, table[0][2]);
    }
}
