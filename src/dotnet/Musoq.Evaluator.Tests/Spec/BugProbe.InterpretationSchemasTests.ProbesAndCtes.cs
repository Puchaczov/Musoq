using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class BugProbeInterpretationSchemasTests
{
    [TestMethod]
    public void Probe_AggregateOnlyAliasOverInterpretation_ShouldDumpGeneratedCode()
    {
        var query = @"
            binary Pkg {
                Count: byte,
                Vals: short le[Count]
            };
            select Sum(v.Value) as Total
            from #test.files() f
            cross apply Interpret<Pkg>(f.Content) p
            cross apply p.Vals v";

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = [1, 10, 0] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var buildItems = InstanceCreator.CreateForAnalyze(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver);
        var generatedCode = string.Join("\n", buildItems.Compilation.SyntaxTrees.Select(tree => tree.GetRoot().ToFullString()));
        var metadata = string.Join(
            "\n",
            (buildItems.PipelineInferredColumns ?? new Dictionary<string, Musoq.Schema.ISchemaColumn[]>())
            .OrderBy(entry => entry.Key)
            .Select(entry => $"{entry.Key}: [{string.Join(", ", entry.Value.Select(column => $"{column.ColumnName}:{column.ColumnIndex}"))}]"));
        var generatedProbePath = CreateGeneratedProbePath(nameof(Probe_AggregateOnlyAliasOverInterpretation_ShouldDumpGeneratedCode), ".cs");
        var generatedProbeMetaPath = CreateGeneratedProbePath(nameof(Probe_AggregateOnlyAliasOverInterpretation_ShouldDumpGeneratedCode), "_meta.txt");
        File.WriteAllText(generatedProbePath, generatedCode);
        File.WriteAllText(generatedProbeMetaPath, metadata);

        Assert.IsTrue(File.Exists(generatedProbePath));
    }

    [TestMethod]
    public void Probe_GroupedAggregateAliasOverInterpretation_ShouldDumpGeneratedCode()
    {
        var query = @"
            binary Entry {
                Cat: byte,
                Score: short le
            };
            binary DataFile {
                Count: byte,
                Entries: Entry[Count]
            };
            select e.Cat, Count(e.Cat) as Cnt, Sum(e.Score) as Total
            from #test.files() f
            cross apply Interpret<DataFile>(f.Content) d
            cross apply d.Entries e
            group by e.Cat
            having Count(e.Cat) > 1
            order by e.Cat asc";

        using var ms = new MemoryStream();
        ms.WriteByte(5);
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes((short)10));
        ms.WriteByte(2);
        ms.Write(BitConverter.GetBytes((short)20));
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes((short)15));
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes((short)25));
        ms.WriteByte(2);
        ms.Write(BitConverter.GetBytes((short)30));

        var entities = new[] { new BinaryEntity { Name = "a.bin", Content = ms.ToArray() } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var buildItems = InstanceCreator.CreateForAnalyze(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver);
        var generatedCode = string.Join("\n", buildItems.Compilation.SyntaxTrees.Select(tree => tree.GetRoot().ToFullString()));
        var generatedProbePath = CreateGeneratedProbePath(nameof(Probe_GroupedAggregateAliasOverInterpretation_ShouldDumpGeneratedCode), ".cs");
        File.WriteAllText(generatedProbePath, generatedCode);

        Assert.IsTrue(File.Exists(generatedProbePath));
    }

    [TestMethod]
    public void Probe_CteWithTryInterpret_ShouldDumpGeneratedCode()
    {
        var query = @"
            binary Rec {
                Magic: int le check Magic = 48879,
                Val: short le
            };
            with ValidRecs as (
                select r.Val as V
                from #test.files() f
                cross apply TryInterpret<Rec>(f.Content) r
            )
            select Sum(V) as Total, Count(V) as Cnt
            from ValidRecs";

        using var ms1 = new MemoryStream();
        ms1.Write(BitConverter.GetBytes(48879));
        ms1.Write(BitConverter.GetBytes((short)10));

        using var ms2 = new MemoryStream();
        ms2.Write(BitConverter.GetBytes(0xDEAD));
        ms2.Write(BitConverter.GetBytes((short)99));

        using var ms3 = new MemoryStream();
        ms3.Write(BitConverter.GetBytes(48879));
        ms3.Write(BitConverter.GetBytes((short)20));

        var entities = new[]
        {
            new BinaryEntity { Name = "ok1.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "bad.bin", Content = ms2.ToArray() },
            new BinaryEntity { Name = "ok2.bin", Content = ms3.ToArray() }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var buildItems = InstanceCreator.CreateForAnalyze(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver);
        var generatedCode = string.Join("\n", buildItems.Compilation.SyntaxTrees.Select(tree => tree.GetRoot().ToFullString()));
        var generatedProbePath = CreateGeneratedProbePath(nameof(Probe_CteWithTryInterpret_ShouldDumpGeneratedCode), ".cs");
        File.WriteAllText(generatedProbePath, generatedCode);

        Assert.IsTrue(File.Exists(generatedProbePath));
    }

    private static string CreateGeneratedProbePath(string testName, string suffix)
    {
        return Path.Combine(AppContext.BaseDirectory, $"__{testName}{suffix}");
    }

    /// <summary>
    ///     Exact reproduction of the user-reported failing schema:
    ///     binary Structure {
    ///     D: int le,
    ///     C: ushort le,
    ///     A: byte,
    ///     B: string[A] ascii
    ///     }
    /// </summary>

    [TestMethod]
    public void Binary_InterpretInFirstCTE_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((ushort)256);
        bw.Write((byte)5);
        bw.Write("Hello"u8.ToArray());
        bw.Flush();

        var query = @"
            binary Structure {
                C: ushort le,
                A: byte,
                B: string[A] ascii
            };
            with ParsedData as (
                select s.C as UshortVal, s.A as ByteVal, s.B as StrData
                from #test.files() b
                cross apply Interpret<Structure>(b.Content) s
            )
            select UshortVal, ByteVal, StrData
            from ParsedData";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((ushort)256, table[0][0]);
        Assert.AreEqual((byte)5, table[0][1]);
        Assert.AreEqual("Hello", table[0][2]);
    }

    /// <summary>
    ///     CTE Test: Interpret in SECOND CTE (first CTE fetches files, second CTE interprets)
    /// </summary>

    [TestMethod]
    public void Binary_InterpretInSecondCTE_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((byte)3);
        bw.Write("XYZ"u8.ToArray());
        bw.Write(42);
        bw.Flush();

        var query = @"
            binary Packet {
                Len: byte,
                Text: string[Len] ascii,
                Count: int le
            };
            with FileData as (
                select b.Name as FileName, b.Content as FileContent
                from #test.files() b
            ),
            ParsedPackets as (
                select s.Len as PacketLen, s.Text as PacketText, s.Count as PacketCount
                from FileData f
                cross apply Interpret<Packet>(f.FileContent) s
            )
            select PacketLen, PacketText, PacketCount
            from ParsedPackets";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        Assert.AreEqual("XYZ", table[0][1]);
        Assert.AreEqual(42, table[0][2]);
    }

    /// <summary>
    ///     CTE Test: Interpret in FIRST CTE with complex user schema (all field types)
    /// </summary>

    [TestMethod]
    public void Binary_InterpretInFirstCTE_ComplexSchema_ShouldWork()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(42);
        bw.Write((ushort)256);
        bw.Write((byte)5);
        bw.Write("Hello"u8.ToArray());
        bw.Flush();

        var query = @"
            binary Structure {
                D: int le,
                C: ushort le,
                A: byte,
                B: string[A] ascii
            };
            with BinaryRecords as (
                select s.D as IntField, s.C as UshortField, s.A as LenField, s.B as NameField
                from #test.files() b
                cross apply Interpret<Structure>(b.Content) s
            )
            select IntField, UshortField, LenField, NameField
            from BinaryRecords
            where IntField > 40";

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
        Assert.AreEqual((ushort)256, table[0][1]);
        Assert.AreEqual((byte)5, table[0][2]);
        Assert.AreEqual("Hello", table[0][3]);
    }
}
