using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P2-0069")]
    public void Current_P2_0069_Utf8ComputedStringResumesAtTail()
    {
        const string query = "binary Utf8ComputedPacket { PayloadLength: byte, Payload: byte[PayloadLength], Text: string[4] utf8, TextCopy: (Text), Tail: byte }; select p.Text as Text, p.TextCopy as TextCopy, p.Tail as Tail from #test.bytes() b cross apply Interpret<Utf8ComputedPacket>(b.Content) p";
        using var compiled = CompileComputedStringCopy(query);
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(result,
            ("Text", typeof(string)), ("TextCopy", typeof(string)), ("Tail", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "\U0001F600", "\U0001F600", (byte)90 });
    }

    [TestMethod]
    public void ComputedFieldCopies_ShouldPreserveChainedStringAndByteTypes()
    {
        const string query = "binary CopyPacket { PayloadLength: byte, Payload: byte[PayloadLength], Text: string[4] utf8, TextCopy: (Text), TextChain: (TextCopy), Tail: byte, TailCopy: (Tail), TailChain: (TailCopy) }; select p.TextChain as Text, p.TailCopy as TailCopy, p.TailChain as TailChain from #test.bytes() b cross apply Interpret<CopyPacket>(b.Content) p";
        using var compiled = CompileComputedStringCopy(query);
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(result,
            ("Text", typeof(string)), ("TailCopy", typeof(byte)), ("TailChain", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "\U0001F600", (byte)90, (byte)90 });
    }

    private CompiledQuery CompileComputedStringCopy(string query)
    {
        var entities = new[]
        {
            new Components.BinaryEntity
            {
                Name = "utf8-four-byte-with-tail.bin",
                Content = [3, 17, 34, 51, 240, 159, 152, 128, 90]
            }
        };
        var sources = new Dictionary<string, IEnumerable<Components.BinaryEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["test"] = entities,
            ["#test"] = entities
        };
        return InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new Components.BinarySchemaProvider(sources), LoggerResolver, TestCompilationOptions);
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P3-0024")]
    public void Current_P3_0024_BinaryComputedFieldCanBeFiltered()
    {
        const string query = "binary ComputedPacket { Value: byte, Copied: (Value), Marker: byte } select p.Copied as Copied, p.Marker as Marker from #test.entities() e cross apply Interpret<ComputedPacket>(e.Content) p where p.Copied = 5";
        var entities = new[]
        {
            new Components.BinaryEntity { Name = "selected.bin", Content = [5, 42] },
            new Components.BinaryEntity { Name = "filtered.bin", Content = [7, 43] }
        };
        var sources = new Dictionary<string, IEnumerable<Components.BinaryEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["test"] = entities,
            ["#test"] = entities
        };
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new Components.BinarySchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(result,
            ("Copied", typeof(byte)), ("Marker", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { (byte)5, (byte)42 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-ComputedBinaryFieldReportsTheReferencedByteType")]
    public void ComputedBinaryFieldReportsTheReferencedByteType()
    {
        const string query = "binary P { V: byte, C: (V) } select p.C C from #test.entities() e cross apply Interpret<P>(e.Content) p";
        var sources = new Dictionary<string, IEnumerable<Components.BinaryEntity>>
        {
            ["#test"] = Array.Empty<Components.BinaryEntity>()
        };
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new Components.BinarySchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(result, ("C", typeof(byte)));
        Assert.AreEqual(0, result.Count);
    }
}
