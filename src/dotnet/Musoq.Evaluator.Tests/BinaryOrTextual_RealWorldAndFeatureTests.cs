#nullable enable annotations

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryOrTextual_RealWorldAndFeatureTests : BinaryOrTextualEvaluatorTestBase
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
            cross apply Interpret(f.Content, 'PngSignature') p";


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

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
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
            cross apply Interpret(f.Content, 'BmpHeader') b";


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

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
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
            cross apply Interpret(f.Content, 'WavHeader') w";

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

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
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
            cross apply Interpret(f.Content, 'ZipLocalHeader') z";

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

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
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
            cross apply Interpret(f.Content, 'GifHeader') g";

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

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
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
            cross apply Interpret(f.Content, 'Elf64Header') e
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

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
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
            cross apply Interpret(f.Content, 'TarHeader') t";


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

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
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

    #region Real-World Text Format Tests

    /// <summary>
    ///     Tests parsing of key=value configuration format (simpler than Apache logs).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SimpleConfig_ShouldParseKeyValuePairs()
    {
        var query = @"
            text Config {
                Key: until '=',
                Value: rest trim
            };
            select
                c.Key,
                c.Value
            from #test.files() f
            cross apply Parse(f.Text, 'Config') c";

        var configLines = new[]
        {
            "host=localhost",
            "port=5432",
            "database=myapp",
            "user=admin"
        };

        var entities = configLines.Select((line, i) => new TextEntity
        {
            Name = $"config_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(4, table.Count);
        var keys = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("host", keys);
        Assert.Contains("port", keys);
        Assert.Contains("database", keys);
        Assert.Contains("user", keys);
    }

    /// <summary>
    ///     Tests parsing of colon-separated format like /etc/passwd.
    ///     Format: username:password:uid:gid:gecos:home:shell
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_ColonSeparated_ShouldParseUserEntries()
    {
        var query = @"
            text PasswdEntry {
                Username: until ':',
                Password: until ':',
                Uid: until ':',
                Gid: until ':',
                Gecos: until ':',
                HomeDir: until ':',
                Shell: rest
            };
            select
                p.Username,
                p.Uid,
                p.Gid,
                p.HomeDir,
                p.Shell
            from #test.files() f
            cross apply Parse(f.Text, 'PasswdEntry') p
            where p.Uid <> '65534'";

        var passwdLines = new[]
        {
            "root:x:0:0:root:/root:/bin/bash",
            "daemon:x:1:1:daemon:/usr/sbin:/usr/sbin/nologin",
            "www-data:x:33:33:www-data:/var/www:/usr/sbin/nologin",
            "nobody:x:65534:65534:nobody:/nonexistent:/usr/sbin/nologin",
            "developer:x:1000:1000:Developer Account:/home/developer:/bin/zsh"
        };

        var entities = passwdLines.Select((line, i) => new TextEntity
        {
            Name = $"passwd_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(4, table.Count);
        var usernames = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("root", usernames);
        Assert.Contains("daemon", usernames);
        Assert.Contains("www-data", usernames);
        Assert.Contains("developer", usernames);
        Assert.DoesNotContain("nobody", usernames);
    }

    /// <summary>
    ///     Tests parsing of pipe-separated log format.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_PipeSeparated_ShouldParseLogEntries()
    {
        var query = @"
            text PipeLog {
                Timestamp: until '|',
                Level: until '|',
                Component: until '|',
                Message: rest trim
            };
            select
                l.Timestamp,
                l.Level,
                l.Component,
                l.Message
            from #test.files() f
            cross apply Parse(f.Text, 'PipeLog') l
            where l.Level = 'ERROR'";

        var logLines = new[]
        {
            "2024-01-05 10:30:00|INFO|WebServer|Request received from 10.0.0.1",
            "2024-01-05 10:30:01|ERROR|Database|Connection timeout after 30s",
            "2024-01-05 10:30:02|DEBUG|Cache|Cache miss for key user_123",
            "2024-01-05 10:30:03|ERROR|Auth|Invalid token for user admin"
        };

        var entities = logLines.Select((line, i) => new TextEntity
        {
            Name = $"log_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var components = table.Select(r => (string)r[2]).ToList();
        Assert.Contains("Database", components);
        Assert.Contains("Auth", components);

        Assert.IsTrue(table.All(r => (string)r[1] == "ERROR"));
    }

    /// <summary>
    ///     Tests parsing of HTTP headers (simple Name: Value format).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_HttpHeaders_ShouldParseRequestLine()
    {
        var query = @"
            text HttpHeader {
                Name: until ':',
                _: until ' ',
                Value: rest
            };
            select
                h.Name,
                h.Value
            from #test.files() f
            cross apply Parse(f.Text, 'HttpHeader') h
            where h.Name in ('Content-Type', 'Authorization', 'User-Agent')";

        var headers = new[]
        {
            "Host: api.example.com",
            "Content-Type: application/json",
            "Authorization: Bearer eyJhbGciOiJIUzI1NiIs",
            "User-Agent: MyApp/1.0.0",
            "Accept: */*",
            "Content-Length: 256"
        };

        var entities = headers.Select((h, i) => new TextEntity
        {
            Name = $"header_{i}",
            Text = h
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        var names = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("Content-Type", names);
        Assert.Contains("Authorization", names);
        Assert.Contains("User-Agent", names);
    }

    /// <summary>
    ///     Tests parsing of space-separated fixed-width fields.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SpaceSeparated_ShouldParseData()
    {
        var query = @"
            text DataEntry {
                Id: until ' ',
                Name: until ' ',
                Value: until ' ',
                Status: rest trim
            };
            select
                d.Id,
                d.Name,
                d.Value,
                d.Status
            from #test.files() f
            cross apply Parse(f.Text, 'DataEntry') d";

        var dataLines = new[]
        {
            "001 Alpha 100 Active",
            "002 Beta 200 Pending",
            "003 Gamma 300 Complete",
            "004 Delta 400 Failed"
        };

        var entities = dataLines.Select((line, i) => new TextEntity
        {
            Name = $"data_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(4, table.Count);
        var ids = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("001", ids);
        Assert.Contains("002", ids);
        Assert.Contains("003", ids);
        Assert.Contains("004", ids);
    }

    /// <summary>
    ///     Tests parsing of tab-separated values (TSV format).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_TabSeparated_ShouldParseTsvData()
    {
        var query = @"
            text TsvRow {
                Name: until '\t',
                Age: until '\t',
                City: rest trim
            };
            select
                t.Name,
                t.Age,
                t.City
            from #test.files() f
            cross apply Parse(f.Text, 'TsvRow') t";

        var tsvLines = new[]
        {
            "Alice\t30\tNew York",
            "Bob\t25\tLos Angeles",
            "Charlie\t35\tChicago"
        };

        var entities = tsvLines.Select((line, i) => new TextEntity
        {
            Name = $"tsv_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        var names = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("Alice", names);
        Assert.Contains("Bob", names);
        Assert.Contains("Charlie", names);
    }

    /// <summary>
    ///     Tests parsing of semicolon-separated format (like CSV with semicolons).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SemicolonSeparated_ShouldParseCsvData()
    {
        var query = @"
            text SemicolonCsv {
                ProductId: until ';',
                ProductName: until ';',
                Price: until ';',
                Quantity: rest trim
            };
            select
                c.ProductId,
                c.ProductName,
                c.Price,
                c.Quantity
            from #test.files() f
            cross apply Parse(f.Text, 'SemicolonCsv') c
            where c.ProductName in ('Laptop', 'Keyboard', 'Monitor')";

        var csvLines = new[]
        {
            "P001;Laptop;999.99;10",
            "P002;Mouse;29.99;150",
            "P003;Keyboard;79.99;75",
            "P004;USB Cable;9.99;500",
            "P005;Monitor;299.99;25"
        };

        var entities = csvLines.Select((line, i) => new TextEntity
        {
            Name = $"csv_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        var products = table.Select(r => (string)r[1]).ToList();
        Assert.Contains("Laptop", products);
        Assert.Contains("Keyboard", products);
        Assert.Contains("Monitor", products);
    }

    /// <summary>
    ///     Tests parsing of git log oneline format (hash + message).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_GitLogOneline_ShouldParseCommits()
    {
        var query = @"
            text GitCommit {
                Hash: until ' ',
                Message: rest trim
            };
            select
                g.Hash,
                g.Message
            from #test.files() f
            cross apply Parse(f.Text, 'GitCommit') g
            where g.Message like '%Fix%' or g.Message like '%Bug%'
            order by g.Hash desc";

        var gitLog = new[]
        {
            "a1b2c3d4 Add new feature for user authentication",
            "e5f6a7b8 Fix null pointer exception in parser",
            "c9d0e1f2 Update dependencies to latest versions",
            "a3b4c5d6 Bug fix handle empty input gracefully",
            "e7f8a9b0 Refactor database connection pooling"
        };

        var entities = gitLog.Select((line, i) => new TextEntity
        {
            Name = $"commit_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("e5f6a7b8", table[0][0]);
        Assert.AreEqual("a3b4c5d6", table[1][0]);
    }

    /// <summary>
    ///     Tests parsing of simple CSV format (comma-separated).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SimpleCsv_ShouldParseFields()
    {
        var query = @"
            text CsvRow {
                Name: until ',',
                Address: until ',',
                Age: until ',',
                Salary: rest trim
            };
            select
                c.Name,
                c.Address,
                c.Age,
                c.Salary
            from #test.files() f
            cross apply Parse(f.Text, 'CsvRow') c";

        var csvLines = new[]
        {
            "John Smith,123 Main St,35,75000",
            "Jane Doe,456 Oak Ave,28,82000",
            "Bob Wilson,789 Pine Rd,42,95000"
        };

        var entities = csvLines.Select((line, i) => new TextEntity
        {
            Name = $"row_{i}",
            Text = line
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        var names = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("John Smith", names);
        Assert.Contains("Jane Doe", names);
        Assert.Contains("Bob Wilson", names);
    }

    /// <summary>
    ///     Tests parsing of email-style headers (Name: Value format).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_EmailHeaders_ShouldParseMailFields()
    {
        var query = @"
            text EmailHeader {
                Field: until ':',
                _: until ' ',
                Value: rest
            };
            select
                e.Field,
                e.Value
            from #test.files() f
            cross apply Parse(f.Text, 'EmailHeader') e
            where e.Field in ('From', 'To', 'Subject', 'Date')";

        var emailHeaders = new[]
        {
            "From: sender@example.com",
            "To: recipient@example.com",
            "Subject: Important Meeting Tomorrow",
            "Date: Mon 5 Jan 2026 10:30:00",
            "Message-ID: abc123@mail.example.com",
            "MIME-Version: 1.0",
            "Content-Type: text/plain"
        };

        var entities = emailHeaders.Select((h, i) => new TextEntity
        {
            Name = $"header_{i}",
            Text = h
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(4, table.Count);
        var fields = table.Select(r => (string)r[0]).ToList();
        Assert.Contains("From", fields);
        Assert.Contains("To", fields);
        Assert.Contains("Subject", fields);
        Assert.Contains("Date", fields);
    }

    /// <summary>
    ///     Tests parsing of URL-like format (protocol://host/path).
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_UrlFormat_ShouldParseUrlComponents()
    {
        var query = @"
            text UrlEntry {
                Protocol: until ':',
                _: until '/',
                _: until '/',
                Host: until '/',
                Path: rest trim
            };
            select
                u.Protocol,
                u.Host,
                u.Path
            from #test.files() f
            cross apply Parse(f.Text, 'UrlEntry') u
            where u.Protocol = 'https'";

        var urls = new[]
        {
            "https://api.example.com/v1/users",
            "http://localhost/health",
            "https://cdn.example.net/assets/image.png",
            "ftp://files.example.org/pub/file.zip"
        };

        var entities = urls.Select((url, i) => new TextEntity
        {
            Name = $"url_{i}",
            Text = url
        }).ToArray();

        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var hosts = table.Select(r => (string)r[1]).ToList();
        Assert.Contains("api.example.com", hosts);
        Assert.Contains("cdn.example.net", hosts);

        Assert.IsTrue(table.All(r => (string)r[0] == "https"));
    }

    #endregion

    #region Lines Function With Parse Tests

    /// <summary>
    ///     Tests the Lines() + Parse() pattern: split multi-line content then parse each line.
    /// </summary>
    [TestMethod]
    public void Query_LinesFunctionWithParse_ShouldSplitAndParseEachLine()
    {
        var query = @"
            text LogEntry {
                Timestamp: until ' ',
                Level: until ' ',
                Message: rest
            };
            select
                log.Timestamp,
                log.Level,
                log.Message
            from #test.files() f
            cross apply Lines(f.Text) line
            cross apply Parse(line.Value, 'LogEntry') log";

        var multiLineContent = string.Join("\n",
            "2024-01-15T10:30:00 INFO Application started",
            "2024-01-15T10:30:01 WARN Low memory detected",
            "2024-01-15T10:30:02 ERROR Connection failed");

        var entities = new[]
        {
            new TextEntity { Name = "app.log", Text = multiLineContent }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);

        var timestamps = table.Select(r => (string)r[0]).ToList();
        var levels = table.Select(r => (string)r[1]).ToList();
        var messages = table.Select(r => (string)r[2]).ToList();

        Assert.Contains("2024-01-15T10:30:00", timestamps);
        Assert.Contains("2024-01-15T10:30:01", timestamps);
        Assert.Contains("2024-01-15T10:30:02", timestamps);

        Assert.Contains("INFO", levels);
        Assert.Contains("WARN", levels);
        Assert.Contains("ERROR", levels);

        Assert.Contains("Application started", messages);
        Assert.Contains("Low memory detected", messages);
        Assert.Contains("Connection failed", messages);
    }

    /// <summary>
    ///     Tests Lines() + Parse() with Windows-style line endings.
    /// </summary>
    [TestMethod]
    public void Query_LinesFunctionWithParse_WindowsNewlines_ShouldWork()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest trim
            };
            select
                kv.Key,
                kv.Value
            from #test.files() f
            cross apply Lines(f.Text) line
            cross apply Parse(line.Value, 'KeyValue') kv
            order by kv.Key asc";

        var multiLineContent = "host=localhost\r\nport=8080\r\ndebug=true";

        var entities = new[]
        {
            new TextEntity { Name = "config.ini", Text = multiLineContent }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("debug", table[0][0]);
        Assert.AreEqual("true", table[0][1]);
        Assert.AreEqual("host", table[1][0]);
        Assert.AreEqual("localhost", table[1][1]);
        Assert.AreEqual("port", table[2][0]);
        Assert.AreEqual("8080", table[2][1]);
    }

    /// <summary>
    ///     Tests that using an unquoted schema name in Parse() produces a clear error.
    /// </summary>
    [TestMethod]
    public void Query_LinesFunctionWithParse_UnquotedSchemaName_ShouldFailWithClearError()
    {
        var query = @"
            text LogEntry {
                Timestamp: until ' ',
                Level: until ' ',
                Message: rest
            };
            select
                log.Timestamp,
                log.Level,
                log.Message
            from #test.files() f
            cross apply Lines(f.Text) line
            cross apply Parse(line.Value, LogEntry) log";

        var entities = new[]
        {
            new TextEntity { Name = "app.log", Text = "2024-01-15 INFO Started" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var ex = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver, TestCompilationOptions));

        Assert.IsTrue(
            ex.Message.Contains("must be quoted", StringComparison.OrdinalIgnoreCase),
            $"Expected error about quoting schema name, got: {ex.Message}");
        Assert.IsTrue(
            ex.Message.Contains("LogEntry", StringComparison.Ordinal),
            $"Expected error to mention the schema name 'LogEntry', got: {ex.Message}");
    }

    #endregion

    #region Array Indexing in WHERE Clause Tests

    /// <summary>
    ///     Tests array indexing in WHERE clause for byte arrays.
    /// </summary>
    [TestMethod]
    public void Query_WhereClause_WithByteArrayIndexing_ShouldFilter()
    {
        var query = @"
            binary MagicHeader {
                Magic: byte[4],
                Version: int le
            };
            select
                f.Name,
                h.Version
            from #test.files() f
            cross apply Interpret(f.Content, 'MagicHeader') h
            where h.Magic[0] = 0x7F and h.Magic[1] = 0x45";

        using var ms1 = new MemoryStream();
        using var bw1 = new BinaryWriter(ms1);
        bw1.Write(new byte[] { 0x7F, 0x45, 0x4C, 0x46 });
        bw1.Write(1);

        using var ms2 = new MemoryStream();
        using var bw2 = new BinaryWriter(ms2);
        bw2.Write(new byte[] { 0x4D, 0x5A, 0x00, 0x00 });
        bw2.Write(2);

        using var ms3 = new MemoryStream();
        using var bw3 = new BinaryWriter(ms3);
        bw3.Write(new byte[] { 0x7F, 0x45, 0x00, 0x00 });
        bw3.Write(3);

        var entities = new[]
        {
            new BinaryEntity { Name = "elf.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "mz.bin", Content = ms2.ToArray() },
            new BinaryEntity { Name = "custom.bin", Content = ms3.ToArray() }
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);


        Assert.AreEqual(2, table.Count);
        var names = table.Select(r => (string)r[0]).OrderBy(n => n).ToList();
        Assert.AreEqual("custom.bin", names[0]);
        Assert.AreEqual("elf.bin", names[1]);
    }

    /// <summary>
    ///     Tests array indexing in SELECT clause for byte arrays.
    /// </summary>
    [TestMethod]
    public void Query_SelectClause_WithByteArrayIndexing_ShouldExtractElements()
    {
        var query = @"
            binary MagicHeader {
                Magic: byte[4],
                Version: int le
            };
            select
                h.Magic[0],
                h.Magic[1],
                h.Magic[2],
                h.Magic[3],
                h.Version
            from #test.files() f
            cross apply Interpret(f.Content, 'MagicHeader') h";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(new byte[] { 0x7F, 0x45, 0x4C, 0x46 });
        bw.Write(42);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x7F, table[0][0]);
        Assert.AreEqual((byte)0x45, table[0][1]);
        Assert.AreEqual((byte)0x4C, table[0][2]);
        Assert.AreEqual((byte)0x46, table[0][3]);
        Assert.AreEqual(42, table[0][4]);
    }

    #endregion


    #region Bitwise Expression Alias Tests

    /// <summary>
    ///     Tests bitwise AND operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithBitwiseAndAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                PackedByte: byte
            };
            select
                h.PackedByte & 0x80 as HighBit
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[] { 0xF7 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x80L, table[0][0]);
    }

    /// <summary>
    ///     Tests bitwise OR operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithBitwiseOrAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                LowByte: byte,
                HighByte: byte
            };
            select
                h.LowByte | h.HighByte as Combined
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[] { 0x0F, 0xF0 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xFF, Convert.ToInt32(table[0][0]));
    }

    /// <summary>
    ///     Tests bitwise XOR operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithBitwiseXorAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                A: byte,
                B: byte
            };
            select
                h.A ^ h.B as Xored
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[] { 0xAA, 0x55 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xFF, Convert.ToInt32(table[0][0]));
    }

    /// <summary>
    ///     Tests left shift operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithLeftShiftAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                Value: byte
            };
            select
                h.Value << 4 as Shifted
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[] { 0x0F };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xF0, Convert.ToInt32(table[0][0]));
    }

    /// <summary>
    ///     Tests right shift operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithRightShiftAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                Value: byte
            };
            select
                h.Value >> 4 as Shifted
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[] { 0xF0 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x0F, Convert.ToInt32(table[0][0]));
    }

    /// <summary>
    ///     Tests complex bitwise expression with alias in SELECT.
    ///     Note: This tests parentheses with bitwise and shift operators.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithComplexBitwiseExpressionAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                PackedByte: byte
            };
            select
                h.PackedByte >> 4 as HighNibble,
                h.PackedByte & 0x0F as LowNibble
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[] { 0xAB };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(0x0A, Convert.ToInt32(table[0][0]));
        Assert.AreEqual(0x0BL, table[0][1]);
    }

    /// <summary>
    ///     Tests multiple bitwise aliases in a single SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithMultipleBitwiseAliases_ShouldWork()
    {
        var query = @"
            binary GifFlags {
                PackedByte: byte
            };
            select
                h.PackedByte & 0x80 as HasGlobalColorTable,
                (h.PackedByte & 0x70) >> 4 as ColorResolution,
                h.PackedByte & 0x08 as SortFlag,
                h.PackedByte & 0x07 as SizeOfGlobalColorTable
            from #test.files() f
            cross apply Interpret(f.Content, 'GifFlags') h";


        var testData = new byte[] { 0xF7 };
        var entities = new[] { new BinaryEntity { Name = "test.gif", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x80L, table[0][0]);
        Assert.AreEqual(0x07L, table[0][1]);
        Assert.AreEqual(0x00L, table[0][2]);
        Assert.AreEqual(0x07L, table[0][3]);
    }

    #endregion

    #region Parse and TryParse in SELECT Should Fail

    [TestMethod]
    public void Query_ParseInSelect_ShouldProduceMeaningfulError()
    {
        var query = @"
            text LogEntry {
                Level: until ':',
                _: literal ' ',
                Message: rest
            };
            select Parse('INFO: booted', 'LogEntry')
            from #test.lines() f";

        var entities = new[] { new TextEntity { Name = "log.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var ex = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver, TestCompilationOptions));

        Assert.IsTrue(
            ex.Message.Contains("CROSS APPLY", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("OUTER APPLY", StringComparison.OrdinalIgnoreCase),
            $"Expected error mentioning CROSS APPLY or OUTER APPLY, got: {ex.Message}");
        Assert.IsTrue(
            ex.Message.Contains("Parse", StringComparison.Ordinal),
            $"Expected error mentioning 'Parse', got: {ex.Message}");
    }

    [TestMethod]
    public void Query_TryParseInSelect_ShouldProduceMeaningfulError()
    {
        var query = @"
            text LogEntry {
                Level: until ':',
                _: literal ' ',
                Message: rest
            };
            select TryParse('INFO: booted', 'LogEntry')
            from #test.lines() f";

        var entities = new[] { new TextEntity { Name = "log.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var ex = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver, TestCompilationOptions));

        Assert.IsTrue(
            ex.Message.Contains("CROSS APPLY", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("OUTER APPLY", StringComparison.OrdinalIgnoreCase),
            $"Expected error mentioning CROSS APPLY or OUTER APPLY, got: {ex.Message}");
        Assert.IsTrue(
            ex.Message.Contains("TryParse", StringComparison.Ordinal),
            $"Expected error mentioning 'TryParse', got: {ex.Message}");
    }

    [TestMethod]
    public void Query_InterpretInSelect_ShouldProduceMeaningfulError()
    {
        var query = @"
            binary Header {
                Magic: int le
            };
            select Interpret(0x00, 'Header')
            from #test.files() f";

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = [0x00] } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var ex = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver, TestCompilationOptions));

        Assert.IsTrue(
            ex.Message.Contains("CROSS APPLY", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("OUTER APPLY", StringComparison.OrdinalIgnoreCase),
            $"Expected error mentioning CROSS APPLY or OUTER APPLY, got: {ex.Message}");
        Assert.IsTrue(
            ex.Message.Contains("Interpret", StringComparison.Ordinal),
            $"Expected error mentioning 'Interpret', got: {ex.Message}");
    }

    [TestMethod]
    public void Query_ParseInWhereClause_ShouldProduceMeaningfulError()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest
            };
            select 1
            from #test.lines() f
            where Parse('key=val', 'KeyValue') is not null";

        var entities = new[] { new TextEntity { Name = "data.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var ex = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                schemaProvider,
                LoggerResolver, TestCompilationOptions));

        Assert.IsTrue(
            ex.Message.Contains("CROSS APPLY", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("OUTER APPLY", StringComparison.OrdinalIgnoreCase),
            $"Expected error mentioning CROSS APPLY or OUTER APPLY, got: {ex.Message}");
    }

    #endregion

    #region Parse With String Literal Tests

    [TestMethod]
    public void Query_ParseWithStringLiteral_ShouldWork()
    {
        var query = @"
            text LogEntry {
                Timestamp: between '[' ']',
                _: literal ' ',
                Level: until ':',
                _: literal ' ',
                Message: rest
            };
            select
                log.Timestamp,
                log.Level,
                log.Message
            from #test.lines() f
            cross apply Parse('[2026-03-09] INFO: booted', 'LogEntry') log";

        var entities = new[] { new TextEntity { Name = "log.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("2026-03-09", table[0][0]);
        Assert.AreEqual("INFO", table[0][1]);
        Assert.AreEqual("booted", table[0][2]);
    }

    [TestMethod]
    public void Query_ParseWithStringLiteral_SelectConstant_ShouldWork()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest
            };
            select
                1 as X
            from #test.lines() f
            cross apply Parse('host=localhost', 'KeyValue') kv";

        var entities = new[] { new TextEntity { Name = "data.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
    }

    [TestMethod]
    public void Query_ParseWithStringLiteral_SelectParsedFields_ShouldWork()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest
            };
            select
                kv.Key,
                kv.Value
            from #test.lines() f
            cross apply Parse('host=localhost', 'KeyValue') kv";

        var entities = new[] { new TextEntity { Name = "data.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("host", table[0][0]);
        Assert.AreEqual("localhost", table[0][1]);
    }

    [TestMethod]
    public void Query_TryParseWithStringLiteral_ShouldWork()
    {
        var query = @"
            text LogEntry {
                Timestamp: between '[' ']',
                _: literal ' ',
                Level: until ':',
                _: literal ' ',
                Message: rest
            };
            select
                log.Timestamp,
                log.Level,
                log.Message
            from #test.lines() f
            outer apply TryParse('[2026-03-09] INFO: booted', 'LogEntry') log";

        var entities = new[] { new TextEntity { Name = "log.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("2026-03-09", table[0][0]);
        Assert.AreEqual("INFO", table[0][1]);
        Assert.AreEqual("booted", table[0][2]);
    }

    [TestMethod]
    public void Query_TryParseWithStringLiteral_WhenParsingFails_ShouldReturnNull()
    {
        var query = @"
            text LogEntry {
                Timestamp: between '[' ']',
                _: literal ' ',
                Level: until ':',
                _: literal ' ',
                Message: rest
            };
            select
                log.Timestamp
            from #test.lines() f
            outer apply TryParse('not a valid log entry', 'LogEntry') log";

        var entities = new[] { new TextEntity { Name = "log.txt", Text = "dummy" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    #endregion
}
