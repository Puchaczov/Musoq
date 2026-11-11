using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryOrTextualSubstreamIntegrationTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void Query_LengthPrefixedFrame_WithUIntLeLength_ShouldParseBodyAndAdvanceToTrailer()
    {
        var query = (
            "\n"
            + "            binary Body {\n"
            + "                A: byte,\n"
            + "                B: byte\n"
            + "            };\n"
            + "            binary Frame {\n"
            + "                Kind: byte,\n"
            + "                Length: uint le,\n"
            + "                Payload: substream[Length] as Body,\n"
            + "                Trailer: byte\n"
            + "            };\n"
            + "            select f.Kind, f.Payload.A, f.Payload.B, f.Trailer from #test.files() src\n"
            + "            cross apply Interpret<Frame>(src.Content) f"
        );

        var data = new byte[] { 0x07, 0x02, 0x00, 0x00, 0x00, 0x0A, 0x0B, 0x99 };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x07, table[0].ReadValue(0));
        Assert.AreEqual((byte)0x0A, table[0].ReadValue(1));
        Assert.AreEqual((byte)0x0B, table[0].ReadValue(2));
        Assert.AreEqual((byte)0x99, table[0].ReadValue(3));
    }

    [TestMethod]
    public void Query_NestedSubstream_OuterFramesInnerFrame_ShouldParseBothLevels()
    {
        var query = (
            "\n"
            + "            binary Inner {\n"
            + "                X: byte\n"
            + "            };\n"
            + "            binary Outer {\n"
            + "                InnerLength: byte,\n"
            + "                Inner: substream[InnerLength] as Inner,\n"
            + "                Tag: byte\n"
            + "            };\n"
            + "            binary Envelope {\n"
            + "                OuterLength: byte,\n"
            + "                Outer: substream[OuterLength] as Outer,\n"
            + "                Footer: byte\n"
            + "            };\n"
            + "            select e.Outer.Inner.X, e.Outer.Tag, e.Footer from #test.files() src\n"
            + "            cross apply Interpret<Envelope>(src.Content) e"
        );

        var data = new byte[] { 0x03, 0x01, 0x2A, 0x55, 0x7E };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x2A, table[0].ReadValue(0));
        Assert.AreEqual((byte)0x55, table[0].ReadValue(1));
        Assert.AreEqual((byte)0x7E, table[0].ReadValue(2));
    }

    [TestMethod]
    public void Query_RawSubstreamFrame_ShouldExposePayloadBytesAndContinueParsing()
    {
        var query = (
            "\n"
            + "            binary Record {\n"
            + "                Type: byte,\n"
            + "                Length: byte,\n"
            + "                Value: substream[Length] raw,\n"
            + "                Next: byte\n"
            + "            };\n"
            + "            select r.Type, r.Value, r.Next from #test.files() src\n"
            + "            cross apply Interpret<Record>(src.Content) r"
        );

        var data = new byte[] { 0x10, 0x04, 0xDE, 0xAD, 0xBE, 0xEF, 0x20 };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x10, table[0].ReadValue(0));
        CollectionAssert.AreEqual(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, (byte[])table[0].ReadValue(1));
        Assert.AreEqual((byte)0x20, table[0].ReadValue(2));
    }

    private Table RunQuery(string query, byte[] content)
    {
        var entities = new[]
        {
            new BinaryEntity { Name = "frame.bin", Content = content },
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
