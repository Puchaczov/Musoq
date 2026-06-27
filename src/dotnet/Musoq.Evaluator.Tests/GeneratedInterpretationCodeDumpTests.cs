using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedInterpretationCodeDumpTests
{
    private readonly ILoggerResolver _loggerResolver = new TestsLoggerResolver();

    [TestMethod]
    public void Q16_BinaryInterpret()
    {
        AssertSampleMatchesCatalog("Q16_BinaryInterpret.cs");
    }

    [TestMethod]
    public void Q17_TextParse()
    {
        AssertSampleMatchesCatalog("Q17_TextParse.cs");
    }

    [TestMethod]
    [Ignore("Local snapshot refresh utility. Run intentionally when interpretation generated-code changes are expected.")]
    public void Refresh_Local_Interpretation_Samples()
    {
        foreach (var sample in GeneratedCodeSamplesCatalog.InterpretationSamples)
            GeneratedCodeSampleArtifacts.Write(sample, _loggerResolver);
    }

    private void AssertSampleMatchesCatalog(string fileName)
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName(fileName);
        var samplePath = GeneratedCodeSampleArtifacts.GetSamplePath(sample);
        if (!File.Exists(samplePath))
            return;

        var expected = File.ReadAllText(samplePath);
        var actual = GeneratedCodeSampleArtifacts.Generate(sample, _loggerResolver);

        Assert.AreEqual(
            GeneratedCodeSampleArtifacts.NormalizeForComparison(expected),
            GeneratedCodeSampleArtifacts.NormalizeForComparison(actual));
    }
}
