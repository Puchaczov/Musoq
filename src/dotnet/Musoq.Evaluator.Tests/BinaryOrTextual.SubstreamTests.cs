using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryOrTextualSubstreamTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void Query_RawSubstream_ShouldReturnBoundedPayloadBytes()
    {
        var query = @"
            binary Packet {
                Kind: byte,
                Length: byte,
                Payload: substream[Length] raw,
                Checksum: byte
            };
            select p.Kind, p.Payload, p.Checksum from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x01, 0x03, 0xAA, 0xBB, 0xCC, 0x7F };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB, 0xCC }, (byte[])table[0][1]);
        Assert.AreEqual((byte)0x7F, table[0][2]);
    }

    [TestMethod]
    public void Query_RawSubstream_ZeroLength_ShouldReturnEmptyPayloadAndAdvanceToChecksum()
    {
        var query = @"
            binary Packet {
                Kind: byte,
                Length: byte,
                Payload: substream[Length] raw,
                Checksum: byte
            };
            select p.Payload, p.Checksum from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x01, 0x00, 0x42 };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        CollectionAssert.AreEqual(new byte[0], (byte[])table[0][0]);
        Assert.AreEqual((byte)0x42, table[0][1]);
    }

    [TestMethod]
    public void Query_StructuredSubstream_ExactMatch_ShouldParseNestedSchemaAndAdvanceToChecksum()
    {
        var query = @"
            binary PayloadBody {
                A: byte,
                B: byte
            };
            binary Packet {
                Kind: byte,
                Length: byte,
                Payload: substream[Length] as PayloadBody,
                Checksum: byte
            };
            select p.Payload.A, p.Payload.B, p.Checksum from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x01, 0x02, 0x0A, 0x0B, 0x7F };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x0A, table[0][0]);
        Assert.AreEqual((byte)0x0B, table[0][1]);
        Assert.AreEqual((byte)0x7F, table[0][2]);
    }

    [TestMethod]
    public void Query_StructuredSubstream_ExactMode_WhenNestedUnderConsumes_ShouldThrow()
    {
        var query = @"
            binary PayloadBody {
                A: byte,
                B: byte
            };
            binary Packet {
                Kind: byte,
                Length: byte,
                Payload: substream[Length] as PayloadBody exact,
                Checksum: byte
            };
            select p.Payload.A from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x01, 0x03, 0x0A, 0x0B, 0xCC, 0x7F };

        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    [TestMethod]
    public void Query_StructuredSubstream_LaxMode_WhenNestedUnderConsumes_ShouldSkipRemainderAndAdvance()
    {
        var query = @"
            binary PayloadBody {
                A: byte,
                B: byte
            };
            binary Packet {
                Kind: byte,
                Length: byte,
                Payload: substream[Length] as PayloadBody lax,
                Checksum: byte
            };
            select p.Payload.A, p.Payload.B, p.Checksum from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x01, 0x03, 0x0A, 0x0B, 0xCC, 0x7F };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x0A, table[0][0]);
        Assert.AreEqual((byte)0x0B, table[0][1]);
        Assert.AreEqual((byte)0x7F, table[0][2]);
    }

    [TestMethod]
    public void Query_InlineSchemaSubstream_ExactMatch_ShouldParseInlineFieldsAndAdvanceToChecksum()
    {
        var query = @"
            binary Packet {
                Kind: byte,
                Length: byte,
                Payload: substream[Length] as { A: byte, B: byte },
                Checksum: byte
            };
            select p.Payload.A, p.Payload.B, p.Checksum from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x01, 0x02, 0x0A, 0x0B, 0x7F };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x0A, table[0][0]);
        Assert.AreEqual((byte)0x0B, table[0][1]);
        Assert.AreEqual((byte)0x7F, table[0][2]);
    }

    [TestMethod]
    public void Query_InlineSchemaSubstream_LaxMode_WhenInlineUnderConsumes_ShouldSkipRemainderAndAdvance()
    {
        var query = @"
            binary Packet {
                Kind: byte,
                Length: byte,
                Payload: substream[Length] as { A: byte, B: byte } lax,
                Checksum: byte
            };
            select p.Payload.A, p.Payload.B, p.Checksum from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x01, 0x03, 0x0A, 0x0B, 0xCC, 0x7F };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x0A, table[0][0]);
        Assert.AreEqual((byte)0x0B, table[0][1]);
        Assert.AreEqual((byte)0x7F, table[0][2]);
    }

    [TestMethod]
    public void Query_InlineSchemaSubstream_ExactMode_WhenInlineUnderConsumes_ShouldThrow()
    {
        var query = @"
            binary Packet {
                Kind: byte,
                Length: byte,
                Payload: substream[Length] as { A: byte, B: byte } exact,
                Checksum: byte
            };
            select p.Payload.A from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var data = new byte[] { 0x01, 0x03, 0x0A, 0x0B, 0xCC, 0x7F };

        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    private Table RunQuery(string query, byte[] content)
    {
        var entities = new[] { new BinaryEntity { Name = "packet.bin", Content = content } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        return TableMaterializationTestHelper.Materialize(vm.Run(CancellationToken.None));
    }
}