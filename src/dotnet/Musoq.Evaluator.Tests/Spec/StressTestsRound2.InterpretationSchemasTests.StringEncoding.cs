using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class StressTestsRound2InterpretationSchemasTests
{
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
            cross apply Interpret<Rec>(f.Content) r";

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
            cross apply Interpret<Rec>(f.Content) r";

        var data = "        "u8.ToArray(); // 8 spaces
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
            cross apply Interpret<Rec>(f.Content) r";

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
            cross apply Interpret<Rec>(f.Content) r";

        using var ms = new MemoryStream();
        ms.WriteByte(3); // Len=3 → string size = 6
        ms.Write("Hello!"u8.ToArray()); // 6 bytes
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
            cross apply Interpret<Rec>(f.Content) r";

        var data = "  Hello   "u8.ToArray(); // 10 bytes: 2 leading + "Hello" + 3 trailing
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
            cross apply Interpret<Rec>(f.Content) r";

        var data = "  Hello   "u8.ToArray(); // 10 bytes
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
}
