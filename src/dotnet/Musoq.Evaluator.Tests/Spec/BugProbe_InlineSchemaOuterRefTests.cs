using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Inline anonymous schema referencing outer schema fields in size expressions.
///     GenerateInlineSchemaNestedClass generates a separate nested class with its own
///     InterpretAt method. If a field inside the inline schema references an outer field
///     in a size expression (e.g., string[Length] where Length is in the parent), the
///     generated code emits (int)_length, but _length doesn't exist in the nested class.
///
///     Root cause: InterpreterCodeGenerator.cs line ~218-289
///     GenerateInlineSchemaNestedClass doesn't pass outer scope variables.
/// </summary>
[TestClass]
public class BugProbe_InlineSchemaOuterRefTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: Inline schema field references outer field 'Length' for string size.
    ///     The inline schema generates a nested class where _length is undefined.
    ///     Expected: Either pass the outer variable or resolve it in the nested scope.
    /// </summary>
    [TestMethod]
    public void Binary_InlineSchemaWithOuterFieldRef_ShouldResolveSize()
    {
        var query = @"
            binary Outer {
                Length: byte,
                Inner: {
                    Data: string[Length] ascii
                }
            };
            select o.Inner.Data from #test.files() b
            cross apply Interpret(b.Content, 'Outer') o";

        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
        bw.Write((byte)5);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("Hello"));
        bw.Flush();

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
    ///     BUG: Inline schema byte array field references outer field for size.
    /// </summary>
    [TestMethod]
    public void Binary_InlineSchemaWithOuterByteArrayRef_ShouldResolveSize()
    {
        var query = @"
            binary Container {
                Size: byte,
                Payload: {
                    Raw: byte[Size]
                }
            };
            select c.Size from #test.files() b
            cross apply Interpret(b.Content, 'Container') c";

        var testData = new byte[] { 0x03, 0xAA, 0xBB, 0xCC };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
    }
}
