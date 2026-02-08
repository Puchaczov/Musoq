using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: Inline/anonymous schemas within binary schemas.
///     Per spec section 8.2: "Anonymous schemas are defined inline".
///     Known issue: code generator types inline schema properties as object (CS1061).
/// </summary>
[TestClass]
public class BugProbe_InlineAnonymousSchemaTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: Inline anonymous schema in binary definition.
    ///     Spec 8.2: "binary Packet { Header: { Magic: int le, Version: short le }, Body: byte[64] }"
    ///     Expected: Header.Magic and Header.Version should be accessible via dot notation.
    /// </summary>
    [TestMethod]
    public void Binary_InlineSchema_ShouldBeAccessibleViaDotNotation()
    {
        var query = @"
            binary Packet {
                Header: {
                    Magic: int le,
                    Version: short le
                },
                Payload: byte
            };
            select p.Header.Magic, p.Header.Version, p.Payload from #test.files() b
            cross apply Interpret(b.Content, 'Packet') p";

        var testData = new byte[]
        {
            0x78, 0x56, 0x34, 0x12, // Magic = 0x12345678
            0x03, 0x00,              // Version = 3
            0xFF                     // Payload
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x12345678, table[0][0]);
        Assert.AreEqual((short)3, table[0][1]);
        Assert.AreEqual((byte)0xFF, table[0][2]);
    }
}
