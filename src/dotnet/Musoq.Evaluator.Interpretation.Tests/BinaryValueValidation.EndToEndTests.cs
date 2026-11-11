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
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static Table RunBinaryQuery(string query, byte[] content)
    {
        var entities = new[]
        {
            new BinaryEntity { Name = "sample.bin", Content = content },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        return TableMaterializationTestHelper.Materialize(vm.Run(CancellationToken.None));
    }

    [TestMethod]
    public void Query_PngSignatureMagic_WithValidSignature_ShouldParseHeader()
    {
        var query = (
            "\n"
            + "            binary PngFile {\n"
            + "                Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],\n"
            + "                Width: int be,\n"
            + "                Height: int be\n"
            + "            };\n"
            + "            select p.Width, p.Height\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<PngFile>(f.Content) p"
        );

        byte[] content = [.. PngSignature, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x20];

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(table, ("Width", typeof(int)), ("Height", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [16, 32]);
    }

    [TestMethod]
    public void Query_PngSignatureMagic_WithCorruptedSignature_ShouldYieldNullViaTryInterpret()
    {
        var query = (
            "\n"
            + "            binary PngFile {\n"
            + "                Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],\n"
            + "                Width: int be,\n"
            + "                Height: int be\n"
            + "            };\n"
            + "            select p.Width\n"
            + "            from #test.files() f\n"
            + "            outer apply TryInterpret<PngFile>(f.Content) p"
        );

        byte[] content =
        [
            0x00,
            0x50,
            0x4E,
            0x47,
            0x0D,
            0x0A,
            0x1A,
            0x0A,
            0x00,
            0x00,
            0x00,
            0x10,
            0x00,
            0x00,
            0x00,
            0x20,
        ];

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(table, ("Width", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null });
    }

    [TestMethod]
    public void Query_ChunkTypeStringOneOf_WithAllowedValue_ShouldParse()
    {
        var query = (
            "\n"
            + "            binary Chunk {\n"
            + "                Length: int be,\n"
            + "                ChunkType: string[4] ascii oneOf ['IHDR', 'IDAT', 'IEND']\n"
            + "            };\n"
            + "            select c.Length, c.ChunkType\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Chunk>(f.Content) c"
        );

        byte[] content = [0x00, 0x00, 0x00, 0x0D, (byte)'I', (byte)'H', (byte)'D', (byte)'R'];

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(table, ("Length", typeof(int)), ("ChunkType", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [13, "IHDR"]);
    }

    [TestMethod]
    public void Query_ChunkTypeStringOneOf_WithDisallowedValue_ShouldYieldNullViaTryInterpret()
    {
        var query = (
            "\n"
            + "            binary Chunk {\n"
            + "                Length: int be,\n"
            + "                ChunkType: string[4] ascii oneOf ['IHDR', 'IDAT', 'IEND']\n"
            + "            };\n"
            + "            select c.ChunkType\n"
            + "            from #test.files() f\n"
            + "            outer apply TryInterpret<Chunk>(f.Content) c"
        );

        byte[] content = [0x00, 0x00, 0x00, 0x0D, (byte)'Z', (byte)'Z', (byte)'Z', (byte)'Z'];

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(table, ("ChunkType", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null });
    }

    [TestMethod]
    public void Query_ReservedByteConst_WithExpectedZero_ShouldParse()
    {
        var query = (
            "\n"
            + "            binary Header {\n"
            + "                Version: byte,\n"
            + "                Reserved: byte const 0\n"
            + "            };\n"
            + "            select h.Version\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Header>(f.Content) h"
        );

        byte[] content = [0x03, 0x00];

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(table, ("Version", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)3]);
    }

    [TestMethod]
    public void Query_RawSubstreamByteListConst_WithMatchingBytes_ShouldParse()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Length: byte,\n"
            + "                Marker: substream[3] raw const [0xAA, 0xBB, 0xCC]\n"
            + "            };\n"
            + "            select p.Length\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        byte[] content = [0x03, 0xAA, 0xBB, 0xCC];

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(table, ("Length", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)3]);
    }

    [TestMethod]
    public void Query_RawSubstreamByteListMagic_WithMismatch_ShouldYieldNullViaTryInterpret()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Length: byte,\n"
            + "                Marker: substream[3] raw magic [0xAA, 0xBB, 0xCC]\n"
            + "            };\n"
            + "            select p.Length\n"
            + "            from #test.files() f\n"
            + "            outer apply TryInterpret<Packet>(f.Content) p"
        );

        byte[] content = [0x03, 0xAA, 0xBB, 0xFF];

        var table = RunBinaryQuery(query, content);

        TableMaterializationTestHelper.AssertColumns(table, ("Length", typeof(byte?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null });
    }
}
