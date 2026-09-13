using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class SourcePredicateContractDocumentationTests
{
    [TestMethod]
    public void EvaluationPhaseDocumentation_ShouldDefineMaterializationBoundary()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositorySourceScan.RepositoryRoot(),
            "src",
            "dotnet",
            "Musoq.Schema",
            "Optimization",
            "SourcePredicateEvaluationPhase.cs"));

        Assert.Contains("after the source has materialized a row", source);
        Assert.Contains("before payload open, decode, or row materialization", source);
    }

    [TestMethod]
    public void MatchingSemanticsDocumentation_ShouldDescribePreparationCulture()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositorySourceScan.RepositoryRoot(),
            "docs",
            "source-string-match-specialization.md"));

        Assert.Contains("execution culture captured when the", source);
        Assert.DoesNotContain("culture-independent", source);
    }
}
