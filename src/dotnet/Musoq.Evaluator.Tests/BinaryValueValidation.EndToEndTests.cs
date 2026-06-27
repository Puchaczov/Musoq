using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryValueValidationEndToEndTests : BinaryOrTextualEvaluatorTestBase
{
    private static readonly byte[] PngSignature =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static (int count, IReadOnlyList<object?> firstRow) RunBinaryQuery(string query, byte[] content)
    {
        var entities = new[] { new BinaryEntity { Name = "sample.bin", Content = content } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        var firstRow = table.Count > 0 ? table[0].Values : Array.Empty<object?>();
        return (table.Count, firstRow);
    }

    [TestMethod]
    public void Query_PngSignatureMagic_WithValidSignature_ShouldParseHeader()
    {
        var query = @"
            binary PngFile {
                Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
                Width: int be,
                Height: int be
            };
            select p.Width, p.Height
            from #test.files() f
            cross apply Interpret<PngFile>(f.Content) p";

        byte[] content = [.. PngSignature, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x20];

        var (count, row) = RunBinaryQuery(query, content);

        Assert.AreEqual(1, count);
        Assert.AreEqual(16, row[0]);
        Assert.AreEqual(32, row[1]);
    }

    [TestMethod]
    public void Query_PngSignatureMagic_WithCorruptedSignature_ShouldYieldNullViaTryInterpret()
    {
        var query = @"
            binary PngFile {
                Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
                Width: int be,
                Height: int be
            };
            select p.Width
            from #test.files() f
            outer apply TryInterpret<PngFile>(f.Content) p";

        byte[] content = [0x00, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x20];

        var (count, row) = RunBinaryQuery(query, content);

        Assert.AreEqual(1, count);
        Assert.IsNull(row[0]);
    }

    [TestMethod]
    public void Query_ChunkTypeStringOneOf_WithAllowedValue_ShouldParse()
    {
        var query = @"
            binary Chunk {
                Length: int be,
                ChunkType: string[4] ascii oneOf ['IHDR', 'IDAT', 'IEND']
            };
            select c.Length, c.ChunkType
            from #test.files() f
            cross apply Interpret<Chunk>(f.Content) c";

        byte[] content = [0x00, 0x00, 0x00, 0x0D, (byte)'I', (byte)'H', (byte)'D', (byte)'R'];

        var (count, row) = RunBinaryQuery(query, content);

        Assert.AreEqual(1, count);
        Assert.AreEqual(13, row[0]);
        Assert.AreEqual("IHDR", row[1]);
    }

    [TestMethod]
    public void Query_ChunkTypeStringOneOf_WithDisallowedValue_ShouldYieldNullViaTryInterpret()
    {
        var query = @"
            binary Chunk {
                Length: int be,
                ChunkType: string[4] ascii oneOf ['IHDR', 'IDAT', 'IEND']
            };
            select c.ChunkType
            from #test.files() f
            outer apply TryInterpret<Chunk>(f.Content) c";

        byte[] content = [0x00, 0x00, 0x00, 0x0D, (byte)'Z', (byte)'Z', (byte)'Z', (byte)'Z'];

        var (count, row) = RunBinaryQuery(query, content);

        Assert.AreEqual(1, count);
        Assert.IsNull(row[0]);
    }

    [TestMethod]
    public void Query_ReservedByteConst_WithExpectedZero_ShouldParse()
    {
        var query = @"
            binary Header {
                Version: byte,
                Reserved: byte const 0
            };
            select h.Version
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h";

        byte[] content = [0x03, 0x00];

        var (count, row) = RunBinaryQuery(query, content);

        Assert.AreEqual(1, count);
        Assert.AreEqual((byte)3, row[0]);
    }

    [TestMethod]
    public void Query_RawSubstreamByteListConst_WithMatchingBytes_ShouldParse()
    {
        var query = @"
            binary Packet {
                Length: byte,
                Marker: substream[3] raw const [0xAA, 0xBB, 0xCC]
            };
            select p.Length
            from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        byte[] content = [0x03, 0xAA, 0xBB, 0xCC];

        var (count, row) = RunBinaryQuery(query, content);

        Assert.AreEqual(1, count);
        Assert.AreEqual((byte)3, row[0]);
    }

    [TestMethod]
    public void Query_RawSubstreamByteListMagic_WithMismatch_ShouldYieldNullViaTryInterpret()
    {
        var query = @"
            binary Packet {
                Length: byte,
                Marker: substream[3] raw magic [0xAA, 0xBB, 0xCC]
            };
            select p.Length
            from #test.files() f
            outer apply TryInterpret<Packet>(f.Content) p";

        byte[] content = [0x03, 0xAA, 0xBB, 0xFF];

        var (count, row) = RunBinaryQuery(query, content);

        Assert.AreEqual(1, count);
        Assert.IsNull(row[0]);
    }
}
