using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void FinalOutputSamples_WhenCheckedIn_ShouldEmitPlainSelectShapes()
    {
        var samples = ReadSamples()
            .Where(static sample => sample.FileName is
                "Q01_SimpleSelectWhere.cs" or
                InnerJoinSampleFileName or
                GroupBySingleSampleFileName or
                MultipleWindowsSampleFileName)
            .ToArray();

        Assert.HasCount(4, samples);

        foreach (var sample in samples)
        {
            var shapeClass = ExtractFinalSelectShapeClass(sample.Content);

            Assert.Contains("private sealed class ResultShape0", shapeClass, sample.FileName);
            Assert.Contains("public ResultShape0(", shapeClass, sample.FileName);
            Assert.DoesNotContain(": Row", shapeClass, sample.FileName);
            Assert.DoesNotContain("AssignValue", shapeClass, sample.FileName);
            Assert.DoesNotContain("HasColumn", shapeClass, sample.FileName);
            Assert.DoesNotContain("public override", shapeClass, sample.FileName);
            Assert.DoesNotContain("Contexts", shapeClass, sample.FileName);
        }
    }

    [TestMethod]
    public void PostOperationSamples_WhenCheckedIn_ShouldNameDerivedTablesWithTargetName()
    {
        var sourceNamePattern = new Regex(
            @"var\s+[A-Za-z_][A-Za-z0-9_]*\s*=\s*new Table\([A-Za-z_][A-Za-z0-9_]*\.Name,\s*__columns_",
            RegexOptions.Compiled);
        var offenders = ReadSamples()
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
        var samples = ReadSamples()
            .Where(static sample => sample.FileName is
                "Q01_SimpleSelectWhere.cs" or
                InnerJoinSampleFileName or
                "Q08_Distinct.cs" or
                "Q60_BinaryBitsRepeatUntilInterpret.cs")
            .ToArray();

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
        var sample = ReadSamples()
            .Single(static item => item.FileName == CteJoinFrameQualifySampleFileName)
            .Content;

        Assert.DoesNotContain("__leftContext", sample, CteJoinFrameQualifySampleFileName);
        Assert.DoesNotContain("__rightContext", sample, CteJoinFrameQualifySampleFileName);
        Assert.DoesNotContain("Contexts => new object[]", sample, CteJoinFrameQualifySampleFileName);
        Assert.DoesNotContain("new Cte0Row0(ko3iko.Name, ko3iko.City, population, (object)ko3iko)", sample, CteJoinFrameQualifySampleFileName);
        Assert.Contains("new Cte0Row0(ko3iko.Name, ko3iko.City, population)", sample, CteJoinFrameQualifySampleFileName);
        Assert.Contains("new Statement0Row0(b.Name, b.Population, a.Name, a.City)", sample, CteJoinFrameQualifySampleFileName);
    }

    private static string ExtractFinalSelectShapeClass(string content)
    {
        const string classMarker = "private sealed class ResultShape0";
        var start = content.IndexOf(classMarker, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, content);

        var nextMemberCandidates = new[]
        {
            content.IndexOf("\n        private sealed class ", start + classMarker.Length, StringComparison.Ordinal),
            content.IndexOf("\n        private readonly struct ", start + classMarker.Length, StringComparison.Ordinal),
            content.IndexOf("\n        private static ", start + classMarker.Length, StringComparison.Ordinal)
        }.Where(static index => index >= 0);
        var end = nextMemberCandidates.DefaultIfEmpty(content.Length).Min();
        return content[start..end];
    }

}
