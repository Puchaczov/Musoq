using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryOrTextualSubstreamIntegrationTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void Query_LengthPrefixedFrame_WithUIntLeLength_ShouldParseBodyAndAdvanceToTrailer()
    {
        var query = @"
            binary Body {
                A: byte,
                B: byte
            };
            binary Frame {
                Kind: byte,
                Length: uint le,
                Payload: substream[Length] as Body,
                Trailer: byte
            };
            select f.Kind, f.Payload.A, f.Payload.B, f.Trailer from #test.files() src
            cross apply Interpret<Frame>(src.Content) f";

        var data = new byte[] { 0x07, 0x02, 0x00, 0x00, 0x00, 0x0A, 0x0B, 0x99 };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x07, table[0][0]);
        Assert.AreEqual((byte)0x0A, table[0][1]);
        Assert.AreEqual((byte)0x0B, table[0][2]);
        Assert.AreEqual((byte)0x99, table[0][3]);
    }

    [TestMethod]
    public void Query_NestedSubstream_OuterFramesInnerFrame_ShouldParseBothLevels()
    {
        var query = @"
            binary Inner {
                X: byte
            };
            binary Outer {
                InnerLength: byte,
                Inner: substream[InnerLength] as Inner,
                Tag: byte
            };
            binary Envelope {
                OuterLength: byte,
                Outer: substream[OuterLength] as Outer,
                Footer: byte
            };
            select e.Outer.Inner.X, e.Outer.Tag, e.Footer from #test.files() src
            cross apply Interpret<Envelope>(src.Content) e";

        var data = new byte[] { 0x03, 0x01, 0x2A, 0x55, 0x7E };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x2A, table[0][0]);
        Assert.AreEqual((byte)0x55, table[0][1]);
        Assert.AreEqual((byte)0x7E, table[0][2]);
    }

    [TestMethod]
    public void Query_RawSubstreamFrame_ShouldExposePayloadBytesAndContinueParsing()
    {
        var query = @"
            binary Record {
                Type: byte,
                Length: byte,
                Value: substream[Length] raw,
                Next: byte
            };
            select r.Type, r.Value, r.Next from #test.files() src
            cross apply Interpret<Record>(src.Content) r";

        var data = new byte[] { 0x10, 0x04, 0xDE, 0xAD, 0xBE, 0xEF, 0x20 };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x10, table[0][0]);
        CollectionAssert.AreEqual(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, (byte[])table[0][1]);
        Assert.AreEqual((byte)0x20, table[0][2]);
    }

    private Table RunQuery(string query, byte[] content)
    {
        var entities = new[] { new BinaryEntity { Name = "frame.bin", Content = content } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        return TableMaterializationTestHelper.Materialize(vm.Run(CancellationToken.None));
    }
}
