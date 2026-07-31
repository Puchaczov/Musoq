using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualRealWorldAndFeatureTests
{
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
            cross apply Interpret<MagicHeader>(f.Content) h
            where h.Magic[0] = 0x7F and h.Magic[1] = 0x45";

        using var ms1 = new MemoryStream();
        using var bw1 = new BinaryWriter(ms1);
        bw1.Write(new byte[] { 0x7F, 0x45, 0x4C, 0x46 });
        bw1.Write(1);

        using var ms2 = new MemoryStream();
        using var bw2 = new BinaryWriter(ms2);
        bw2.Write("MZ\u0000\u0000"u8.ToArray());
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

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
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
            cross apply Interpret<MagicHeader>(f.Content) h";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(new byte[] { 0x7F, 0x45, 0x4C, 0x46 });
        bw.Write(42);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
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
}
