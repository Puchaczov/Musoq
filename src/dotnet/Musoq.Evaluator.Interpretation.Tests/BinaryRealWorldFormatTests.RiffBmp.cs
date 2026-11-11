using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class BinaryRealWorldFormatTests
{
    #region Wave 2: RIFF/WAV headers

    // Minimal RIFF/WAV:
    //   'RIFF', RiffSize (uint le), 'WAVE'
    //   'fmt ', FmtSize (uint le), AudioFormat/Channels/SampleRate/...
    //   'data', DataSize (uint le), Data (substream[DataSize] raw)

    private const string WavSchema = (
        "\n"
        + "        binary WavFile {\n"
        + "            Riff: string[4] ascii magic 'RIFF',\n"
        + "            RiffSize: uint le,\n"
        + "            Wave: string[4] ascii magic 'WAVE',\n"
        + "            FmtId: string[4] ascii oneOf ['fmt ', 'data'],\n"
        + "            FmtSize: uint le,\n"
        + "            AudioFormat: ushort le,\n"
        + "            Channels: ushort le,\n"
        + "            SampleRate: uint le,\n"
        + "            ByteRate: uint le,\n"
        + "            BlockAlign: ushort le,\n"
        + "            BitsPerSample: ushort le,\n"
        + "            DataId: string[4] ascii oneOf ['fmt ', 'data'],\n"
        + "            DataSize: uint le,\n"
        + "            Data: substream[DataSize] raw\n"
        + "        };"
    );

    private byte[] BuildWav(string riff, string wave, byte[] data)
    {
        return Bytes()
            .Ascii(riff)
            .U32Le(36 + data.Length)
            .Ascii(wave)
            .Ascii("fmt ")
            .U32Le(16)
            .U16Le(1) // AudioFormat (PCM)
            .U16Le(2) // Channels
            .U32Le(44100) // SampleRate
            .U32Le(176400) // ByteRate
            .U16Le(4) // BlockAlign
            .U16Le(16) // BitsPerSample
            .Ascii("data")
            .U32Le(data.Length)
            .Raw(data)
            .ToArray();
    }

    [TestMethod]
    public void Interpret_ValidWav_ShouldProjectChannelsSampleRateAndDataLength()
    {
        var query =
            WavSchema
            + (
                "\n"
                + "            select w.Channels, w.SampleRate, w.Data from #test.files() f\n"
                + "            cross apply Interpret<WavFile>(f.Content) w"
            );

        var data = BuildWav("RIFF", "WAVE", [0x11, 0x22, 0x33, 0x44]);
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((ushort)2, table[0].ReadValue(0));
        Assert.AreEqual(44100u, table[0].ReadValue(1));
        CollectionAssert.AreEqual(new byte[] { 0x11, 0x22, 0x33, 0x44 }, (byte[])table[0].ReadValue(2));
    }

    [TestMethod]
    public void Interpret_WavInvalidRiffMagic_ShouldThrowParseException()
    {
        var query =
            WavSchema
            + (
                "\n"
                + "            select w.Channels from #test.files() f\n"
                + "            cross apply Interpret<WavFile>(f.Content) w"
            );

        var data = BuildWav("RIFX", "WAVE", [0x01]);
        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    [TestMethod]
    public void TryInterpret_WavInvalidWaveMagic_ShouldReturnNull()
    {
        var query =
            WavSchema
            + (
                "\n"
                + "            select f.Name, w.Channels from #test.files() f\n"
                + "            outer apply TryInterpret<WavFile>(f.Content) w"
            );

        var data = BuildWav("RIFF", "AVI ", [0x01]);
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0].ReadValue(1));
    }

    #endregion

    #region Wave 2: BMP headers

    // Minimal BMP:
    //   'BM', FileSize (uint le), Reserved (discarded), DataOffset (uint le)
    //   DibHeaderSize (uint le const 40), Width/Height (int le)
    //   Planes (ushort le const 1), BitsPerPixel (ushort le oneOf [24, 32])
    //   Pixels read with 'at DataOffset'

    private const string BmpSchema = (
        "\n"
        + "        binary BmpFile {\n"
        + "            Signature: string[2] ascii magic 'BM',\n"
        + "            FileSize: uint le,\n"
        + "            _: uint le,\n"
        + "            DataOffset: uint le,\n"
        + "            DibHeaderSize: uint le const 40,\n"
        + "            Width: int le,\n"
        + "            Height: int le,\n"
        + "            Planes: ushort le const 1,\n"
        + "            BitsPerPixel: ushort le oneOf [24, 32],\n"
        + "            Pixels: byte[6] at DataOffset\n"
        + "        };"
    );

    private byte[] BuildBmp(int bitsPerPixel, int dibHeaderSize = 40, int planes = 1)
    {
        const int dataOffset = 30;
        var pixels = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0xFF, 0x00 };
        return Bytes()
            .Ascii("BM")
            .U32Le(dataOffset + pixels.Length)
            .U32Le(0) // reserved
            .U32Le(dataOffset)
            .U32Le(dibHeaderSize)
            .U32Le(2) // Width
            .U32Le(1) // Height
            .U16Le(planes)
            .U16Le(bitsPerPixel)
            .Raw(pixels)
            .ToArray();
    }

    [TestMethod]
    public void Interpret_ValidBmp_ShouldProjectDimensionsAndPixelPayload()
    {
        var query =
            BmpSchema
            + (
                "\n"
                + "            select b.Width, b.Height, b.BitsPerPixel, b.Pixels from #test.files() f\n"
                + "            cross apply Interpret<BmpFile>(f.Content) b"
            );

        var data = BuildBmp(24);
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0].ReadValue(0));
        Assert.AreEqual(1, table[0].ReadValue(1));
        Assert.AreEqual((ushort)24, table[0].ReadValue(2));
        CollectionAssert.AreEqual(new byte[] { 0xFF, 0x00, 0x00, 0x00, 0xFF, 0x00 }, (byte[])table[0].ReadValue(3));
    }

    [TestMethod]
    public void Interpret_BmpInvalidBitsPerPixel_ShouldFailOneOf()
    {
        var query =
            BmpSchema
            + (
                "\n"
                + "            select b.Width from #test.files() f\n"
                + "            cross apply Interpret<BmpFile>(f.Content) b"
            );

        var data = BuildBmp(16);
        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    [TestMethod]
    public void Interpret_BmpInvalidDibHeaderSize_ShouldFailConst()
    {
        var query =
            BmpSchema
            + (
                "\n"
                + "            select b.Width from #test.files() f\n"
                + "            cross apply Interpret<BmpFile>(f.Content) b"
            );

        var data = BuildBmp(24, dibHeaderSize: 12);
        Assert.Throws<ParseException>(() => RunQuery(query, data));
    }

    [TestMethod]
    public void TryInterpret_BmpInvalidBitsPerPixel_ShouldPreserveInvalidRowAsNull()
    {
        var query =
            BmpSchema
            + (
                "\n"
                + "            select f.Name, b.Width from #test.files() f\n"
                + "            outer apply TryInterpret<BmpFile>(f.Content) b"
            );

        var data = BuildBmp(8);
        var table = RunQuery(query, data);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("fixture.bin", table[0].ReadValue(0));
        Assert.IsNull(table[0].ReadValue(1));
    }

    #endregion
}
