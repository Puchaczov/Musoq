using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryOrTextualRepeatUntilEofSubstreamTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void Query_EofRepeatInsideSubstream_ShouldConsumeAllBoundedBytesAndAdvanceToTrailer()
    {
        var query = @"
            binary Inner {
                Bytes: byte repeat until eof
            };
            binary Frame {
                Length: byte,
                Payload: substream[Length] as Inner,
                Trailer: byte
            };
            select f.Payload.Bytes, f.Trailer from #test.files() src
            cross apply Interpret<Frame>(src.Content) f";

        var data = new byte[] { 0x03, 0xAA, 0xBB, 0xCC, 0x99 };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB, 0xCC }, (byte[])table[0][0]);
        Assert.AreEqual((byte)0x99, table[0][1]);
    }

    [TestMethod]
    public void Query_EofRepeatInsideZeroLengthSubstream_ShouldReturnEmptyArrayAndAdvanceToTrailer()
    {
        var query = @"
            binary Inner {
                Bytes: byte repeat until eof
            };
            binary Frame {
                Length: byte,
                Payload: substream[Length] as Inner,
                Trailer: byte
            };
            select f.Payload.Bytes, f.Trailer from #test.files() src
            cross apply Interpret<Frame>(src.Content) f";

        var data = new byte[] { 0x00, 0x42 };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        CollectionAssert.AreEqual(Array.Empty<byte>(), (byte[])table[0][0]);
        Assert.AreEqual((byte)0x42, table[0][1]);
    }

    [TestMethod]
    public void Query_EofRepeatFixedWidthStringInsideSubstream_ShouldReadAllChunks()
    {
        var query = @"
            binary Inner {
                Chunks: string[2] ascii repeat until eof
            };
            binary Frame {
                Length: byte,
                Payload: substream[Length] as Inner,
                Trailer: byte
            };
            select f.Payload.Chunks, f.Trailer from #test.files() src
            cross apply Interpret<Frame>(src.Content) f";

        var data = new byte[] { 0x04, 0x41, 0x42, 0x43, 0x44, 0x7F };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        CollectionAssert.AreEqual(new[] { "AB", "CD" }, (string[])table[0][0]);
        Assert.AreEqual((byte)0x7F, table[0][1]);
    }

    [TestMethod]
    public void Query_EofRepeatStructuredRecordsInsideSubstream_ShouldReadAllRecords()
    {
        var query = @"
            binary Entry {
                Id: byte
            };
            binary Inner {
                Items: Entry repeat until eof
            };
            binary Frame {
                Length: byte,
                Payload: substream[Length] as Inner,
                Trailer: byte
            };
            select f.Payload.Items, f.Trailer from #test.files() src
            cross apply Interpret<Frame>(src.Content) f";

        var data = new byte[] { 0x03, 0x0A, 0x0B, 0x0C, 0x55 };
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, ((object[])table[0][0]).Length);
        Assert.AreEqual((byte)0x55, table[0][1]);
    }

    private Table RunQuery(string query, byte[] content)
    {
        var entities = new[] { new BinaryEntity { Name = "frame.bin", Content = content } };
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
