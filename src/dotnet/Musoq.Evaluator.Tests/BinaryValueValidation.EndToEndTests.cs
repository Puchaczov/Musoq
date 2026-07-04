using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryValueValidationEndToEndTests : BinaryOrTextualEvaluatorTestBase
{
    private static readonly byte[] PngSignature =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static Table RunBinaryQuery(string query, byte[] content)
    {
        var entities = new[] { new BinaryEntity { Name = "sample.bin", Content = content } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver, TestCompilationOptions);
        return TableMaterializationTestHelper.Materialize(vm.Run(CancellationToken.None));
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

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Width", typeof(int)),
            ("p.Height", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [16, 32]);
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

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Width", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null });
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

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.Length", typeof(int)),
            ("c.ChunkType", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [13, "IHDR"]);
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

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(table, ("c.ChunkType", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null });
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

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(table, ("h.Version", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)3]);
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

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Length", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)3]);
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

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Length", typeof(byte?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null });
    }
}
