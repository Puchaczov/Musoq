using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class BinaryRealWorldFormatTests
{
    #region Wave 6: Real-world failure diagnostics matrix

    // Compact diagnostic schema exercising every binary validation/IO failure mode in one place.
    //   Tag: uint le magic 0x44494147 ('DIAG')
    //   Version: byte const 1
    //   Kind: byte oneOf [1, 2, 3]
    //   PayloadLength: byte
    //   Payload: substream[PayloadLength] raw
    private const string DiagnosticSchema = @"
        binary DiagInner {
            A: ushort le,
            B: ushort le
        };
        binary DiagRecord {
            Tag: uint le magic 0x44494147,
            Version: byte const 1,
            Kind: byte oneOf [1, 2, 3],
            PayloadLength: byte,
            Payload: substream[PayloadLength] raw
        };
        binary DiagExactRecord {
            Len: byte,
            Body: substream[Len] as DiagInner
        };
        binary DiagStream {
            Records: DiagRecord repeat until eof
        };";

    private static ByteWriter DiagRecordBytes(long tag, int version, int kind, byte[] payload)
    {
        return Bytes()
            .U32Le(tag)
            .U8(version)
            .U8(kind)
            .U8(payload.Length)
            .Raw(payload);
    }

    private ParseException CaptureDiagnosticFailure(string innerSchema, byte[] data)
    {
        var query = DiagnosticSchema + innerSchema;
        return Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    private const string SelectDiagRecord = @"
        select r.Tag from #test.files() f
        cross apply Interpret<DiagRecord>(f.Content) r";

    [TestMethod]
    public void Diagnostics_BadMagic_ShouldReportValidationFailedAtTagField()
    {
        var data = DiagRecordBytes(0x00000000, 1, 1, [0x01]).ToArray();

        var exception = CaptureDiagnosticFailure(SelectDiagRecord, data);

        Assert.AreEqual(ParseErrorCode.ValidationFailed, exception.ErrorCode);
        Assert.AreEqual("Tag", exception.FieldName);
    }

    [TestMethod]
    public void Diagnostics_BadConst_ShouldReportValidationFailedAtVersionField()
    {
        var data = DiagRecordBytes(0x44494147, 9, 1, [0x01]).ToArray();

        var exception = CaptureDiagnosticFailure(SelectDiagRecord, data);

        Assert.AreEqual(ParseErrorCode.ValidationFailed, exception.ErrorCode);
        Assert.AreEqual("Version", exception.FieldName);
    }

    [TestMethod]
    public void Diagnostics_BadOneOf_ShouldReportValidationFailedAtKindField()
    {
        var data = DiagRecordBytes(0x44494147, 1, 7, [0x01]).ToArray();

        var exception = CaptureDiagnosticFailure(SelectDiagRecord, data);

        Assert.AreEqual(ParseErrorCode.ValidationFailed, exception.ErrorCode);
        Assert.AreEqual("Kind", exception.FieldName);
    }

    [TestMethod]
    public void Diagnostics_SubstreamLengthExceedsRemaining_ShouldReportInsufficientData()
    {
        // PayloadLength claims 10 bytes but only 2 follow.
        var data = Bytes()
            .U32Le(0x44494147)
            .U8(1)
            .U8(1)
            .U8(10)
            .Raw([0xAA, 0xBB])
            .ToArray();

        var exception = CaptureDiagnosticFailure(SelectDiagRecord, data);

        Assert.AreEqual(ParseErrorCode.InsufficientData, exception.ErrorCode);
    }

    [TestMethod]
    public void Diagnostics_ExactSubstreamUnderConsumes_ShouldReportValidationFailedAtBodyField()
    {
        // Body declares 6 bytes but DiagInner only consumes 4.
        var data = Bytes()
            .U8(6)
            .U16Le(0x1111)
            .U16Le(0x2222)
            .Raw([0x00, 0x00])
            .ToArray();

        var select = @"
            select r.Body.A from #test.files() f
            cross apply Interpret<DiagExactRecord>(f.Content) r";

        var exception = CaptureDiagnosticFailure(select, data);

        Assert.AreEqual(ParseErrorCode.ValidationFailed, exception.ErrorCode);
        Assert.AreEqual("Body", exception.FieldName);
    }

    [TestMethod]
    public void Diagnostics_RepeatTruncatedFinalRecord_ShouldReportInsufficientData()
    {
        // One complete record followed by a partial record (tag only, rest truncated).
        var data = Bytes()
            .Raw(DiagRecordBytes(0x44494147, 1, 1, [0x01]).ToArray())
            .U32Le(0x44494147)
            .ToArray();

        var select = @"
            select s.Records from #test.files() f
            cross apply Interpret<DiagStream>(f.Content) s";

        var exception = CaptureDiagnosticFailure(select, data);

        Assert.AreEqual(ParseErrorCode.InsufficientData, exception.ErrorCode);
    }

    [TestMethod]
    public void Diagnostics_FailureCodeFormatsAsIseString()
    {
        var data = DiagRecordBytes(0x00000000, 1, 1, [0x01]).ToArray();

        var exception = CaptureDiagnosticFailure(SelectDiagRecord, data);

        Assert.AreEqual("ISE0002", exception.FormattedErrorCode);
    }

    [TestMethod]
    public void Diagnostics_TryInterpretBadMagic_ShouldPreserveRowAsNull()
    {
        var query = DiagnosticSchema + @"
            select f.Name, r.Tag from #test.files() f
            outer apply TryInterpret<DiagRecord>(f.Content) r";

        var data = DiagRecordBytes(0x00000000, 1, 1, [0x01]).ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void Diagnostics_TryInterpretTruncatedRepeat_ShouldPreserveRowAsNull()
    {
        var query = DiagnosticSchema + @"
            select f.Name, s.Records from #test.files() f
            outer apply TryInterpret<DiagStream>(f.Content) s";

        var data = Bytes()
            .Raw(DiagRecordBytes(0x44494147, 1, 1, [0x01]).ToArray())
            .U32Le(0x44494147)
            .ToArray();

        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][1]);
    }

    #endregion
}
