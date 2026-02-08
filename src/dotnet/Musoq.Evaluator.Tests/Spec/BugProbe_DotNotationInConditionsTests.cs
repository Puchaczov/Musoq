using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: DotNode missing from GenerateConditionExpression in code generator.
///     This means dot-notation expressions like 'Header.Version' in WHEN conditions
///     or computed fields referencing nested schema fields would generate broken code.
///     Per spec 6.4: "Nested fields via dot notation: Header.Version"
/// </summary>
[TestClass]
public class BugProbe_DotNotationInConditionsTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: Computed field referencing nested schema field via dot notation.
    ///     Spec 6.4: "Nested fields via dot notation: Header.Version"
    ///     Expected: Computed field should be able to reference Inner.X.
    /// </summary>
    [TestMethod]
    public void Binary_ComputedFieldWithDotNotationReference_ShouldWork()
    {
        var query = @"
            binary Inner { X: byte, Y: byte };
            binary Outer { 
                Coords: Inner,
                Sum: = Coords.X + Coords.Y
            };
            select o.Coords.X, o.Coords.Y, o.Sum from #test.files() b
            cross apply Interpret(b.Content, 'Outer') o";

        var testData = new byte[] { 0x0A, 0x14 }; // X=10, Y=20
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)10, table[0][0]);
        Assert.AreEqual((byte)20, table[0][1]);
        Assert.AreEqual(30, table[0][2]);
    }

    /// <summary>
    ///     BUG: Conditional field (WHEN) referencing nested schema field via dot notation.
    ///     Spec 6.4: Field references in expressions may use dot notation.
    ///     Expected: WHEN clause should evaluate nested field access correctly.
    /// </summary>
    [TestMethod]
    public void Binary_ConditionalWithDotNotation_ShouldEvaluateNested()
    {
        var query = @"
            binary Header { Version: byte, Flags: byte };
            binary Packet {
                Hdr: Header,
                Payload: int le when Hdr.Version > 0
            };
            select p.Hdr.Version, p.Payload from #test.files() b
            cross apply Interpret(b.Content, 'Packet') p";

        var testData = new byte[] { 0x02, 0x00, 0x2A, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual(42, table[0][1]);
    }

    /// <summary>
    ///     BUG: Check constraint referencing nested schema field via dot notation.
    ///     Spec 6.4: "The current field (only in check clauses)" + dot notation.
    ///     Expected: Check should validate using nested field value.
    /// </summary>
    [TestMethod]
    public void Binary_CheckConstraintWithDotNotation_ShouldValidateNested()
    {
        var query = @"
            binary Inner { Magic: int le };
            binary Outer {
                Hdr: Inner check Hdr.Magic = 0x12345678,
                Value: byte
            };
            select o.Hdr.Magic, o.Value from #test.files() b
            cross apply Interpret(b.Content, 'Outer') o";

        var testData = new byte[] { 0x78, 0x56, 0x34, 0x12, 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x12345678, table[0][0]);
        Assert.AreEqual((byte)0xFF, table[0][1]);
    }
}
