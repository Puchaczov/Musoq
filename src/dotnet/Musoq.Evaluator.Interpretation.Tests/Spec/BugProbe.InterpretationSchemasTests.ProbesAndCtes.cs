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
        var query = (
            "\n"
            + "            binary Pkg {\n"
            + "                Count: byte,\n"
            + "                Vals: short le[Count]\n"
            + "            };\n"
            + "            select Sum(v.Value) as Total\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Pkg>(f.Content) p\n"
            + "            cross apply p.Vals v"
        );

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = [1, 10, 0] },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var buildItems = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver
        );
        var generatedCode = string.Join(
            "\n",
            buildItems.Compilation.SyntaxTrees.Select(tree => tree.GetRoot().ToFullString())
        );
        var metadata = string.Join(
            "\n",
            (buildItems.PipelineInferredColumns ?? new Dictionary<string, Musoq.Schema.ISchemaColumn[]>())
                .OrderBy(entry => entry.Key)
                .Select(entry =>
                    $"{entry.Key}: [{string.Join(", ", entry.Value.Select(column => $"{column.ColumnName}:{column.ColumnIndex}"))}]"
                )
        );
        var generatedProbePath = CreateGeneratedProbePath(
            nameof(Probe_AggregateOnlyAliasOverInterpretation_ShouldDumpGeneratedCode),
            ".cs"
        );
        var generatedProbeMetaPath = CreateGeneratedProbePath(
            nameof(Probe_AggregateOnlyAliasOverInterpretation_ShouldDumpGeneratedCode),
            "_meta.txt"
        );
        File.WriteAllText(generatedProbePath, generatedCode);
        File.WriteAllText(generatedProbeMetaPath, metadata);

        Assert.IsTrue(File.Exists(generatedProbePath));
    }

    [TestMethod]
    public void Probe_GroupedAggregateAliasOverInterpretation_ShouldDumpGeneratedCode()
    {
        var query = (
            "\n"
            + "            binary Entry {\n"
            + "                Cat: byte,\n"
            + "                Score: short le\n"
            + "            };\n"
            + "            binary DataFile {\n"
            + "                Count: byte,\n"
            + "                Entries: Entry[Count]\n"
            + "            };\n"
            + "            select e.Cat, Count(e.Cat) as Cnt, Sum(e.Score) as Total\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<DataFile>(f.Content) d\n"
            + "            cross apply d.Entries e\n"
            + "            group by e.Cat\n"
            + "            having Count(e.Cat) > 1\n"
            + "            order by e.Cat asc"
        );

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

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = ms.ToArray() },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var buildItems = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver
        );
        var generatedCode = string.Join(
            "\n",
            buildItems.Compilation.SyntaxTrees.Select(tree => tree.GetRoot().ToFullString())
        );
        var generatedProbePath = CreateGeneratedProbePath(
            nameof(Probe_GroupedAggregateAliasOverInterpretation_ShouldDumpGeneratedCode),
            ".cs"
        );
        File.WriteAllText(generatedProbePath, generatedCode);

        Assert.IsTrue(File.Exists(generatedProbePath));
    }

    [TestMethod]
    public void Probe_CteWithTryInterpret_ShouldDumpGeneratedCode()
    {
        var query = (
            "\n"
            + "            binary Rec {\n"
            + "                Magic: int le check Magic = 48879,\n"
            + "                Val: short le\n"
            + "            };\n"
            + "            with ValidRecs as (\n"
            + "                select r.Val as V\n"
            + "                from #test.files() f\n"
            + "                cross apply TryInterpret<Rec>(f.Content) r\n"
            + "            )\n"
            + "            select Sum(V) as Total, Count(V) as Cnt\n"
            + "            from ValidRecs"
        );

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
            new BinaryEntity { Name = "ok2.bin", Content = ms3.ToArray() },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var buildItems = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver
        );
        var generatedCode = string.Join(
            "\n",
            buildItems.Compilation.SyntaxTrees.Select(tree => tree.GetRoot().ToFullString())
        );
        var generatedProbePath = CreateGeneratedProbePath(
            nameof(Probe_CteWithTryInterpret_ShouldDumpGeneratedCode),
            ".cs"
        );
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

        var query = (
            "\n"
            + "            binary Structure {\n"
            + "                C: ushort le,\n"
            + "                A: byte,\n"
            + "                B: string[A] ascii\n"
            + "            };\n"
            + "            with ParsedData as (\n"
            + "                select s.C as UshortVal, s.A as ByteVal, s.B as StrData\n"
            + "                from #test.files() b\n"
            + "                cross apply Interpret<Structure>(b.Content) s\n"
            + "            )\n"
            + "            select UshortVal, ByteVal, StrData\n"
            + "            from ParsedData"
        );

        var testData = ms.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((ushort)256, table[0].ReadValue(0));
        Assert.AreEqual((byte)5, table[0].ReadValue(1));
        Assert.AreEqual("Hello", table[0].ReadValue(2));
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

        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Len: byte,\n"
            + "                Text: string[Len] ascii,\n"
            + "                Count: int le\n"
            + "            };\n"
            + "            with FileData as (\n"
            + "                select b.Name as FileName, b.Content as FileContent\n"
            + "                from #test.files() b\n"
            + "            ),\n"
            + "            ParsedPackets as (\n"
            + "                select s.Len as PacketLen, s.Text as PacketText, s.Count as PacketCount\n"
            + "                from FileData f\n"
            + "                cross apply Interpret<Packet>(f.FileContent) s\n"
            + "            )\n"
            + "            select PacketLen, PacketText, PacketCount\n"
            + "            from ParsedPackets"
        );

        var testData = ms.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0].ReadValue(0));
        Assert.AreEqual("XYZ", table[0].ReadValue(1));
        Assert.AreEqual(42, table[0].ReadValue(2));
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

        var query = (
            "\n"
            + "            binary Structure {\n"
            + "                D: int le,\n"
            + "                C: ushort le,\n"
            + "                A: byte,\n"
            + "                B: string[A] ascii\n"
            + "            };\n"
            + "            with BinaryRecords as (\n"
            + "                select s.D as IntField, s.C as UshortField, s.A as LenField, s.B as NameField\n"
            + "                from #test.files() b\n"
            + "                cross apply Interpret<Structure>(b.Content) s\n"
            + "            )\n"
            + "            select IntField, UshortField, LenField, NameField\n"
            + "            from BinaryRecords\n"
            + "            where IntField > 40"
        );

        var testData = ms.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0].ReadValue(0));
        Assert.AreEqual((ushort)256, table[0].ReadValue(1));
        Assert.AreEqual((byte)5, table[0].ReadValue(2));
        Assert.AreEqual("Hello", table[0].ReadValue(3));
    }
}
