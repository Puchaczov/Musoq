using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryRealWorldFormatTests
{
    #region Wave 7: Real-world formats in execution routing

    private const string PngFileSchema = PngChunkSchema + @"
        binary PngFile {
            Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            First: PngChunk,
            Rest: PngChunk repeat until eof
        };";

    private static byte[] PngFileBytes(params (string type, byte[] payload)[] chunks)
    {
        var writer = Bytes().Raw([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        foreach (var (type, payload) in chunks)
            writer.Raw(PngChunk(type, payload, 0).ToArray());
        return writer.ToArray();
    }

    [TestMethod]
    public void ExecutionRoute_PngCrossApply_ShouldProjectFirstChunkAcrossMultipleFiles()
    {
        var query = PngFileSchema + @"
            select f.Name, p.First.Type from #test.files() f
            cross apply Interpret<PngFile>(f.Content) p";

        var first = new BinaryEntity
        {
            Name = "a.png",
            Content = PngFileBytes(("IHDR", [0x01]), ("IEND", []))
        };
        var second = new BinaryEntity
        {
            Name = "b.png",
            Content = PngFileBytes(("IHDR", [0x02]), ("IDAT", [0x03]), ("IEND", []))
        };

        var table = RunQuery(query, first, second);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("a.png", table[0][0]);
        Assert.AreEqual("IHDR", table[0][1]);
        Assert.AreEqual("b.png", table[1][0]);
        Assert.AreEqual("IHDR", table[1][1]);
    }

    [TestMethod]
    public void ExecutionRoute_ZipCrossApply_ShouldProjectRepeatedFileCount()
    {
        var query = ZipSchema + @"
            select f.Name, a.Files from #test.files() f
            cross apply Interpret<ZipArchive>(f.Content) a";

        var archive = Bytes()
            .Raw(ZipLocalFile(0, "one.txt", [], [0x01]).ToArray())
            .Raw(ZipLocalFile(8, "two.bin", [], [0x02, 0x03]).ToArray())
            .Raw(ZipLocalFile(0, "three", [], []).ToArray())
            .ToArray();

        var table = RunQuery(query, new BinaryEntity { Name = "bundle.zip", Content = archive });

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, ((object[])table[0][1]).Length);
    }

    [TestMethod]
    public void ExecutionRoute_MixedRecordCrossApply_ShouldFilterOnParsedTextAcrossFiles()
    {
        var query = MixedRecordSchema + @"
            select f.Name, r.PayloadLength from #test.files() f
            cross apply Interpret<MixedRecord>(f.Content) r
            where r.Metadata.Kind = 'image'";

        var keep = new BinaryEntity
        {
            Name = "keep.rec",
            Content = MixedRecordBytes("avatar=image", [0x01, 0x02]).ToArray()
        };
        var drop = new BinaryEntity
        {
            Name = "drop.rec",
            Content = MixedRecordBytes("config=binary", [0x03]).ToArray()
        };

        var table = RunQuery(query, keep, drop);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("keep.rec", table[0][0]);
        Assert.AreEqual((byte)2, table[0][1]);
    }

    [TestMethod]
    public void ExecutionRoute_PngOuterApply_ShouldPreserveInvalidRowsAsNull()
    {
        var query = PngFileSchema + @"
            select f.Name, p.First from #test.files() f
            outer apply TryInterpret<PngFile>(f.Content) p";

        var valid = new BinaryEntity
        {
            Name = "good.png",
            Content = PngFileBytes(("IHDR", [0x01]), ("IEND", []))
        };
        var invalid = new BinaryEntity
        {
            Name = "bad.png",
            Content = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]
        };

        var table = RunQuery(query, valid, invalid);

        Assert.AreEqual(2, table.Count);
        Assert.IsNotNull(table[0][1]);
        Assert.AreEqual("bad.png", table[1][0]);
        Assert.IsNull(table[1][1]);
    }

    #endregion
}
