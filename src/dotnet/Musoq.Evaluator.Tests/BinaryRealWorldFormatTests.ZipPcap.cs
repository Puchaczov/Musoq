using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class BinaryRealWorldFormatTests
{
    #region Wave 3: ZIP local file stream

    // Minimal ZIP local-file stream:
    //   Signature: uint le magic 0x04034B50
    //   Compression: ushort le oneOf [0, 8]
    //   NameLength: ushort le, ExtraLength: ushort le, DataLength: uint le
    //   Name: string[NameLength] ascii
    //   Extra: substream[ExtraLength] raw
    //   Data: substream[DataLength] raw
    private const string ZipSchema = @"
        binary ZipLocalFile {
            Signature: uint le magic 0x04034B50,
            Compression: ushort le oneOf [0, 8],
            NameLength: ushort le,
            ExtraLength: ushort le,
            DataLength: uint le,
            Name: string[NameLength] ascii,
            Extra: substream[ExtraLength] raw,
            Data: substream[DataLength] raw
        };
        binary ZipArchive {
            Files: ZipLocalFile repeat until eof
        };";

    private static ByteWriter ZipLocalFile(int compression, string name, byte[] extra, byte[] data)
    {
        return Bytes()
            .U32Le(0x04034B50)
            .U16Le(compression)
            .U16Le(name.Length)
            .U16Le(extra.Length)
            .U32Le(data.Length)
            .Ascii(name)
            .Raw(extra)
            .Raw(data);
    }

    [TestMethod]
    public void Interpret_ZipStreamWithTwoLocalFiles_ShouldProjectCountAndNames()
    {
        var query = ZipSchema + @"
            select a.Files from #test.files() f
            cross apply Interpret<ZipArchive>(f.Content) a";

        var data = Bytes()
            .Raw(ZipLocalFile(0, "a.txt", [0x01, 0x02], [0xAA, 0xBB, 0xCC]).ToArray())
            .Raw(ZipLocalFile(8, "b.bin", [], [0xDD]).ToArray())
            .ToArray();

        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, ((object[])table[0][0]).Length);
    }

    [TestMethod]
    public void Interpret_ZipSingleFile_ShouldProjectNameAndDataPayload()
    {
        var query = ZipSchema + @"
            binary ZipFirst {
                File: ZipLocalFile
            };
            select z.File.Name, z.File.Compression, z.File.Data from #test.files() f
            cross apply Interpret<ZipFirst>(f.Content) z";

        var data = ZipLocalFile(8, "hello.txt", [0x09], [0x10, 0x20, 0x30]).ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello.txt", table[0][0]);
        Assert.AreEqual((ushort)8, table[0][1]);
        CollectionAssert.AreEqual(new byte[] { 0x10, 0x20, 0x30 }, (byte[])table[0][2]);
    }

    [TestMethod]
    public void Interpret_ZipZeroLengthExtraAndData_ShouldProjectEmptyPayloads()
    {
        var query = ZipSchema + @"
            binary ZipFirst {
                File: ZipLocalFile
            };
            select z.File.Name, z.File.Extra, z.File.Data from #test.files() f
            cross apply Interpret<ZipFirst>(f.Content) z";

        var data = ZipLocalFile(0, "empty", [], []).ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("empty", table[0][0]);
        CollectionAssert.AreEqual(System.Array.Empty<byte>(), (byte[])table[0][1]);
        CollectionAssert.AreEqual(System.Array.Empty<byte>(), (byte[])table[0][2]);
    }

    [TestMethod]
    public void Interpret_ZipBadSignature_ShouldThrowParseException()
    {
        var query = ZipSchema + @"
            binary ZipFirst {
                File: ZipLocalFile
            };
            select z.File.Name from #test.files() f
            cross apply Interpret<ZipFirst>(f.Content) z";

        var good = ZipLocalFile(0, "x", [], [0x01]).ToArray();
        var data = (byte[])good.Clone();
        data[0] = 0x00; // corrupt signature

        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    #endregion

    #region Wave 3: PCAP capture

    // Minimal little-endian PCAP:
    //   Global header: Magic (uint le magic 0xA1B2C3D4), VersionMajor/Minor (ushort le), 4x uint le reserved-ish
    //   Per packet: TsSec/TsUsec (uint le), IncludedLength (uint le), OriginalLength (uint le), Data substream[IncludedLength] raw
    private const string PcapSchema = @"
        binary PcapPacket {
            TsSec: uint le,
            TsUsec: uint le,
            IncludedLength: uint le,
            OriginalLength: uint le,
            Data: substream[IncludedLength] raw
        };
        binary PcapFile {
            Magic: uint le magic 0xA1B2C3D4,
            VersionMajor: ushort le,
            VersionMinor: ushort le,
            ThisZone: int le,
            SigFigs: uint le,
            SnapLen: uint le,
            LinkType: uint le,
            Packets: PcapPacket repeat until eof
        };";

    private static ByteWriter PcapGlobalHeader()
    {
        return Bytes()
            .U32Le(0xA1B2C3D4)
            .U16Le(2) // VersionMajor
            .U16Le(4) // VersionMinor
            .U32Le(0) // ThisZone
            .U32Le(0) // SigFigs
            .U32Le(65535) // SnapLen
            .U32Le(1); // LinkType
    }

    private static ByteWriter PcapPacket(byte[] payload)
    {
        return Bytes()
            .U32Le(1000) // TsSec
            .U32Le(500) // TsUsec
            .U32Le(payload.Length) // IncludedLength
            .U32Le(payload.Length) // OriginalLength
            .Raw(payload);
    }

    [TestMethod]
    public void Interpret_PcapWithTwoPackets_ShouldProjectCountAndFirstPayloadByte()
    {
        var query = PcapSchema + @"
            binary PcapFirst {
                Magic: uint le magic 0xA1B2C3D4,
                VersionMajor: ushort le,
                VersionMinor: ushort le,
                ThisZone: int le,
                SigFigs: uint le,
                SnapLen: uint le,
                LinkType: uint le,
                First: PcapPacket,
                Rest: PcapPacket repeat until eof
            };
            select p.First.IncludedLength, p.First.Data, p.Rest from #test.files() f
            cross apply Interpret<PcapFirst>(f.Content) p";

        var data = Bytes()
            .Raw(PcapGlobalHeader().ToArray())
            .Raw(PcapPacket([0x45, 0x00, 0x10]).ToArray())
            .Raw(PcapPacket([0x99]).ToArray())
            .ToArray();

        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3u, table[0][0]);
        CollectionAssert.AreEqual(new byte[] { 0x45, 0x00, 0x10 }, (byte[])table[0][1]);
        Assert.AreEqual(1, ((object[])table[0][2]).Length);
    }

    [TestMethod]
    public void Interpret_PcapEmptyPacketSection_ShouldReturnZeroRecords()
    {
        var query = PcapSchema + @"
            select p.Packets from #test.files() f
            cross apply Interpret<PcapFile>(f.Content) p";

        var data = PcapGlobalHeader().ToArray();
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0, ((object[])table[0][0]).Length);
    }

    [TestMethod]
    public void TryInterpret_PcapTruncatedPacket_ShouldReturnNull()
    {
        var query = PcapSchema + @"
            select f.Name, p.Packets from #test.files() f
            outer apply TryInterpret<PcapFile>(f.Content) p";

        // Packet claims IncludedLength=8 but only 2 payload bytes are present.
        var truncatedPacket = Bytes()
            .U32Le(1000)
            .U32Le(500)
            .U32Le(8)
            .U32Le(8)
            .Raw([0x01, 0x02])
            .ToArray();

        var data = Bytes()
            .Raw(PcapGlobalHeader().ToArray())
            .Raw(truncatedPacket)
            .ToArray();

        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void Interpret_PcapBadMagic_ShouldThrowParseException()
    {
        var query = PcapSchema + @"
            select p.Packets from #test.files() f
            cross apply Interpret<PcapFile>(f.Content) p";

        var data = PcapGlobalHeader().ToArray();
        data[0] = 0x00; // corrupt magic

        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    #endregion
}
