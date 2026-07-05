using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave18GuardrailTests
{
    private static readonly Regex BroadNotSupportedCatch = new(
        @"\bcatch\s*\(\s*NotSupportedException\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [TestMethod]
    public void DirectApplyOrderConversion_ShouldUseTypedUnsupportedResult()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var file = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "Visitors",
            "RewriteQueryVisitor.DirectApply.cs");

        var text = File.ReadAllText(file);

        StringAssert.Contains(text, ".TryConvert(expression)");
        Assert.IsFalse(
            BroadNotSupportedCatch.IsMatch(text),
            "Direct-apply order conversion must not catch broad NotSupportedException; unsupported parser shapes flow through ExpressionConverter.TryConvert.");
    }

    [TestMethod]
    public void UnsupportedShapeFallbacks_ShouldOnlyLeaveDocumentedCompatibilityBroadCatches()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Evaluator");

        var catches = productionFiles
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(item => BroadNotSupportedCatch.IsMatch(item.Text))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();

        CollectionAssert.AreEquivalent(
            new[]
            {
                "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.AsOfJoins.cs",
                "src/dotnet/Musoq.Evaluator/IR/Planning/SourcePredicatePlanner.cs"
            },
            catches
                .Select(item => item.Split(':')[0])
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            "Wave 18 should keep unsupported-shape fallback broad catches constrained to the remaining ASOF and source predicate compatibility boundaries.");
    }
}
