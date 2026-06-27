using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class BinaryRealWorldFormatTests
{
    #region Wave 5: Mixed binary container with embedded text metadata

    // A binary record that carries a length-prefixed text metadata block parsed by a text schema,
    // followed by a length-prefixed raw binary payload.
    //   MetadataLength: byte
    //   Metadata: string[MetadataLength] utf8 as RecordMetadata  ('name=type' parsed by text schema)
    //   PayloadLength: byte
    //   Payload: substream[PayloadLength] raw
    private const string MixedRecordSchema = @"
        text RecordMetadata {
            Name: until '=',
            Kind: rest trim
        };
        binary MixedRecord {
            MetadataLength: byte,
            Metadata: string[MetadataLength] utf8 as RecordMetadata,
            PayloadLength: byte,
            Payload: substream[PayloadLength] raw
        };";

    private static ByteWriter MixedRecordBytes(string metadata, byte[] payload)
    {
        var metadataBytes = Encoding.UTF8.GetBytes(metadata);
        return Bytes()
            .U8(metadataBytes.Length)
            .Raw(metadataBytes)
            .U8(payload.Length)
            .Raw(payload);
    }

    [TestMethod]
    public void Interpret_MixedRecord_ShouldParseEmbeddedTextMetadata()
    {
        var query = MixedRecordSchema + @"
            select r.Metadata.Name, r.Metadata.Kind from #test.files() f
            cross apply Interpret<MixedRecord>(f.Content) r";

        var data = MixedRecordBytes("avatar=image", [0x01, 0x02]).ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("avatar", table[0][0]);
        Assert.AreEqual("image", table[0][1]);
    }

    [TestMethod]
    public void Interpret_MixedRecord_ShouldReadBinaryPayloadAfterTextAtCorrectCursor()
    {
        var query = MixedRecordSchema + @"
            select r.PayloadLength, r.Payload from #test.files() f
            cross apply Interpret<MixedRecord>(f.Content) r";

        var data = MixedRecordBytes("k=v", [0xAA, 0xBB, 0xCC, 0xDD]).ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)4, table[0][0]);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, (byte[])table[0][1]);
    }

    [TestMethod]
    public void Interpret_MixedRecordMalformedMetadata_ShouldThrowParseException()
    {
        var query = MixedRecordSchema + @"
            select r.Metadata.Name from #test.files() f
            cross apply Interpret<MixedRecord>(f.Content) r";

        // Metadata block lacks the '=' delimiter the text schema requires.
        var data = MixedRecordBytes("noseparator", [0x01]).ToArray();

        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    [TestMethod]
    public void TryInterpret_MixedRecordMalformedMetadata_ShouldReturnNull()
    {
        var query = MixedRecordSchema + @"
            select f.Name, r.Metadata from #test.files() f
            outer apply TryInterpret<MixedRecord>(f.Content) r";

        var data = MixedRecordBytes("noseparator", [0x01]).ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void Interpret_MixedRecord_ShouldFilterOnParsedTextFieldAndProjectPayloadLength()
    {
        var query = MixedRecordSchema + @"
            select r.PayloadLength from #test.files() f
            cross apply Interpret<MixedRecord>(f.Content) r
            where r.Metadata.Name = 'config'";

        var data = MixedRecordBytes("config=binary", [0x10, 0x20, 0x30]).ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
    }

    [TestMethod]
    public void Interpret_MixedRecord_ShouldExcludeRowsNotMatchingParsedTextFilter()
    {
        var query = MixedRecordSchema + @"
            select r.PayloadLength from #test.files() f
            cross apply Interpret<MixedRecord>(f.Content) r
            where r.Metadata.Name = 'config'";

        var data = MixedRecordBytes("other=binary", [0x10, 0x20, 0x30]).ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(0, table.Count);
    }

    #endregion
}
