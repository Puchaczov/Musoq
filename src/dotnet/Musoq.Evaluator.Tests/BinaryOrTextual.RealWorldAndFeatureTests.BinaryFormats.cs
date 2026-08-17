using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualRealWorldAndFeatureTests
{
    #region Real-World Binary Format Tests

    /// <summary>
    ///     Tests parsing of PNG file format header.
    ///     PNG files start with an 8-byte signature followed by chunks.
    ///     Each chunk has: 4-byte length, 4-byte type, data, 4-byte CRC.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_PngHeader_ShouldParseSignatureAndIHDR()
    {
        var query = @"
            binary PngSignature {
                Signature: byte[8],
                FirstChunkLength: int be,
                FirstChunkType: string[4] ascii,
                Width: int be,
                Height: int be,
                BitDepth: byte,
                ColorType: byte,
                CompressionMethod: byte,
                FilterMethod: byte,
                InterlaceMethod: byte
            };
            select
                p.Width,
                p.Height,
                p.BitDepth,
                p.ColorType,
                p.FirstChunkType
            from #test.files() f
            cross apply Interpret<PngSignature>(f.Content) p";


        var pngData = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x03, 0x20,
            0x00, 0x00, 0x02, 0x58,
            0x08,
            0x06,
            0x00,
            0x00,
            0x00
        };

        var entities = new[] { new BinaryEntity { Name = "image.png", Content = pngData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(800, table[0][0]);
        Assert.AreEqual(600, table[0][1]);
        Assert.AreEqual((byte)8, table[0][2]);
        Assert.AreEqual((byte)6, table[0][3]);
        Assert.AreEqual("IHDR", table[0][4]);
    }

    /// <summary>
    ///     Tests parsing of BMP file format header.
    ///     BMP has a 14-byte file header followed by DIB header (typically 40 bytes for BITMAPINFOHEADER).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_BmpHeader_ShouldParseFileAndDibHeader()
    {
        var query = @"
            binary BmpHeader {
                Magic: string[2] ascii,
                FileSize: int le,
                Reserved1: short le,
                Reserved2: short le,
                PixelDataOffset: int le,
                DibHeaderSize: int le,
                Width: int le,
                Height: int le,
                ColorPlanes: short le,
                BitsPerPixel: short le,
                Compression: int le,
                ImageSize: int le,
                HorizontalRes: int le,
                VerticalRes: int le,
                ColorsInPalette: int le,
                ImportantColors: int le
            };
            select
                b.Magic,
                b.Width,
                b.Height,
                b.BitsPerPixel,
                b.Compression
            from #test.files() f
            cross apply Interpret<BmpHeader>(f.Content) b";


        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);


        bw.Write((byte)'B');
        bw.Write((byte)'M');
        bw.Write(1024 * 768 * 3 + 54);
        bw.Write((short)0);
        bw.Write((short)0);
        bw.Write(54);


        bw.Write(40);
        bw.Write(1024);
        bw.Write(768);
        bw.Write((short)1);
        bw.Write((short)24);
        bw.Write(0);
        bw.Write(1024 * 768 * 3);
        bw.Write(2835);
        bw.Write(2835);
        bw.Write(0);
        bw.Write(0);

        var entities = new[] { new BinaryEntity { Name = "image.bmp", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("BM", table[0][0]);
        Assert.AreEqual(1024, table[0][1]);
        Assert.AreEqual(768, table[0][2]);
        Assert.AreEqual((short)24, table[0][3]);
        Assert.AreEqual(0, table[0][4]);
    }

    /// <summary>
    ///     Tests parsing of WAV audio file format (RIFF container).
    ///     WAV files have: RIFF header, fmt chunk, and data chunk.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_WavHeader_ShouldParseRiffAndFmtChunk()
    {
        var query = @"
            binary WavHeader {
                RiffMagic: string[4] ascii,
                FileSize: int le,
                WaveMagic: string[4] ascii,
                FmtChunkId: string[4] ascii,
                FmtChunkSize: int le,
                AudioFormat: short le,
                NumChannels: short le,
                SampleRate: int le,
                ByteRate: int le,
                BlockAlign: short le,
                BitsPerSample: short le
            };
            select
                w.RiffMagic,
                w.WaveMagic,
                w.AudioFormat,
                w.NumChannels,
                w.SampleRate,
                w.BitsPerSample
            from #test.files() f
            cross apply Interpret<WavHeader>(f.Content) w";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);


        bw.Write("RIFF"u8.ToArray());
        bw.Write(44100 * 2 * 2 + 36);
        bw.Write("WAVE"u8.ToArray());


        bw.Write("fmt "u8.ToArray());
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)2);
        bw.Write(44100);
        bw.Write(44100 * 2 * 2);
        bw.Write((short)4);
        bw.Write((short)16);

        var entities = new[] { new BinaryEntity { Name = "audio.wav", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("RIFF", table[0][0]);
        Assert.AreEqual("WAVE", table[0][1]);
        Assert.AreEqual((short)1, table[0][2]);
        Assert.AreEqual((short)2, table[0][3]);
        Assert.AreEqual(44100, table[0][4]);
        Assert.AreEqual((short)16, table[0][5]);
    }

    /// <summary>
    ///     Tests parsing of ZIP local file header.
    ///     ZIP files have local file headers with signature 0x04034b50.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_ZipLocalFileHeader_ShouldParseEntry()
    {
        var query = @"
            binary ZipLocalHeader {
                Signature: int le,
                VersionNeeded: short le,
                GeneralPurpose: short le,
                CompressionMethod: short le,
                LastModTime: short le,
                LastModDate: short le,
                Crc32: int le,
                CompressedSize: int le,
                UncompressedSize: int le,
                FileNameLength: short le,
                ExtraFieldLength: short le,
                FileName: string[FileNameLength] ascii
            };
            select
                z.Signature,
                z.CompressionMethod,
                z.CompressedSize,
                z.UncompressedSize,
                z.FileName
            from #test.files() f
            cross apply Interpret<ZipLocalHeader>(f.Content) z";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        var fileName = "hello.txt"u8.ToArray();

        bw.Write(0x04034b50);
        bw.Write((short)20);
        bw.Write((short)0);
        bw.Write((short)8);
        bw.Write((short)0);
        bw.Write((short)0);
        bw.Write(0x12345678);
        bw.Write(100);
        bw.Write(200);
        bw.Write((short)fileName.Length);
        bw.Write((short)0);
        bw.Write(fileName);

        var entities = new[] { new BinaryEntity { Name = "archive.zip", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x04034b50, table[0][0]);
        Assert.AreEqual((short)8, table[0][1]);
        Assert.AreEqual(100, table[0][2]);
        Assert.AreEqual(200, table[0][3]);
        Assert.AreEqual("hello.txt", table[0][4]);
    }

    /// <summary>
    ///     Tests parsing of GIF header (GIF89a format).
    ///     GIF files have: signature, logical screen descriptor, and global color table.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_GifHeader_ShouldParseSignatureAndScreenDescriptor()
    {
        var query = @"
            binary GifHeader {
                Signature: string[3] ascii,
                Version: string[3] ascii,
                Width: short le,
                Height: short le,
                PackedByte: byte,
                BackgroundColorIndex: byte,
                PixelAspectRatio: byte
            };
            select
                g.Signature,
                g.Version,
                g.Width,
                g.Height,
                g.PackedByte,
                g.BackgroundColorIndex
            from #test.files() f
            cross apply Interpret<GifHeader>(f.Content) g";

        var gifData = new byte[]
        {
            0x47, 0x49, 0x46,
            0x38, 0x39, 0x61,
            0x80, 0x02,
            0xE0, 0x01,
            0xF7,
            0x00,
            0x00
        };

        var entities = new[] { new BinaryEntity { Name = "image.gif", Content = gifData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("GIF", table[0][0]);
        Assert.AreEqual("89a", table[0][1]);
        Assert.AreEqual((short)640, table[0][2]);
        Assert.AreEqual((short)480, table[0][3]);
        Assert.AreEqual((byte)0xF7, table[0][4]);
        Assert.AreEqual((byte)0, table[0][5]);
    }

    /// <summary>
    ///     Tests parsing of ELF executable header (Linux binary format).
    ///     ELF files have a 52-byte (32-bit) or 64-byte (64-bit) header.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_Elf64Header_ShouldParseExecutable()
    {
        var query = @"
            binary Elf64Header {
                Magic: byte[4],
                Class: byte,
                Endianness: byte,
                Version: byte,
                OsAbi: byte,
                AbiVersion: byte,
                Padding: byte[7],
                Type: short le,
                Machine: short le,
                ElfVersion: int le,
                EntryPoint: long le,
                ProgramHeaderOffset: long le,
                SectionHeaderOffset: long le
            };
            select
                e.Class,
                e.Endianness,
                e.Type,
                e.Machine,
                e.EntryPoint
            from #test.files() f
            cross apply Interpret<Elf64Header>(f.Content) e
            where e.Class = 2";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);


        bw.Write(new byte[] { 0x7F, 0x45, 0x4C, 0x46 });
        bw.Write((byte)2);
        bw.Write((byte)1);
        bw.Write((byte)1);
        bw.Write((byte)0);
        bw.Write((byte)0);
        bw.Write(new byte[7]);
        bw.Write((short)2);
        bw.Write((short)62);
        bw.Write(1);
        bw.Write((long)0x400000);
        bw.Write((long)64);
        bw.Write((long)0);

        var entities = new[] { new BinaryEntity { Name = "program", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
        Assert.AreEqual((short)2, table[0][2]);
        Assert.AreEqual((short)62, table[0][3]);
        Assert.AreEqual(0x400000L, table[0][4]);
    }

    /// <summary>
    ///     Tests parsing of TAR archive header (USTAR format).
    ///     TAR headers are 512 bytes with fixed-width ASCII fields.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_TarHeader_ShouldParseArchiveEntry()
    {
        var query = @"
            binary TarHeader {
                FileName: string[100] ascii,
                FileMode: string[8] ascii,
                OwnerId: string[8] ascii,
                GroupId: string[8] ascii,
                FileSize: string[12] ascii,
                ModTime: string[12] ascii,
                Checksum: string[8] ascii,
                TypeFlag: byte,
                LinkName: string[100] ascii,
                UstarMagic: string[6] ascii,
                UstarVersion: string[2] ascii,
                OwnerName: string[32] ascii,
                GroupName: string[32] ascii
            };
            select
                Trim(t.FileName) as FileName,
                Trim(t.FileMode) as FileMode,
                Trim(t.FileSize) as FileSize,
                t.TypeFlag,
                Trim(t.UstarMagic) as Magic,
                Trim(t.OwnerName) as Owner
            from #test.files() f
            cross apply Interpret<TarHeader>(f.Content) t";


        var header = new byte[512];
        var fileName = "documents/report.txt"u8.ToArray();
        var fileMode = "0000644\0"u8.ToArray();
        var ownerId = "0001750\0"u8.ToArray();
        var groupId = "0001750\0"u8.ToArray();
        var fileSize = "00000001234\0"u8.ToArray();
        var modTime = "14342633427\0"u8.ToArray();
        var checksum = "        "u8.ToArray();
        var ustarMagic = "ustar\0"u8.ToArray();
        var ustarVersion = "00"u8.ToArray();
        var ownerName = "developer"u8.ToArray();

        Array.Copy(fileName, 0, header, 0, fileName.Length);
        Array.Copy(fileMode, 0, header, 100, fileMode.Length);
        Array.Copy(ownerId, 0, header, 108, ownerId.Length);
        Array.Copy(groupId, 0, header, 116, groupId.Length);
        Array.Copy(fileSize, 0, header, 124, fileSize.Length);
        Array.Copy(modTime, 0, header, 136, modTime.Length);
        Array.Copy(checksum, 0, header, 148, checksum.Length);
        header[156] = (byte)'0';
        Array.Copy(ustarMagic, 0, header, 257, ustarMagic.Length);
        Array.Copy(ustarVersion, 0, header, 263, ustarVersion.Length);
        Array.Copy(ownerName, 0, header, 265, ownerName.Length);

        var entities = new[] { new BinaryEntity { Name = "archive.tar", Content = header } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);


        Assert.StartsWith("documents/report.txt", (string)table[0][0]);
        Assert.StartsWith("0000644", (string)table[0][1]);
        Assert.StartsWith("00000001234", (string)table[0][2]);
        Assert.AreEqual((byte)'0', table[0][3]);
        Assert.StartsWith("ustar", (string)table[0][4]);
        Assert.StartsWith("developer", (string)table[0][5]);
    }

    #endregion
}
