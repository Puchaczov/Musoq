using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class BinaryOrTextualSubstreamTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void Query_RawSubstream_ShouldReturnBoundedPayloadBytes()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Kind: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: substream[Length] raw,\n"
            + "                Checksum: byte\n"
            + "            };\n"
            + "            select p.Kind, p.Payload, p.Checksum from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x03, 0xAA, 0xBB, 0xCC, 0x7F };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Kind", typeof(byte)),
            ("Payload", typeof(byte[])),
            ("Checksum", typeof(byte))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [(byte)0x01, new byte[] { 0xAA, 0xBB, 0xCC }, (byte)0x7F]
        );
    }

    [TestMethod]
    public void Query_RawSubstream_ZeroLength_ShouldReturnEmptyPayloadAndAdvanceToChecksum()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Kind: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: substream[Length] raw,\n"
            + "                Checksum: byte\n"
            + "            };\n"
            + "            select p.Payload, p.Checksum from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x00, 0x42 };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(table, ("Payload", typeof(byte[])), ("Checksum", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [Array.Empty<byte>(), (byte)0x42]);
    }

    [TestMethod]
    public void Query_StructuredSubstream_ExactMatch_ShouldParseNestedSchemaAndAdvanceToChecksum()
    {
        var query = (
            "\n"
            + "            binary PayloadBody {\n"
            + "                A: byte,\n"
            + "                B: byte\n"
            + "            };\n"
            + "            binary Packet {\n"
            + "                Kind: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: substream[Length] as PayloadBody,\n"
            + "                Checksum: byte\n"
            + "            };\n"
            + "            select p.Payload.A, p.Payload.B, p.Checksum from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x02, 0x0A, 0x0B, 0x7F };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Payload.A", typeof(byte)),
            ("p.Payload.B", typeof(byte)),
            ("Checksum", typeof(byte))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)0x0A, (byte)0x0B, (byte)0x7F]);
    }

    [TestMethod]
    public void Query_StructuredSubstream_ExactMode_WhenNestedUnderConsumes_ShouldThrow()
    {
        var query = (
            "\n"
            + "            binary PayloadBody {\n"
            + "                A: byte,\n"
            + "                B: byte\n"
            + "            };\n"
            + "            binary Packet {\n"
            + "                Kind: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: substream[Length] as PayloadBody exact,\n"
            + "                Checksum: byte\n"
            + "            };\n"
            + "            select p.Payload.A from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x03, 0x0A, 0x0B, 0xCC, 0x7F };

        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    [TestMethod]
    public void Query_StructuredSubstream_LaxMode_WhenNestedUnderConsumes_ShouldSkipRemainderAndAdvance()
    {
        var query = (
            "\n"
            + "            binary PayloadBody {\n"
            + "                A: byte,\n"
            + "                B: byte\n"
            + "            };\n"
            + "            binary Packet {\n"
            + "                Kind: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: substream[Length] as PayloadBody lax,\n"
            + "                Checksum: byte\n"
            + "            };\n"
            + "            select p.Payload.A, p.Payload.B, p.Checksum from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x03, 0x0A, 0x0B, 0xCC, 0x7F };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Payload.A", typeof(byte)),
            ("p.Payload.B", typeof(byte)),
            ("Checksum", typeof(byte))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)0x0A, (byte)0x0B, (byte)0x7F]);
    }

    [TestMethod]
    public void Query_InlineSchemaSubstream_ExactMatch_ShouldParseInlineFieldsAndAdvanceToChecksum()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Kind: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: substream[Length] as { A: byte, B: byte },\n"
            + "                Checksum: byte\n"
            + "            };\n"
            + "            select p.Payload.A, p.Payload.B, p.Checksum from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x02, 0x0A, 0x0B, 0x7F };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Payload.A", typeof(byte)),
            ("p.Payload.B", typeof(byte)),
            ("Checksum", typeof(byte))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)0x0A, (byte)0x0B, (byte)0x7F]);
    }

    [TestMethod]
    public void Query_InlineSchemaSubstream_LaxMode_WhenInlineUnderConsumes_ShouldSkipRemainderAndAdvance()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Kind: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: substream[Length] as { A: byte, B: byte } lax,\n"
            + "                Checksum: byte\n"
            + "            };\n"
            + "            select p.Payload.A, p.Payload.B, p.Checksum from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x03, 0x0A, 0x0B, 0xCC, 0x7F };
        var table = RunQuery(query, data);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Payload.A", typeof(byte)),
            ("p.Payload.B", typeof(byte)),
            ("Checksum", typeof(byte))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)0x0A, (byte)0x0B, (byte)0x7F]);
    }

    [TestMethod]
    public void Query_InlineSchemaSubstream_ExactMode_WhenInlineUnderConsumes_ShouldThrow()
    {
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Kind: byte,\n"
            + "                Length: byte,\n"
            + "                Payload: substream[Length] as { A: byte, B: byte } exact,\n"
            + "                Checksum: byte\n"
            + "            };\n"
            + "            select p.Payload.A from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p"
        );

        var data = new byte[] { 0x01, 0x03, 0x0A, 0x0B, 0xCC, 0x7F };

        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    private Table RunQuery(string query, byte[] content)
    {
        var entities = new[]
        {
            new BinaryEntity { Name = "packet.bin", Content = content },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        return TableMaterializationTestHelper.Materialize(vm.Run(CancellationToken.None));
    }
}
