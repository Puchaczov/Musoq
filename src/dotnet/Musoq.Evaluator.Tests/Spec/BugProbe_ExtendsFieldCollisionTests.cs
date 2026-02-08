using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     BUG PROBE: extends with duplicate field names between parent and child.
///     GetAllFieldsIncludingInherited collects parent + child fields without
///     deduplication, causing duplicate variable declarations in generated C# code.
///
///     Root cause: InterpreterCodeGenerator.cs GetAllFieldsIncludingInherited()
///     line ~304-321. allFields.AddRange(schema.Fields) without checking for
///     duplicates from parent schema.
/// </summary>
[TestClass]
public class BugProbe_ExtendsFieldCollisionTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    /// <summary>
    ///     BUG: Parent and child schema both define a field with the same name 'Version'.
    ///     Generated code will have:
    ///       var _version = ReadInt32LE(...);  // from parent
    ///       var _version = ReadInt32LE(...);  // from child (duplicate!)
    ///     → CS0128: A local variable named '_version' is already defined.
    ///     Expected: Child field should override parent field.
    /// </summary>
    [TestMethod]
    public void Binary_ExtendsWithOverriddenField_ShouldUseChildVersion()
    {
        var query = @"
            binary Base {
                Version: int le,
                Tag: byte
            };
            binary Extended extends Base {
                Version: int le,
                Extra: int le
            };
            select e.Version, e.Tag, e.Extra from #test.files() b
            cross apply Interpret(b.Content, 'Extended') e";

        // With field override, child's Version replaces parent's Version at the same position.
        // Data layout: [Version: int le][Tag: byte][Extra: int le]
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
        bw.Write(2);           // Version (child override, int le)
        bw.Write((byte)0xAA);  // Tag (inherited from parent)
        bw.Write(99);          // Extra (child field)
        bw.Flush();

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        // Child should override parent's Version
        Assert.AreEqual(2, table[0][0], "Child's Version should override parent's");
        Assert.AreEqual((byte)0xAA, table[0][1]);
        Assert.AreEqual(99, table[0][2]);
    }

    /// <summary>
    ///     BUG: Parent and child have multiple overlapping field names.
    ///     Each duplicate generates a CS0128 error.
    /// </summary>
    [TestMethod]
    public void Binary_ExtendsWithMultipleOverriddenFields_ShouldCompile()
    {
        var query = @"
            binary Header {
                Magic: int le,
                Flags: byte
            };
            binary DetailedHeader extends Header {
                Magic: int le,
                Flags: byte,
                Length: int le
            };
            select d.Magic, d.Flags, d.Length from #test.files() b
            cross apply Interpret(b.Content, 'DetailedHeader') d";

        // With field override, both Magic and Flags replace parent's at same position.
        // Data layout: [Magic: int le][Flags: byte][Length: int le]
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);
        bw.Write(0x12345678);  // Magic (child override, same type)
        bw.Write((byte)0x02);  // Flags (child override, same type)
        bw.Write(100);         // Length (child new field)
        bw.Flush();

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x12345678, table[0][0], "Magic should be read correctly");
        Assert.AreEqual((byte)0x02, table[0][1], "Flags should be read correctly");
        Assert.AreEqual(100, table[0][2], "Length should be read correctly");
    }
}
