using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class BinaryRealWorldFormatTests
{
    #region Wave 1: PNG-like chunk streams

    // PNG-like format:
    //   Signature: byte[8] magic [0x89 'P' 'N' 'G' \r \n 0x1A \n]
    //   Each chunk: Length (uint be), Type (string[4] ascii oneOf), Payload (substream[Length] raw), Crc (uint be)

    private const string PngChunkSchema = @"
        binary PngChunk {
            Length: uint be,
            Type: string[4] ascii oneOf ['IHDR', 'IDAT', 'IEND', 'tEXt'],
            Payload: substream[Length] raw,
            Crc: uint be
        };";

    private static byte[] PngSignature()
    {
        return [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    }

    private static ByteWriter PngChunk(string type, byte[] payload, long crc)
    {
        return Bytes()
            .U32Be(payload.Length)
            .Ascii(type)
            .Raw(payload)
            .U32Be(crc);
    }

    [TestMethod]
    public void Interpret_ValidPngChunkStream_ShouldProjectFirstChunkAndCount()
    {
        var query = PngChunkSchema + @"
            binary PngFile {
                Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
                First: PngChunk,
                Rest: PngChunk repeat until eof
            };
            select p.First.Type, p.First.Payload, p.Rest from #test.files() f
            cross apply Interpret<PngFile>(f.Content) p";

        var data = Bytes()
            .Raw(PngSignature())
            .Raw(PngChunk("IHDR", [0x01, 0x02], 0x0000000A).ToArray())
            .Raw(PngChunk("IDAT", [0x10, 0x20, 0x30], 0x0000000B).ToArray())
            .Raw(PngChunk("IEND", [], 0x0000000C).ToArray())
            .ToArray();

        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("IHDR", table[0][0]);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02 }, (byte[])table[0][1]);
        Assert.AreEqual(2, ((object[])table[0][2]).Length);
    }

    [TestMethod]
    public void Interpret_PngChunkStream_ShouldReadAllChunksUntilEof()
    {
        var query = PngChunkSchema + @"
            binary PngFile {
                Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
                Chunks: PngChunk repeat until eof
            };
            select p.Chunks from #test.files() f
            cross apply Interpret<PngFile>(f.Content) p";

        var data = Bytes()
            .Raw(PngSignature())
            .Raw(PngChunk("IHDR", [0x01, 0x02], 0x0000000A).ToArray())
            .Raw(PngChunk("IDAT", [0x10, 0x20, 0x30], 0x0000000B).ToArray())
            .Raw(PngChunk("IEND", [], 0x0000000C).ToArray())
            .ToArray();

        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, ((object[])table[0][0]).Length);
    }

    [TestMethod]
    public void Interpret_PngZeroLengthIendPayload_ShouldProjectEmptyByteArray()
    {
        var query = PngChunkSchema + @"
            binary PngFile {
                Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
                First: PngChunk
            };
            select p.First.Type, p.First.Payload from #test.files() f
            cross apply Interpret<PngFile>(f.Content) p";

        var data = Bytes()
            .Raw(PngSignature())
            .Raw(PngChunk("IEND", [], 0x0000000C).ToArray())
            .ToArray();

        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("IEND", table[0][0]);
        CollectionAssert.AreEqual(Array.Empty<byte>(), (byte[])table[0][1]);
    }

    [TestMethod]
    public void Interpret_PngInvalidSignature_ShouldThrowParseException()
    {
        var query = PngChunkSchema + @"
            binary PngFile {
                Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
                First: PngChunk
            };
            select p.First.Type from #test.files() f
            cross apply Interpret<PngFile>(f.Content) p";

        var data = Bytes()
            .Raw([0x00, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])
            .Raw(PngChunk("IHDR", [0x01, 0x02], 0x0000000A).ToArray())
            .ToArray();

        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    [TestMethod]
    public void TryInterpret_PngInvalidSignature_ShouldReturnNull()
    {
        var query = PngChunkSchema + @"
            binary PngFile {
                Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
                First: PngChunk
            };
            select f.Name, p.First from #test.files() f
            outer apply TryInterpret<PngFile>(f.Content) p";

        var data = Bytes()
            .Raw([0x00, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])
            .Raw(PngChunk("IHDR", [0x01, 0x02], 0x0000000A).ToArray())
            .ToArray();

        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void Interpret_PngUnknownChunkType_ShouldFailOneOf()
    {
        var query = PngChunkSchema + @"
            binary PngFile {
                Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
                First: PngChunk
            };
            select p.First.Type from #test.files() f
            cross apply Interpret<PngFile>(f.Content) p";

        var data = Bytes()
            .Raw(PngSignature())
            .Raw(PngChunk("XXXX", [0x01], 0x0000000A).ToArray())
            .ToArray();

        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    [TestMethod]
    public void TryInterpret_PngTruncatedFinalChunk_ShouldReturnNull()
    {
        var query = PngChunkSchema + @"
            binary PngFile {
                Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
                Chunks: PngChunk repeat until eof
            };
            select f.Name, p.Chunks from #test.files() f
            outer apply TryInterpret<PngFile>(f.Content) p";

        // Final chunk header claims Length=5 but only two payload bytes are present.
        var truncated = Bytes()
            .U32Be(5)
            .Ascii("IDAT")
            .Raw([0xAA, 0xBB])
            .ToArray();

        var data = Bytes()
            .Raw(PngSignature())
            .Raw(PngChunk("IHDR", [0x01, 0x02], 0x0000000A).ToArray())
            .Raw(truncated)
            .ToArray();

        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][1]);
    }

    #endregion
}
