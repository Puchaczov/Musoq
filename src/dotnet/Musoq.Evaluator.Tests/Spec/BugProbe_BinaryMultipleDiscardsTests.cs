using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Multiple discard fields (_) in a single binary schema.
///     Per spec section 4.11: "Multiple _ fields are permitted."
///     Known issue: code generator creates duplicate _discard variable (CS0128).
/// </summary>
[TestClass]
public class BugProbe_BinaryMultipleDiscardsTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: Two discard byte fields in the same binary schema.
    ///     Spec 4.11: "Fields named _ are parsed (cursor advances)...Multiple _ fields are permitted"
    ///     Expected: Both discards consume bytes, we read the field after them.
    /// </summary>
    [TestMethod]
    public void Binary_TwoDiscardBytes_ShouldBothAdvanceCursor()
    {
        var query = @"
            binary Data { 
                Header: byte,
                _: byte,
                _: byte,
                Value: byte
            };
            select d.Header, d.Value from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        // Header=0xAA, skip 0xBB, skip 0xCC, Value=0xDD
        var testData = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xAA, table[0][0]);
        Assert.AreEqual((byte)0xDD, table[0][1]);
    }

    /// <summary>
    ///     BUG: Three discard fields of different types in the same schema.
    ///     Spec 4.11: "Discard fields may have any type"
    /// </summary>
    [TestMethod]
    public void Binary_ThreeDiscardsDifferentTypes_ShouldAllAdvanceCursor()
    {
        var query = @"
            binary Data { 
                Id: byte,
                _: byte,
                _: short le,
                _: int le,
                Trailer: byte
            };
            select d.Id, d.Trailer from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        // Id=0x01, skip 1 byte, skip 2 bytes, skip 4 bytes, Trailer=0xFF
        var testData = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((byte)0xFF, table[0][1]);
    }

    /// <summary>
    ///     BUG: Discard byte array + discard byte in same schema.
    ///     Spec 4.11: "Discard fields may have any type, including conditional"
    /// </summary>
    [TestMethod]
    public void Binary_DiscardByteArrayAndDiscardByte_ShouldWork()
    {
        var query = @"
            binary Data { 
                Id: byte,
                _: byte[3],
                _: byte,
                Value: short le
            };
            select d.Id, d.Value from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        // Id=0x42, skip 3 bytes, skip 1 byte, Value=0x0064 (100)
        var testData = new byte[] { 0x42, 0x00, 0x00, 0x00, 0x00, 0x64, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x42, table[0][0]);
        Assert.AreEqual((short)100, table[0][1]);
    }
}
