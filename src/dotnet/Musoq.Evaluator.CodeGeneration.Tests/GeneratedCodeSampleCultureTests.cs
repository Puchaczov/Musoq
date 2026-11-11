using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedCodeSampleCultureTests
{
    [TestMethod]
    [DataRow("en-US")]
    [DataRow("fr-FR")]
    public void DecimalSample_WhenCallerCultureDiffers_ShouldMatchTrackedSnapshot(string cultureName)
    {
        var callerCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var sample = GeneratedCodeSamplesCatalog.GetByFileName("Q108_RuntimeV2DecimalConversion.cs") with
            {
                Category = $"culture-regression-{cultureName}",
            };
            var generated = GeneratedCodeSampleArtifacts.Generate(sample, new TestsLoggerResolver());
            var expected = File.ReadAllText(GeneratedCodeSampleArtifacts.GetSamplePath(sample));

            Assert.AreEqual(
                GeneratedCodeSampleArtifacts.NormalizeForComparison(expected),
                GeneratedCodeSampleArtifacts.NormalizeForComparison(generated)
            );
        }
        finally
        {
            CultureInfo.CurrentCulture = callerCulture;
        }
    }

    [TestMethod]
    public void Sample_WhenGenerated_ShouldRestoreCallerCulture()
    {
        var callerCulture = CultureInfo.CurrentCulture;
        try
        {
            var requestedCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentCulture = requestedCulture;
            var sample = GeneratedCodeSamplesCatalog.GetByFileName("Q108_RuntimeV2DecimalConversion.cs") with
            {
                Category = "culture-restore-regression",
            };

            GeneratedCodeSampleArtifacts.Generate(sample, new TestsLoggerResolver());

            Assert.AreSame(requestedCulture, CultureInfo.CurrentCulture);
        }
        finally
        {
            CultureInfo.CurrentCulture = callerCulture;
        }
    }
}
