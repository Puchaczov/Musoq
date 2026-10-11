using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    [DataRow(InnerJoinSampleFileName)]
    [DataRow(GroupBySingleSampleFileName)]
    [DataRow(MultipleWindowsSampleFileName)]
    public void BufferedFinalOutputSamples_WhenCheckedIn_ShouldBufferFreshFinalRowsWithoutCopies(string fileName)
    {
        var sample = ReadSample(fileName).Content;

        Assert.Contains("var __musoqFinalShapeRows = new List<ResultRow0>();", sample);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultRow0(", sample);
        Assert.Contains("return __musoqFinalShapeRows;", sample);
        Assert.Contains("yield return __musoqShapeRow;", sample);
        Assert.DoesNotContain("new ResultShape0(", sample);
        Assert.DoesNotContain("private sealed class ResultShape0", sample);
        Assert.DoesNotContain("new ResultRow0(__musoqShapeRow.", sample);
    }

    [TestMethod]
    public void SerialFinalOutput_ShouldConstructFreshRowsWithoutAnIntermediateShape()
    {
        var code = ReadSample("Q01_SimpleSelectWhere.cs").Content;

        Assert.DoesNotContain("new ResultShape0(", code);
        Assert.DoesNotContain("private sealed class ResultShape0", code);
        Assert.DoesNotContain("new ResultRow0(__musoqShapeRow.", code);
        Assert.Contains("yield return new ResultRow0(", code);
        Assert.Contains("yield return __musoqShapeRow;", code);
    }

    [TestMethod]
    public void PostOperationSamples_WhenCheckedIn_ShouldNameDerivedTablesWithTargetName()
    {
        var sourceNamePattern = new Regex(
            @"var\s+[A-Za-z_][A-Za-z0-9_]*\s*=\s*new Table\([A-Za-z_][A-Za-z0-9_]*\.Name,\s*__columns_",
            RegexOptions.Compiled);
        var offenders = ReadAllSamples()
            .Where(sample => sourceNamePattern.IsMatch(sample.Content))
            .Select(static sample => sample.FileName)
            .ToArray();

        Assert.IsEmpty(
            offenders,
            $"Derived post-operation tables should use their target table name, not source.Name: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void SimpleFinalOutputSamples_WhenCheckedIn_ShouldNotRetainSourceContexts()
    {
        var samples = ReadNamedSamples(
            "Q01_SimpleSelectWhere.cs",
            InnerJoinSampleFileName,
            "Q08_Distinct.cs",
            "Q60_BinaryBitsRepeatUntilInterpret.cs");

        Assert.HasCount(4, samples);

        foreach (var sample in samples)
        {
            Assert.DoesNotContain("__leftContext", sample.Content, sample.FileName);
            Assert.DoesNotContain("__rightContext", sample.Content, sample.FileName);
            Assert.DoesNotContain("Contexts => new object[]", sample.Content, sample.FileName);
            Assert.DoesNotContain("new ResultRow0(ko3iko.Name, population, (object)ko3iko)", sample.Content, sample.FileName);
            Assert.DoesNotContain("new ResultRow0(a.Name, b.Country, (object)a, (object)b)", sample.Content, sample.FileName);
            Assert.DoesNotContain("new Statement0Row0(file.Content, p.Flags, (object)file, (object)p)", sample.Content, sample.FileName);
        }
    }

    [TestMethod]
    public void CteJoinFrameQualifySample_WhenCheckedIn_ShouldNotRetainUnusedIntermediateContexts()
    {
        var sample = ReadSample(CteJoinFrameQualifySampleFileName)
            .Content;

        Assert.DoesNotContain("__leftContext", sample, CteJoinFrameQualifySampleFileName);
        Assert.DoesNotContain("__rightContext", sample, CteJoinFrameQualifySampleFileName);
        Assert.DoesNotContain("Contexts => new object[]", sample, CteJoinFrameQualifySampleFileName);
        Assert.DoesNotContain("new Cte0Row0(ko3iko.Name, ko3iko.City, population, (object)ko3iko)", sample, CteJoinFrameQualifySampleFileName);
        Assert.DoesNotContain("new Cte0HashPayload0(ko3iko.Name, population, (object)ko3iko)", sample, CteJoinFrameQualifySampleFileName);
        Assert.Contains("new Cte0HashPayload0(ko3iko.Name, ko3iko.Population)", sample, CteJoinFrameQualifySampleFileName);
        Assert.Contains("GeneratedColumnarBuffer<Statement0Row0>", sample, CteJoinFrameQualifySampleFileName);
        Assert.Contains("public Statement0Row0(StoredTableColumnBuffer1 buffer, int index)", sample, CteJoinFrameQualifySampleFileName);
        Assert.Contains("statement0.Add(b.Name, b.Population, a.Name, a.City)", sample, CteJoinFrameQualifySampleFileName);
    }

}
