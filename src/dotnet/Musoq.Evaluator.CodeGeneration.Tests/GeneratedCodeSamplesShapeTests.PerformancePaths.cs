using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static readonly string[] BenchmarkShapeFiles =
    [
        "B01_Scan_BenchmarkTable.cs",
        "B01_Scan_BenchmarkTyped.cs",
        "B01_Wide_BenchmarkTable.cs",
        "B13_Apply_BenchmarkTable.cs",
        "B16_RowsFrame_BenchmarkTable.cs",
        "B16_RowsFrame_BenchmarkTyped.cs",
        "B22_Recursive_BenchmarkTable.cs",
        "B30_TextDelimited_BenchmarkTable.cs",
        "B31_TextCaptures_BenchmarkTable.cs",
        "B32_Partial_BenchmarkTable.cs",
        "B32_TryParse_BenchmarkTable.cs",
        "X05_CollectionPivot_BenchmarkTable.cs",
        "X08_PacketAnalytics_BenchmarkTable.cs",
        "X09_RecursiveChunks_BenchmarkTable.cs",
    ];

    private static readonly Lazy<GeneratedCodeSampleFile[]> BenchmarkShapeSamples = new(LoadBenchmarkShapeSamples);

    private static GeneratedCodeSampleFile[] LoadBenchmarkShapeSamples()
    {
        var directory = BenchmarkSnapshotGenerator.DirectoryPath;
        var manifest = BenchmarkSnapshotManifest.Parse(
            File.ReadAllText(Path.Combine(directory, BenchmarkSnapshotManifest.FileName))
        );
        var cases = manifest.Cases.ToLookup(item => item.File, StringComparer.Ordinal);
        return BenchmarkShapeFiles
            .Select(file => new GeneratedCodeSampleFile(
                file,
                "OfficialBenchmark",
                File.ReadAllText(Path.Combine(directory, file)),
                cases[file].First().OutputType
            ))
            .ToArray();
    }

    private static GeneratedCodeSampleFile ReadBenchmarkSample(string file) =>
        BenchmarkShapeSamples.Value.Single(sample => sample.FileName == file);

    [TestMethod]
    [DataRow("B13_Apply_BenchmarkTable.cs")]
    [DataRow("X05_CollectionPivot_BenchmarkTable.cs")]
    public void ScalarBenchmarkSamples_UseDirectTypedOrdinalityWithoutWrapperChunks(string file)
    {
        var code = ReadBenchmarkSample(file).Content;
        Assert.Contains("is int[]", code);
        Assert.Contains("is List<int>", code);
        Assert.DoesNotContain("PrimitiveTypeEntity<", code);
        Assert.DoesNotContain("ConvertScalarEnumerableToTypedChunks", code);
        Assert.DoesNotContain("ConvertEnumerableOutputToChunks<int>", code);
    }

    [TestMethod]
    [DataRow("B16_RowsFrame_BenchmarkTable.cs")]
    [DataRow("B16_RowsFrame_BenchmarkTyped.cs")]
    public void WindowBenchmarkSamples_SortAndReturnOwnedBufferWithStableTies(string file)
    {
        var code = ReadBenchmarkSample(file).Content;
        Assert.Contains("SortOwnedBuffer [result by", code);
        Assert.Contains("result.Sort(Comparer<ResultShape0>.Create", code);
        Assert.Contains("return left.__musoqSequence.CompareTo(right.__musoqSequence);", code);
        Assert.Contains("return result;", code);
        Assert.DoesNotContain("result.OrderBy", code);
        Assert.DoesNotContain("__musoqFinalShapeRows", code);
    }

    [TestMethod]
    public void WideBenchmarkSample_ReturnsSortCarrierWithoutAnotherFullWidthShape()
    {
        var code = ReadBenchmarkSample("B01_Wide_BenchmarkTable.cs").Content;
        Assert.Contains("TransferOwnedRows [resultOrderRecords -> result: ResultRow0WithSortKeys]", code);
        Assert.Contains(
            "EvaluationHelper.SortRecordsIfUnordered(resultOrderRecords, ResultRow0WithSortKeysComparer.Instance);",
            code
        );
        Assert.Contains("return resultOrderRecords;", code);
        Assert.Contains("ResultRow0WithSortKeys : Row", code);
        Assert.Contains("yield return __musoqShapeRow;", code);
        Assert.DoesNotContain("yield return new ResultRow0(", code);
        Assert.DoesNotContain("new ResultShape0", code);
        Assert.DoesNotContain("__musoqFinalShapeRows", code);
    }

    [TestMethod]
    public void TolerantBenchmarkSample_UsesStatementCatchWithoutCapturingDelegate()
    {
        var code = ReadBenchmarkSample("B32_TryParse_BenchmarkTable.cs").Content;
        Assert.Contains("catch (global::Musoq.Schema.Interpreters.ParseException)", code);
        Assert.DoesNotContain("new Func<", code);
        Assert.Contains("Valid? statement0_pRows = null;", code);
    }

    [TestMethod]
    [DataRow("Statement0Row0", "r_Key,r_Text,p_Body,p_Tail")]
    [DataRow("Statement1Row0", "r_Key,r_Text,p_Body")]
    [DataRow("Statement2Row0", "r_Key,p_Body,t_Level")]
    [DataRow("Statement3Row0", "p_Body,t_Level")]
    public void PacketBenchmarkSample_CarriesOnlyLivePassthroughFields(string carrier, string expected)
    {
        var code = ReadBenchmarkSample("X08_PacketAnalytics_BenchmarkTable.cs").Content;
        var declaration = CSharpSyntaxTree
            .ParseText(code)
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Single(type => type.Identifier.ValueText == carrier);
        var fields = declaration
            .Members.OfType<PropertyDeclarationSyntax>()
            .Select(static property => property.Identifier.ValueText)
            .Where(static name =>
                name.StartsWith("r_", StringComparison.Ordinal)
                || name.StartsWith("p_", StringComparison.Ordinal)
                || name.StartsWith("t_", StringComparison.Ordinal)
            )
            .ToArray();
        CollectionAssert.AreEquivalent(expected.Split(','), fields);
        Assert.Contains("p_Tail", code);
        Assert.Contains("new Musoq.Generated.Interpreters.Entry()", code);
    }
}
