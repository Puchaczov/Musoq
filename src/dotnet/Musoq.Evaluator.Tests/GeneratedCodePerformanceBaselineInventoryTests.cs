using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Architecture;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedCodePerformanceBaselineInventoryTests
{
    private static readonly string[] ForbiddenGeneratedExecutionTokens =
    [
        "GetNestedValue",
        "GetNestedValueAccessor",
        "GetRequiredType",
        "GetRowSourceChunks",
        "System.Reflection"
    ];

    [TestMethod]
    public void CurrentCorpus_Contains237SnapshotsAndNoGeneratedReflection()
    {
        var files = Directory
            .EnumerateFiles(GeneratedCodeSampleArtifacts.SamplesDirectory, "*.cs")
            .ToArray();

        Assert.AreEqual(237, files.Length);
        Assert.AreEqual(237, GeneratedCodeSamplesCatalog.Samples.Count);

        var expectedFiles = new[]
        {
            "Q122_ScriptParameterSourceArgument.cs",
            "Q132_ScriptVariableSourceArgument.cs",
            "Q227_PerformanceJoinAggregate.cs",
            "Q228_PerformanceWideCorrelatedSubquery.cs",
            "Q229_PerformanceWindowCteSetOperation.cs",
            "Q230_PerformanceTableProjection.cs",
            "Q231_PublicDynamicRootConstant.cs",
            "Q232_PublicDynamicRootFilterProjection.cs",
            "Q233_PublicDynamicNestedNullable.cs",
            "Q234_PublicDynamicJoinMethod.cs",
            "Q58_BinaryGenericInterpret.cs",
            "Q59_BinaryNestedGenericInterpret.cs"
        };

        var actualFiles = files.Select(Path.GetFileName).ToArray();
        CollectionAssert.IsSubsetOf(expectedFiles, actualFiles);

        var offenders = FindTokenOffenders(files, ForbiddenGeneratedExecutionTokens);
        Assert.IsEmpty(offenders, FormatOffenders(offenders));

        var rendererDirectory = Path.Combine(
            RepositoryRoot(),
            "src",
            "dotnet",
            "Musoq.Targets.CSharpClr",
            "Rendering",
            "Execution");
        var rendererFiles = Directory.EnumerateFiles(rendererDirectory, "*.cs", SearchOption.AllDirectories);
        var rendererOffenders = FindTokenOffenders(rendererFiles, ForbiddenGeneratedExecutionTokens[..^1]);
        Assert.IsEmpty(rendererOffenders, FormatOffenders(rendererOffenders));
    }

    [TestMethod]
    public void Q228_WideCorrelation_UsesNestedTypedTupleKeys()
    {
        var source = File.ReadAllText(Path.Combine(
            GeneratedCodeSampleArtifacts.SamplesDirectory,
            "Q228_PerformanceWideCorrelatedSubquery.cs"));

        Assert.IsFalse(source.Contains("CreateNullableHashJoinKey", StringComparison.Ordinal));
        Assert.Contains("ValueTuple<", source);
        Assert.Contains("ValueTuple<int?, int?>", source);
    }

    [TestMethod]
    public void Q229_WindowSet_UsesOneTypedFinalCarrierAndAliasesTypedCteRows()
    {
        var source = File.ReadAllText(Path.Combine(
            GeneratedCodeSampleArtifacts.SamplesDirectory,
            "Q229_PerformanceWindowCteSetOperation.cs"));

        Assert.IsFalse(source.Contains("LeftShape0", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("RightRow0", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("MaterializeGeneratedRows", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("new LeftRow0(__musoqShapeRow", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("new List<LeftRow0>()", StringComparison.Ordinal));
        Assert.Contains("rightSorted = right.OrderBy", source);
        Assert.Contains("yield return resultLeftRow", source);
        Assert.Contains("yield return resultRightRow", source);
        Assert.Contains("MaterializeChunkedRowsList", source);
        Assert.Contains("ReturnDeferredTable [result: LeftRow0 <- LeftRow0]", source);
    }

    [TestMethod]
    public void Q230_TableProjection_ProjectsFinalRowsDirectly()
    {
        var source = File.ReadAllText(Path.Combine(
            GeneratedCodeSampleArtifacts.SamplesDirectory,
            "Q230_PerformanceTableProjection.cs"));

        var generatedCode = source[source.IndexOf("// === Generated C# ===", StringComparison.Ordinal)..];

        Assert.Contains("TableProjectionRows.ProjectOptionalRowsSerial", generatedCode);
        Assert.Contains("new ResultRow0(ko3iko.Name, ko3iko.City, population)", generatedCode);
        Assert.Contains("_ = __musoqMaterializedTable.Count", generatedCode);
        Assert.IsFalse(generatedCode.Contains("ResultShape0", StringComparison.Ordinal));
        Assert.IsFalse(generatedCode.Contains("ComputeShapeRows", StringComparison.Ordinal));
    }

    private static Dictionary<string, string[]> FindTokenOffenders(
        IEnumerable<string> files,
        IEnumerable<string> tokens)
    {
        var tokenArray = tokens.ToArray();
        return files
            .Select(path => new
            {
                FileName = Path.GetRelativePath(RepositoryRoot(), path),
                Tokens = tokenArray
                    .Where(token => File.ReadAllText(path).Contains(token, StringComparison.Ordinal))
                    .ToArray()
            })
            .Where(item => item.Tokens.Length > 0)
            .ToDictionary(item => item.FileName, item => item.Tokens, StringComparer.Ordinal);
    }

    private static string FormatOffenders(IReadOnlyDictionary<string, string[]> offenders)
    {
        return string.Join(
            Environment.NewLine,
            offenders.Select(item => $"{item.Key}: {string.Join(", ", item.Value)}"));
    }

    private static string RepositoryRoot() => RepositorySourceScan.RepositoryRoot();
}
