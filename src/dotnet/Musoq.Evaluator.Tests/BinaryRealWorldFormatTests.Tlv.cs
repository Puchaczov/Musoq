using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class BinaryRealWorldFormatTests
{
    #region Wave 4: TLV structured substream payloads

    // Minimal type-length-value protocol:
    //   Stream: Magic (uint le 'TLV\x01') then records repeat until eof.
    //   Each record: Type (byte oneOf [1,2,3]), Length (byte), Payload (substream[Length]).
    //   Payload may be read raw or interpreted with a nested schema.
    private const string TlvPayloadSchemas = @"
        binary TlvTextPayload {
            Encoding: byte const 1,
            TextLength: byte,
            Text: string[TextLength] utf8
        };
        binary TlvMetricPayload {
            MetricId: ushort le,
            Value: int le
        };";

    private const string TlvRawStreamSchema = TlvPayloadSchemas + @"
        binary TlvRawRecord {
            Type: byte oneOf [1, 2, 3],
            Length: byte,
            Payload: substream[Length] raw
        };
        binary TlvStream {
            Magic: uint le magic 0x544C5601,
            Records: TlvRawRecord repeat until eof
        };";

    private const string TlvTextRecordSchema = TlvPayloadSchemas + @"
        binary TlvTextRecord {
            Type: byte const 1,
            Length: byte,
            Payload: substream[Length] as TlvTextPayload
        };";

    private const string TlvMetricRecordSchema = TlvPayloadSchemas + @"
        binary TlvMetricRecord {
            Type: byte const 2,
            Length: byte,
            Payload: substream[Length] as TlvMetricPayload
        };";

    private const string TlvMetricLaxRecordSchema = TlvPayloadSchemas + @"
        binary TlvMetricLaxRecord {
            Type: byte const 2,
            Length: byte,
            Payload: substream[Length] as TlvMetricPayload lax
        };";

    private static ByteWriter TlvRecord(int type, byte[] payload)
    {
        return Bytes()
            .U8(type)
            .U8(payload.Length)
            .Raw(payload);
    }

    private static byte[] TlvTextPayload(string text)
    {
        var encoded = Encoding.UTF8.GetBytes(text);
        return Bytes()
            .U8(1)
            .U8(encoded.Length)
            .Raw(encoded)
            .ToArray();
    }

    private static byte[] TlvMetricPayload(int metricId, int value)
    {
        return Bytes()
            .U16Le(metricId)
            .U32Le(value)
            .ToArray();
    }

    [TestMethod]
    public void Interpret_TlvStreamWithThreeRawRecords_ShouldProjectRecordCount()
    {
        var query = TlvRawStreamSchema + @"
            select s.Records from #test.files() f
            cross apply Interpret<TlvStream>(f.Content) s";

        var data = Bytes()
            .U32Le(0x544C5601)
            .Raw(TlvRecord(1, [0x0A]).ToArray())
            .Raw(TlvRecord(2, [0x0B, 0x0C]).ToArray())
            .Raw(TlvRecord(3, []).ToArray())
            .ToArray();

        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, ((object[])table[0][0]).Length);
    }

    [TestMethod]
    public void Interpret_TlvRawRecord_ShouldProjectRawPayloadBytes()
    {
        var query = TlvRawStreamSchema + @"
            binary TlvRawFirst {
                Record: TlvRawRecord
            };
            select r.Record.Payload from #test.files() f
            cross apply Interpret<TlvRawFirst>(f.Content) r";

        var data = TlvRecord(2, [0xDE, 0xAD, 0xBE]).ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        CollectionAssert.AreEqual(new byte[] { 0xDE, 0xAD, 0xBE }, (byte[])table[0][0]);
    }

    [TestMethod]
    public void Interpret_TlvTextRecord_ShouldProjectInnerText()
    {
        var query = TlvTextRecordSchema + @"
            select t.Payload.Text from #test.files() f
            cross apply Interpret<TlvTextRecord>(f.Content) t";

        var payload = TlvTextPayload("hi");
        var data = TlvRecord(1, payload).ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hi", table[0][0]);
    }

    [TestMethod]
    public void Interpret_TlvMetricRecord_ShouldProjectNumericValue()
    {
        var query = TlvMetricRecordSchema + @"
            select m.Payload.MetricId, m.Payload.Value from #test.files() f
            cross apply Interpret<TlvMetricRecord>(f.Content) m";

        var payload = TlvMetricPayload(0x1234, 1000);
        var data = TlvRecord(2, payload).ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((ushort)0x1234, table[0][0]);
        Assert.AreEqual(1000, table[0][1]);
    }

    [TestMethod]
    public void Interpret_TlvMetricExactUnderConsumes_ShouldThrowParseException()
    {
        var query = TlvMetricRecordSchema + @"
            select m.Payload.Value from #test.files() f
            cross apply Interpret<TlvMetricRecord>(f.Content) m";

        var payload = TlvMetricPayload(0x1234, 1000);
        // Length claims 8 bytes but the metric payload only consumes 6 (exact mode rejects this).
        var data = Bytes()
            .U8(2)
            .U8(8)
            .Raw(payload)
            .Raw([0x00, 0x00])
            .ToArray();

        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    [TestMethod]
    public void Interpret_TlvMetricLaxAllowsTrailingBytes_ShouldProjectValue()
    {
        var query = TlvMetricLaxRecordSchema + @"
            select m.Payload.Value from #test.files() f
            cross apply Interpret<TlvMetricLaxRecord>(f.Content) m";

        var payload = TlvMetricPayload(0x1234, 4242);
        // Lax mode skips the 2 trailing bytes beyond the consumed metric payload.
        var data = Bytes()
            .U8(2)
            .U8(8)
            .Raw(payload)
            .Raw([0x77, 0x88])
            .ToArray();

        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(4242, table[0][0]);
    }

    [TestMethod]
    public void Interpret_TlvBadRecordType_ShouldFailOneOf()
    {
        var query = TlvRawStreamSchema + @"
            binary TlvRawFirst {
                Record: TlvRawRecord
            };
            select r.Record.Type from #test.files() f
            cross apply Interpret<TlvRawFirst>(f.Content) r";

        var data = TlvRecord(9, [0x01]).ToArray();

        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    [TestMethod]
    public void TryInterpret_TlvBadRecordType_ShouldReturnNull()
    {
        var query = TlvRawStreamSchema + @"
            binary TlvRawFirst {
                Record: TlvRawRecord
            };
            select f.Name, r.Record from #test.files() f
            outer apply TryInterpret<TlvRawFirst>(f.Content) r";

        var data = TlvRecord(9, [0x01]).ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][1]);
    }

    #endregion
}
