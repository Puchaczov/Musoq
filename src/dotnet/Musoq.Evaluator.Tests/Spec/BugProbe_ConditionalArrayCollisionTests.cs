using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Conditional array fields generating non-unique loop variables.
///     Per code analysis: GenerateFieldReadCodeInner uses `i` as loop variable,
///     which can collide when multiple conditional arrays exist in the same schema.
///     Also probes for: arrays of strings, arrays of primitive types (long, int).
/// </summary>
[TestClass]
public class BugProbe_ConditionalArrayCollisionTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: Two conditional schema arrays in the same schema.
    ///     Both would use `i` as the loop variable in generated code, causing CS0128.
    ///     Expected: Both arrays should generate code with unique loop variables.
    /// </summary>
    [TestMethod]
    public void Binary_TwoConditionalArrays_ShouldNotCollideOnLoopVar()
    {
        var query = @"
            binary Item { Val: byte };
            binary Data {
                Type: byte,
                CountA: byte,
                ItemsA: Item[CountA] when Type = 1,
                CountB: byte,
                ItemsB: Item[CountB] when Type = 2
            };
            select d.Type, d.CountA, d.CountB from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        // Type=1, CountA=2, Item1=0xAA, Item2=0xBB, CountB=0
        var testData = new byte[] { 0x01, 0x02, 0xAA, 0xBB, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)2, table[0][1]);
    }

    /// <summary>
    ///     PROBE: Array of primitive integers (not schema references).
    ///     Spec 4.4: "Arrays may be of any type: primitives, strings, bytes, or schemas."
    ///     Spec example: "Ids: long le[Count] -- Array of integers"
    /// </summary>
    [TestMethod]
    public void Binary_PrimitiveIntArray_ShouldParseCorrectly()
    {
        var query = @"
            binary Data {
                Count: byte,
                Values: int le[Count]
            };
            select v from #test.files() b
            cross apply Interpret(b.Content, 'Data') d
            cross apply d.Values v
            order by v asc";

        var testData = new byte[]
        {
            0x03, // Count=3
            0x03, 0x00, 0x00, 0x00,  // 3
            0x01, 0x00, 0x00, 0x00,  // 1
            0x02, 0x00, 0x00, 0x00   // 2
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual(2, table[1][0]);
        Assert.AreEqual(3, table[2][0]);
    }

    /// <summary>
    ///     PROBE: Array of strings.
    ///     Spec 4.4 example: "Names: string[32] utf8[Count] -- Array of fixed strings"
    ///     Note: TODO comment in code generator says this is unsupported!
    /// </summary>
    [TestMethod]
    public void Binary_StringArray_ShouldParseMultipleStrings()
    {
        var query = @"
            binary Data {
                Count: byte,
                Names: string[3] utf8[Count]
            };
            select n from #test.files() b
            cross apply Interpret(b.Content, 'Data') d
            cross apply d.Names n
            order by n asc";

        var testData = new byte[]
        {
            0x02, // Count=2
            0x48, 0x69, 0x21, // "Hi!"
            0x42, 0x79, 0x65  // "Bye"
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Bye", table[0][0]);
        Assert.AreEqual("Hi!", table[1][0]);
    }
}
