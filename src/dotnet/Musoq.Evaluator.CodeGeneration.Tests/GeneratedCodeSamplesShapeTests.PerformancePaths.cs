using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    [DataRow("B13_Apply_BenchmarkTable.cs")]
    [DataRow("X05_CollectionPivot_BenchmarkTable.cs")]
    public void ScalarBenchmarkSamples_UseDirectTypedOrdinalityWithoutWrapperChunks(string file)
    {
        var code = ReadSample(file).Content;
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
        var code = ReadSample(file).Content;
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
        var code = ReadSample("B01_Wide_BenchmarkTable.cs").Content;
        Assert.Contains("TransferOwnedRows [resultOrderRecords -> result: ResultRow0WithSortKeys]", code);
        Assert.Contains("resultOrderRecords.Sort(ResultRow0WithSortKeysComparer.Instance);", code);
        Assert.Contains("return resultOrderRecords;", code);
        Assert.DoesNotContain("new ResultShape0", code);
        Assert.DoesNotContain("__musoqFinalShapeRows", code);
    }

    [TestMethod]
    public void TolerantBenchmarkSample_UsesStatementCatchWithoutCapturingDelegate()
    {
        var code = ReadSample("B32_TryParse_BenchmarkTable.cs").Content;
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
        var code = ReadSample("X08_PacketAnalytics_BenchmarkTable.cs").Content;
        var declaration = CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes()
            .OfType<ClassDeclarationSyntax>().Single(type => type.Identifier.ValueText == carrier);
        var fields = declaration.Members.OfType<PropertyDeclarationSyntax>()
            .Select(static property => property.Identifier.ValueText)
            .Where(static name => name.StartsWith("r_", StringComparison.Ordinal) ||
                                  name.StartsWith("p_", StringComparison.Ordinal) ||
                                  name.StartsWith("t_", StringComparison.Ordinal)).ToArray();
        CollectionAssert.AreEquivalent(expected.Split(','), fields);
        Assert.Contains("p_Tail", code);
        Assert.Contains("new Musoq.Generated.Interpreters.Entry()", code);
    }
}
